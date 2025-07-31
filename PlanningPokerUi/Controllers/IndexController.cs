using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PlanningPokerUi.Hubs;
using PlanningPokerUi.Models;
using PlanningPokerUi.Services;
using System;
using System.Linq;

namespace PlanningPokerUi.Controllers
{
    public class IndexController : Controller
    {
        private readonly RoomsManagerService _roomsManagerService;
        private readonly PeopleManagerService _peopleManagerService;
        private readonly IHubContext<RoomHub> _hubContext;

        public IndexController(RoomsManagerService roomsManagerService, PeopleManagerService peopleManagerService, IHubContext<RoomHub> hubContext)
        {
            _roomsManagerService = roomsManagerService;
            _peopleManagerService = peopleManagerService;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("api/cardsets")]
        public IActionResult GetCardSets()
        {
            var cardSets = Models.CardSets.CardSetNames.Select(kvp => new 
            { 
                Value = kvp.Key, 
                Display = kvp.Value 
            }).ToList();
            
            return Json(cardSets);
        }

        [HttpPost("CreateRoom")]
        public IActionResult CreateRoom([FromForm] FormViewModel formViewModel)
        {
            if (string.IsNullOrEmpty(formViewModel.Name))
            {
                return RedirectPermanent("/");
            }
            var newPerson = _peopleManagerService.CreatePerson(HttpContext);
            newPerson.CopyFrom(formViewModel);

            var guid = _roomsManagerService.CreateRoom(newPerson, formViewModel.UseFunRoomName, formViewModel.CardSet);
            if (string.IsNullOrEmpty(guid))
            {
                return Conflict();
            }

            SetupHealthCheck(guid);

            return RedirectPermanent($"/Room/{guid}");
        }

        [HttpPost("JoinRoom")]
        public IActionResult JoinRoom(FormViewModel formViewModel)
        {
            if (string.IsNullOrEmpty(formViewModel.Name) || string.IsNullOrEmpty(formViewModel.RoomName))
            {
                return RedirectPermanent("/");
            }

            var a = RedirectPermanent($"/Room/{formViewModel.RoomName}");
            a.PreserveMethod = true;
            return a;
        }

        private void SetupHealthCheck(string roomGuid)
        {
            var room = _roomsManagerService.GetRoom(roomGuid);
            if (room == null) return;

            var sendMessage = new System.Timers.ElapsedEventHandler(async (sender, e) =>
            {
                foreach (var p in room.GetNotConnected())
                {
                    try
                    {
                        if (p.ConnectionId == room.Guid)
                        {
                            _roomsManagerService.ExitRoom(p, room.Guid);
                        }
                        
                        var otherPeople = room.GetPeople().Except(new System.Collections.Generic.List<Person>() { p });
                        await _hubContext.Clients.Group(room.Guid).SendAsync("PersonExited", p);

                        if (room.IsVotingEnabled() && room.DidEveryoneVote())
                        {
                            await _hubContext.Clients.Group(room.Guid).SendAsync("VotesShown", room.GenerateResultsAndStatistics());
                        }
                    }
                    catch (Exception) { }
                }

                foreach (var p in room.AllPeople())
                {
                    p.IsConnected = false;
                }

                await _hubContext.Clients.Group(roomGuid).SendAsync("HealthCheckRequest");
            });

            room.HealthCheckTimer.SetElapsed(sendMessage);
        }
    }
}
