using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PlanningPokerUi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PlanningPokerUi.Services
{
    public class RoomsMessageService : WebSocketHandlerService
    {
        private readonly RoomsManagerService _roomsManagerService;
        private readonly PeopleManagerService _peopleManagerService;

        public RoomsMessageService(RoomsManagerService roomsManagerService, PeopleManagerService peopleManagerService, WebSocketManagerService webSocketManagerService) : base(webSocketManagerService)
        {
            _roomsManagerService = roomsManagerService;
            _peopleManagerService = peopleManagerService;
        }

        public override async Task OnConnectedAsync(WebSocket webSocket, HttpContext httpContext)
        {
            var person = _peopleManagerService.GetPerson(httpContext);
            if (person.WebSocket != null && person.WebSocket.GetHashCode() != webSocket.GetHashCode())
            {
                var messageToSend = new Message()
                {
                    Verb = "Disconnected"
                };
                var messageAsString = JsonConvert.SerializeObject(messageToSend);
                await SendMessageAsync(person.WebSocket, messageAsString);
            }
            await base.OnConnectedAsync(webSocket, httpContext);
        }

        public override async Task OnDisconnectedAsync(WebSocket webSocket, HttpContext httpContext)
        {
            var messageToSend = new Message()
            {
                Verb = "Disconnected"
            };
            var messageAsString = JsonConvert.SerializeObject(messageToSend);
            await SendMessageAsync(webSocket, messageAsString);

            var person = _peopleManagerService.GetPerson(httpContext);
            var room = _roomsManagerService.GetRoom(person);
            await PersonExit(webSocket, person, room);
            await base.OnDisconnectedAsync(webSocket, httpContext);
        }

        private async Task PersonExit(WebSocket webSocket, Person person, Room room)
        {
            if (room == null)
            {
                return;
            }

            if (person.WebSocket != null && person.WebSocket == webSocket)
            {
                _roomsManagerService.ExitRoom(person, room.Guid);
            }
            var otherPeople = room.GetPeople().Except(new List<Person>() { person });

            var messageToSend = new Message()
            {
                Verb = "Exited",
                Object = person
            };
            await SendMessageToAll(messageToSend, otherPeople);

            if (room.IsVotingEnabled() && room.DidEveryoneVote())
            {
                await ShowVotesAndStatisticsWithTimer(room);
            }
        }

        public override async Task ReceiveAsync(HttpContext httpContext, WebSocket webSocket, WebSocketReceiveResult result, byte[] buffer)
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var messageAsString = Encoding.UTF8.GetString(buffer);

                var message = JsonConvert.DeserializeObject<Message>(messageAsString);
                await HandleMessage(message, webSocket, httpContext);
            }
        }

        public async Task HandleMessage(Message message, WebSocket webSocket, HttpContext httpContext)
        {
            Person person;
            Room room = null;
            Message messageToSend;

            switch (message.Verb)
            {
                case "Join":
                    person = _peopleManagerService.GetPerson(httpContext);
                    person.WebSocket = webSocket;
                    person.IsConnected = true;
                    var roomGuid = message.Object as string;

                    var isSuccessful = !string.IsNullOrWhiteSpace(person.Name) && _roomsManagerService.JoinRoom(person, roomGuid, out room);

                    messageToSend = new Message()
                    {
                        Verb = "IsJoined",
                        Object = new
                        {
                            IsSuccessful = isSuccessful,
                            People = isSuccessful ? room.GetPeople() : new List<Person>(),
                            VoteResultInfo = room.GetCurrentStatus()
                        }
                    };

                    await SendMessageAsync(webSocket, JsonConvert.SerializeObject(messageToSend));

                    if (isSuccessful)
                    {
                        messageToSend = new Message()
                        {
                            Verb = "Joined",
                            Object = new
                            {
                                Person = person,
                                Vote = room.GetVote(person.Guid)
                            }
                        };
                        await SendMessageToAll(messageToSend, room, except: person);

                        if (room.IsVotingEnabled() && person.PersonType != "obs")
                        {
                            room.VotingTimer.ClearElapsed();
                            messageToSend = new Message()
                            {
                                Verb = "Countdown",
                                Object = new
                                {
                                    Countdown = room.VotingTimer.Countdown,
                                    Reset = true
                                }
                            };
                            await SendMessageToAll(messageToSend, room, except: person);
                        }
                    }

                    break;
                case "Vote":
                    person = _peopleManagerService.GetPerson(httpContext);
                    room = _roomsManagerService.GetRoom(person);

                    if (room.IsVotingEnabled())
                    {
                        var mark = message.Object as string;
                        room.Vote(person.Guid, mark);

                        messageToSend = new Message()
                        {
                            Verb = "AVote",
                            Object = person.Guid
                        };
                        await SendMessageToAll(messageToSend, room);

                        if (room.DidEveryoneVote())
                        {
                            await ShowVotesAndStatisticsWithTimer(room);
                        }
                    }
                    break;
                case "ClearVotes":
                    person = _peopleManagerService.GetPerson(httpContext);
                    room = _roomsManagerService.GetRoom(person);

                    room.ClearVotes();

                    messageToSend = new Message()
                    {
                        Verb = "ClearVote"
                    };

                    await SendMessageToAll(messageToSend, room);
                    break;
                case "ShowVotes":
                    person = _peopleManagerService.GetPerson(httpContext);
                    room = _roomsManagerService.GetRoom(person);

                    if (room.IsVotingEnabled())
                    {
                        await ShowVotesAndStatisticsWithTimer(room);
                    }
                    break;
                case "ForceShowVotes":
                    person = _peopleManagerService.GetPerson(httpContext);
                    room = _roomsManagerService.GetRoom(person);
                    if (room.IsVotingEnabled())
                    {
                        await ShowVotesAndStatistics(room);
                    }
                    break;
                case "PersonTypeChange":
                    person = _peopleManagerService.GetPerson(httpContext);
                    room = _roomsManagerService.GetRoom(person);

                    var personTypeObj = message.Object as dynamic;

                    person.PersonType = personTypeObj.PersonType;

                    messageToSend = new Message()
                    {
                        Verb = "PersonTypeChange",
                        Object = new
                        {
                            Person = person,
                            VoteResultInfo = room.GetCurrentStatus()
                        }
                    };

                    await SendMessageToAll(messageToSend, room);

                    if (room.IsVotingEnabled())
                    {
                        if (room.DidEveryoneVote())
                        {
                            await ShowVotesAndStatisticsWithTimer(room);
                        }
                        else
                        {
                            room.VotingTimer.ClearElapsed();
                        }
                    }
                    break;
                case "Healthy":
                    person = _peopleManagerService.GetPerson(httpContext);
                    person.IsConnected = true;
                    break;
            }
        }

        private async Task ShowVotesAndStatisticsWithTimer(Room room)
        {
            var sendMessage = new ElapsedEventHandler(async (sender, e) =>
            {
                if (room.VotingTimer.Countdown == 0)
                {
                    await ShowVotesAndStatistics(room);
                }
                else
                {
                    await SendCountdown(room);
                }
            });

            room.VotingTimer.SetElapsed(sendMessage);
            await SendCountdown(room);
        }

        private async Task ShowVotesAndStatistics(Room room)
        {
            room.VotingTimer.ClearElapsed();
            await SendMessageToAll(new Message()
            {
                Verb = "ShowVotes",
                Object = room.GenerateResultsAndStatistics()
            }, room);
        }

        private Task SendCountdown(Room room)
        {
            return SendMessageToAll(new Message()
            {
                Verb = "Countdown",
                Object = new
                {
                    Countdown = room.VotingTimer.Countdown,
                    Reset = false
                }
            }, room);
        }

        public Task SendMessageToAll(Message messageToSend, Room room, Person except = null)
        {
            return SendMessageToAll(messageToSend, room.GetPeople().Except(except == null ? new List<Person>() : new List<Person>() { except }));
        }

        public Task SendMessageToAll(Message messageToSend, IEnumerable<Person> people)
        {
            var messageAsString = JsonConvert.SerializeObject(messageToSend);
            var tasks = new List<Task>();

            foreach (var aPerson in people)
            {
                tasks.Add(SendMessageAsync(aPerson.WebSocket, messageAsString));
            }

            return Task.WhenAll(tasks);
        }

        public void SetupHealthCheck(string roomGuid)
        {
            var room = _roomsManagerService.GetRoom(roomGuid);

            var sendMessage = new ElapsedEventHandler(async (sender, e) =>
            {
                foreach (var p in room.GetNotConnected())
                {
                    try
                    {
                        await PersonExit(p.WebSocket, p, room);
                        await _webSocketManagerService.RemoveWebSocket(p.Guid);
                    }
                    catch (Exception) { }
                }

                foreach (var p in room.AllPeople())
                {
                    p.IsConnected = false;
                }

                Message messageToSend = new Message()
                {
                    Verb = "HealthCheck"
                };


                await SendMessageToAll(messageToSend, room);
            });

            room.HealthCheckTimer.SetElapsed(sendMessage);
        }
    }
}
