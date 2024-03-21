using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanningPokerUi.Models;
using PlanningPokerUi.Services;
using System;

namespace PlanningPokerUi.Controllers
{
    public class IndexController : Controller
    {
        private readonly RoomsManagerService _roomsManagerService;
        private readonly PeopleManagerService _peopleManagerService;
        private readonly RoomsMessageService _roomsMessageService;

        public IndexController(RoomsManagerService roomsManagerService, PeopleManagerService peopleManagerService, RoomsMessageService roomsMessageService)
        {
            _roomsManagerService = roomsManagerService;
            _peopleManagerService = peopleManagerService;
            _roomsMessageService = roomsMessageService;
        }

        public IActionResult Index()
        {
            return View();
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

            var guid = _roomsManagerService.CreateRoom(newPerson, formViewModel.UseFunRoomName);
            if (string.IsNullOrEmpty(guid))
            {
                return Conflict();
            }

            _roomsMessageService.SetupHealthCheck(guid);

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
    }
}
