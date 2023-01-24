using Microsoft.AspNetCore.Mvc;
using PlanningPokerUi.Models;
using PlanningPokerUi.Services;
using PlanningPokerUi.ViewModels;
using System;

namespace PlanningPokerUi.Controllers
{
    public class IndexController : Controller
    {
        private readonly RoomsManagerService _roomsManagerService;
        private readonly PeopleManagerService _peopleManagerService;

        public IndexController(RoomsManagerService roomsManagerService, PeopleManagerService peopleManagerService)
        {
            _roomsManagerService = roomsManagerService;
            _peopleManagerService = peopleManagerService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("CreateRoom")]
        public IActionResult CreateRoom([FromForm] FormViewModel formViewModel)
        {
            var existingPerson = _peopleManagerService.GetPerson(HttpContext);
            existingPerson.CopyFrom(formViewModel);

            var guid = _roomsManagerService.CreateRoom(existingPerson, formViewModel.UseFunRoomName);
            if (string.IsNullOrEmpty(guid))
            {
                return Conflict();
            }

            return RedirectPermanent($"/Room/{guid}");
        }

        [HttpPost("JoinRoom")]
        public IActionResult JoinRoom(FormViewModel formViewModel)
        {
            var a = RedirectPermanent($"/Room/{formViewModel.RoomName}");
            a.PreserveMethod = true;
            return a;
        }
    }
}
