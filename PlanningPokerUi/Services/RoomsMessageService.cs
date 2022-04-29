using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PlanningPokerUi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

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
            if (room != null)
            {
                if (person.WebSocket != null && person.WebSocket == webSocket)
                {
                    _roomsManagerService.ExitRoom(person, room.Guid);
                }
                var otherPeople = room.GetPeople().Except(new List<Person>() { person });

                messageToSend = new Message()
                {
                    Verb = "Exited",
                    Object = person
                };
                await SendMessageToAll(messageToSend, otherPeople);

                if (room.DidEveryoneVote())
                {
                    messageToSend = GenerateVotesAndStatisticsMessage(room);
                    await SendMessageToAll(messageToSend, otherPeople);
                }
            }
            await base.OnDisconnectedAsync(webSocket, httpContext);
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
            Room room;
            Message messageToSend;
            switch (message.Verb)
            {
                case "Join":
                    person = _peopleManagerService.GetPerson(httpContext);
                    person.WebSocket = webSocket;
                    var roomGuid = Guid.Parse(message.Object as string);

                    var isSuccessful = !string.IsNullOrWhiteSpace(person.Name) && _roomsManagerService.JoinRoom(person, roomGuid);

                    room = _roomsManagerService.GetRoom(roomGuid);
                    var allPeople = isSuccessful ? room.GetPeople() : new List<Person>();

                    messageToSend = new Message()
                    {
                        Verb = "IsJoined"
                    };
                    bool everyoneVoted = false;

                    if (isSuccessful)
                    {
                        everyoneVoted = room.DidEveryoneVote();
                        messageToSend.Verb = "IsJoined";
                        messageToSend.Object = new
                        {
                            IsSuccessful = isSuccessful,
                            People = allPeople,
                            Votes = room.AllVotes().Select(p => new
                            {
                                Guid = p.guid,
                                Mark = everyoneVoted ? p.mark : "hide"
                            })
                        };
                    }
                    else
                    {
                        messageToSend.Object = new
                        {
                            Verb = "IsJoined",
                            IsSuccessful = isSuccessful
                        };
                    }


                    await SendMessageAsync(webSocket, JsonConvert.SerializeObject(messageToSend));

                    if (isSuccessful)
                    {
                        messageToSend = new Message()
                        {
                            Verb = "Joined",
                            Object = person
                        };
                        await SendMessageToAll(messageToSend, allPeople.Except(new List<Person> { person }));
                    }

                    if (everyoneVoted)
                    {
                        messageToSend = GenerateVotesAndStatisticsMessage(room);
                        await SendMessageToAll(messageToSend, allPeople);
                    }

                    break;
                case "Vote":
                    person = _peopleManagerService.GetPerson(httpContext);
                    room = _roomsManagerService.GetRoom(person);

                    var mark = message.Object as string;
                    room.Vote(person.Guid, mark);

                    if (room.DidEveryoneVote())
                    {
                        messageToSend = GenerateVotesAndStatisticsMessage(room);
                    }
                    else
                    {
                        messageToSend = new Message()
                        {
                            Verb = "AVote",
                            Object = person.Guid
                        };
                    }

                    await SendMessageToAll(messageToSend, room);
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

                    messageToSend = GenerateVotesAndStatisticsMessage(room);

                    await SendMessageToAll(messageToSend, room);
                    break;
                case "ObserverChange":
                    person = _peopleManagerService.GetPerson(httpContext);
                    room = _roomsManagerService.GetRoom(person);

                    var dynObj = message.Object as dynamic;

                    person.IsObserver = dynObj.IsObserver;

                    messageToSend = new Message()
                    {
                        Verb = "ObserverChange",
                        Object = person
                    };

                    await SendMessageToAll(messageToSend, room);

                    if (room.DidEveryoneVote())
                    {
                        messageToSend = GenerateVotesAndStatisticsMessage(room);
                        await SendMessageToAll(messageToSend, room.GetPeople());
                    }
                    else
                    {
                        var votes = room.AllVotes();
                        messageToSend = new Message()
                        {
                            Verb = "ShowVotes",
                            Object = new 
                            {
                                Votes = votes.Select(p => new
                                {
                                    Guid = p.guid,
                                    Mark = "hide"
                                }).ToList(),
                                Statistics = new
                                {
                                    Marks = new List<object>()
                                }
                            }
                        };
                        await SendMessageToAll(messageToSend, room.GetPeople());
                    }
                    break;
            }
        }

        private static Message GenerateVotesAndStatisticsMessage(Room room)
        {
            Message messageToSend;
            var votes = room.AllVotes();
            var groupedVotes = votes.GroupBy(p => p.mark).Select(group => new
            {
                Mark = group.Key,
                Percentage = (group.Count() / (decimal)votes.Count()) * 100
            });
            var valueMarks = votes.Where(a =>
            {
                return decimal.TryParse(a.mark, out _);
            });

            var maxPercent = groupedVotes.OrderByDescending(a => a.Percentage).FirstOrDefault();
            var otherHighestVote = groupedVotes.FirstOrDefault(a => a.Percentage == maxPercent.Percentage && a.Mark != maxPercent.Mark);

            messageToSend = new Message()
            {
                Verb = "ShowVotes",
                Object = new
                {
                    Votes = votes.Select(p => new
                    {
                        Guid = p.guid,
                        Mark = p.mark
                    }).ToList(),
                    Statistics = new
                    {
                        Marks = groupedVotes.OrderBy(a =>
                        {
                            if (decimal.TryParse(a.Mark, out var markDecimal))
                            {
                                return markDecimal;
                            }
                            else if (a.Mark == "?")
                            {
                                return 101;
                            }
                            else if (a.Mark == "coffee")
                            {
                                return 102;
                            }
                            return 103;
                        }),
                        AverageMark = valueMarks.Any() ? (decimal?)valueMarks.Average(a => decimal.Parse(a.mark)) : null,
                        HighestMark = otherHighestVote == null ? maxPercent?.Mark : null
                    }
                }
            };
            return messageToSend;
        }

        public Task SendMessageToAll(Message messageToSend, Room room)
        {
            return SendMessageToAll(messageToSend, room.GetPeople());
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
    }
}
