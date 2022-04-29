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
        public IActionResult CreateRoom([FromForm] Person person)
        {
            var existingPerson = _peopleManagerService.GetPerson(HttpContext);
            existingPerson.CopyFrom(person);

            var guid = _roomsManagerService.CreateRoom(existingPerson);
            if (guid == Guid.Empty)
            {
                return Conflict();
            }

            return LocalRedirect($"/Room/{guid}");
        }
    }
}
