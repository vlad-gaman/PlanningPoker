using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using PlanningPokerUi.Models;
using PlanningPokerUi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Text.RegularExpressions;

namespace PlanningPokerUi.Hubs
{
    public class RoomHub : Hub
    {
        private readonly RoomsManagerService _roomsManagerService;
        private readonly PeopleManagerService _peopleManagerService;
        private readonly IHubContext<RoomHub> _hubContext;
        
        public RoomHub(RoomsManagerService roomsManagerService, PeopleManagerService peopleManagerService, IHubContext<RoomHub> hubContext)
        {
            _roomsManagerService = roomsManagerService;
            _peopleManagerService = peopleManagerService;
            _hubContext = hubContext;
        }

        private bool ValidateInput(string input, int maxLength = 100)
        {
            if (string.IsNullOrEmpty(input) || input.Length > maxLength)
                return false;
            
            // Basic XSS prevention - reject potentially dangerous characters
            var dangerousChars = new[] { '<', '>', '"', '\'', '&', '\0', '\r', '\n' };
            return !input.Any(c => dangerousChars.Contains(c));
        }

        private bool IsValidRoomIdentifier(string roomId)
        {
            // Accept GUIDs
            if (Guid.TryParse(roomId, out _))
                return true;
            
            // Accept fun room names (alphanumeric, underscore, hyphen, max 100 chars)
            if (string.IsNullOrWhiteSpace(roomId) || roomId.Length > 100)
                return false;
            
            // Fun room names should only contain letters, numbers, underscores, and hyphens
            return System.Text.RegularExpressions.Regex.IsMatch(roomId, @"^[a-zA-Z0-9_-]+$");
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            if (person != null)
            {
                person.IsConnected = true;
                person.ConnectionId = Context.ConnectionId;
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            var room = _roomsManagerService.GetRoom(person);
            
            // Clear any active timers to prevent ObjectDisposedException
            if (room != null)
            {
                room.VotingTimer.ClearElapsed();
                room.HealthCheckTimer.ClearElapsed();
            }
            
            
            await PersonExit(person, room);
            await base.OnDisconnectedAsync(exception);
        }

        private async Task PersonExit(Person person, Room room)
        {
            if (room == null || person == null)
            {
                return;
            }

            (bool roomDisposed, Person newOwner) exitResult = (false, null);
            
            if (person.ConnectionId == Context.ConnectionId)
            {
                exitResult = _roomsManagerService.ExitRoom(person, room.Guid);
            }
            
            // If room was disposed, no need to continue
            if (exitResult.roomDisposed)
            {
                await Clients.Group(room.Guid).SendAsync("RoomDisposed", new { Message = "Room has been closed as the last person left." });
                return;
            }
            
            await SendToGroupExcept(room.Guid, "PersonExited", person, person);
            
            // If ownership was transferred, notify all users
            if (exitResult.newOwner != null)
            {
                await Clients.Group(room.Guid).SendAsync("OwnershipTransferred", new 
                { 
                    NewOwner = exitResult.newOwner,
                    Message = $"Room ownership transferred to {exitResult.newOwner.Name}"
                });
            }

            if (room.IsVotingEnabled() && room.DidEveryoneVote())
            {
                // If countdown is 0, show votes immediately without timer
                if (room.Configuration.CountdownSeconds == 0)
                {
                    await ShowVotesAndStatistics(room);
                }
                else
                {
                    await ShowVotesAndStatisticsWithTimer(room);
                }
            }
        }

        public async Task JoinRoom(string roomGuid)
        {
            // Security: Input validation
            if (!IsValidRoomIdentifier(roomGuid))
            {
                await Clients.Caller.SendAsync("RoomJoined", new
                {
                    IsSuccessful = false,
                    People = new List<Person>(),
                    VoteResultInfo = (object)null,
                    Error = "Invalid room identifier."
                });
                return;
            }

            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            // Debug: Log session information
            var sessionId = httpContext?.Session?.Id;
            var guidStr = httpContext?.Session?.GetString("Guid");
            Console.WriteLine($"SignalR JoinRoom - SessionId: {sessionId}, GuidStr: {guidStr}, Person: {person?.Name}");
            
            // If person is null (session issue), send failure response
            if (person == null)
            {
                await Clients.Caller.SendAsync("RoomJoined", new
                {
                    IsSuccessful = false,
                    People = new List<Person>(),
                    VoteResultInfo = (object)null,
                    Error = $"Session expired or not found. SessionId: {sessionId}, Please refresh the page."
                });
                return;
            }

            person.ConnectionId = Context.ConnectionId;
            person.IsConnected = true;

            Room room = null;
            var isSuccessful = !string.IsNullOrEmpty(person.Name) && _roomsManagerService.JoinRoom(person, roomGuid, out room);

            await Clients.Caller.SendAsync("RoomJoined", new
            {
                IsSuccessful = isSuccessful,
                People = isSuccessful ? room.GetPeople() : new List<Person>(),
                VoteResultInfo = room?.GetCurrentStatus()
            });

            if (isSuccessful)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomGuid);
                
                await Clients.GroupExcept(roomGuid, Context.ConnectionId).SendAsync("PersonJoined", new
                {
                    Person = person,
                    Vote = room.GetVote(person.Guid),
                    PersonType = person.PersonType
                });

                if (room.IsVotingEnabled() && person.PersonType != "obs")
                {
                    room.VotingTimer.ClearElapsed();
                    await Clients.GroupExcept(roomGuid, Context.ConnectionId).SendAsync("CountdownReset", new
                    {
                        Countdown = room.VotingTimer.Countdown,
                        Reset = true
                    });
                }
            }
        }

        public async Task Vote(string mark)
        {
            // Security: Input validation
            if (!ValidateInput(mark, 20))
            {
                return;
            }

            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            if (person == null) return;
            
            var room = _roomsManagerService.GetRoom(person);

            if (room != null && room.IsVotingEnabled() && room.CanPersonVote(person))
            {
                room.Vote(person.Guid, mark);

                await Clients.Group(room.Guid).SendAsync("VoteCast", person.Guid);

                // Send updated status to all clients so they know if everyone has voted
                var currentStatus = room.GetCurrentStatus();
                await Clients.Group(room.Guid).SendAsync("VoteStatusUpdate", currentStatus);

                if (room.DidEveryoneVote() && room.Configuration.AutoShowVotes)
                {
                    // If countdown is 0, show votes immediately without timer
                    if (room.Configuration.CountdownSeconds == 0)
                    {
                        await ShowVotesAndStatistics(room);
                    }
                    else
                    {
                        await ShowVotesAndStatisticsWithTimer(room);
                    }
                }
            }
        }

        public async Task ClearVotes()
        {
            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            if (person == null) return;
            
            var room = _roomsManagerService.GetRoom(person);

            if (room != null)
            {
                room.ClearVotes();
                await Clients.Group(room.Guid).SendAsync("VotesCleared");
            }
        }

        public async Task ShowVotes()
        {
            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            if (person == null) return;
            
            var room = _roomsManagerService.GetRoom(person);

            if (room != null && room.IsVotingEnabled())
            {
                // If countdown is 0, show votes immediately without timer
                if (room.Configuration.CountdownSeconds == 0)
                {
                    await ShowVotesAndStatistics(room);
                }
                else
                {
                    await ShowVotesAndStatisticsWithTimer(room);
                }
            }
        }

        public async Task ForceShowVotes()
        {
            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            if (person == null) return;
            
            var room = _roomsManagerService.GetRoom(person);

            if (room != null && room.IsVotingEnabled())
            {
                await ShowVotesAndStatistics(room);
            }
        }

        public async Task ChangePersonType(string personType)
        {
            // Security: Input validation - only allow specific person types
            var validPersonTypes = new[] { "dev", "test", "obs" };
            if (!validPersonTypes.Contains(personType))
            {
                return;
            }

            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            if (person == null) return;
            
            var room = _roomsManagerService.GetRoom(person);

            if (room != null)
            {
                person.PersonType = personType;

                await Clients.Group(room.Guid).SendAsync("PersonTypeChanged", new
                {
                    Person = person,
                    VoteResultInfo = room.GetCurrentStatus()
                });

                if (room.IsVotingEnabled())
                {
                    if (room.DidEveryoneVote())
                    {
                        // If countdown is 0, show votes immediately without timer
                        if (room.Configuration.CountdownSeconds == 0)
                        {
                            await ShowVotesAndStatistics(room);
                        }
                        else
                        {
                            await ShowVotesAndStatisticsWithTimer(room);
                        }
                    }
                    else
                    {
                        room.VotingTimer.ClearElapsed();
                    }
                }
            }
        }

        public async Task HealthCheck()
        {
            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            if (person != null)
            {
                person.IsConnected = true;
            }
            
            // Always respond to keep the connection alive
            await Task.CompletedTask;
        }

        public async Task UpdateRoomConfiguration(RoomConfiguration configuration)
        {
            // Security: Input validation
            if (configuration == null)
                return;

            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            if (person == null) return;
            
            var room = _roomsManagerService.GetRoom(person);

            if (room != null)
            {
                // Update room configuration
                room.UpdateConfiguration(configuration);

                // Get the card set details for the update
                var cardSetDetails = Models.CardSets.GetCardSet(room.Configuration.CardSet);
                
                // Notify all room members about the configuration change
                await Clients.Group(room.Guid).SendAsync("RoomConfigurationUpdated", new
                {
                    Configuration = room.Configuration,
                    UpdatedBy = person.Name,
                    CardSetDetails = cardSetDetails.Select(card => new { value = card.value, display = card.display }).ToList()
                });
            }
        }

        private async Task ShowVotesAndStatisticsWithTimer(Room room)
        {
            var sendMessage = new ElapsedEventHandler(async (sender, e) =>
            {
                try
                {
                    if (room.VotingTimer.Countdown == 0)
                    {
                        await ShowVotesAndStatistics(room);
                    }
                    else
                    {
                        await SendCountdown(room);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Hub disposed, clear the timer to prevent further calls
                    room.VotingTimer.ClearElapsed();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in timer callback: {ex.Message}");
                    room.VotingTimer.ClearElapsed();
                }
            });

            room.VotingTimer.SetElapsed(sendMessage);
            await SendCountdown(room);
        }

        private async Task ShowVotesAndStatistics(Room room)
        {
            room.VotingTimer.ClearElapsed();
            await _hubContext.Clients.Group(room.Guid).SendAsync("VotesShown", room.GenerateResultsAndStatistics());
        }

        private async Task SendCountdown(Room room)
        {
            await _hubContext.Clients.Group(room.Guid).SendAsync("CountdownUpdate", new
            {
                Countdown = room.VotingTimer.Countdown,
                Reset = false
            });
        }

        private async Task SendToGroupExcept(string groupName, string method, object data, Person except)
        {
            if (except?.ConnectionId != null)
            {
                await Clients.GroupExcept(groupName, except.ConnectionId).SendAsync(method, data);
            }
            else
            {
                await Clients.Group(groupName).SendAsync(method, data);
            }
        }

        public void SetupHealthCheck(string roomGuid)
        {
            var room = _roomsManagerService.GetRoom(roomGuid);
            if (room == null) return;

            var sendMessage = new ElapsedEventHandler(async (sender, e) =>
            {
                foreach (var p in room.GetNotConnected())
                {
                    try
                    {
                        await PersonExit(p, room);
                    }
                    catch (Exception) { }
                }

                foreach (var p in room.AllPeople())
                {
                    p.IsConnected = false;
                }

                await Clients.Group(roomGuid).SendAsync("HealthCheckRequest");
            });

            room.HealthCheckTimer.SetElapsed(sendMessage);
        }
        
        public async Task TransferOwnership(string targetPersonGuid)
        {
            // Security: Input validation
            if (!Guid.TryParse(targetPersonGuid, out var targetGuid))
                return;

            var httpContext = Context.GetHttpContext();
            var person = _peopleManagerService.GetPerson(httpContext);
            
            if (person == null) return;
            
            var room = _roomsManagerService.GetRoom(person);

            if (room != null && room.Owner?.Guid == person.Guid) // Only current owner can transfer
            {
                var targetPerson = room.GetPeople().FirstOrDefault(p => p.Guid == targetGuid);
                
                if (targetPerson != null && room.TransferOwnership(targetPerson))
                {
                    await Clients.Group(room.Guid).SendAsync("OwnershipTransferred", new 
                    { 
                        NewOwner = targetPerson,
                        Message = $"Room ownership transferred to {targetPerson.Name} by {person.Name}"
                    });
                }
            }
        }
    }
}