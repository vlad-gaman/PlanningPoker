using Microsoft.AspNetCore.Mvc;
using PlanningPokerUi.Models;
using PlanningPokerUi.Services;
using PlanningPokerUi.ViewModels;
using System;

namespace PlanningPokerUi.Controllers
{
    public class RoomController : Controller
    {
        private readonly RoomsManagerService _roomsService;
        private readonly PeopleManagerService _peopleManagerService;

        public RoomController(RoomsManagerService roomsService, PeopleManagerService peopleManagerService)
        {
            _roomsService = roomsService;
            _peopleManagerService = peopleManagerService;
        }

        public IActionResult Room(Guid guid)
        {
            var person = _peopleManagerService.GetPerson(HttpContext);

            if (_roomsService.DoesRoomExist(guid))
            {
                return View(new RoomViewModel()
                {
                    Guid = guid,
                    Person = person
                });
            }
            return Redirect("/");
        }

        [HttpPost]
        public IActionResult Room(Guid guid, [FromForm] Person person)
        {
            var existingPerson = _peopleManagerService.GetPerson(HttpContext);
            existingPerson.CopyFrom(person);

            if (_roomsService.DoesRoomExist(guid))
            {
                return View("Room", new RoomViewModel()
                {
                    Guid = guid,
                    Person = existingPerson         
                });
            }
            return Redirect("/");
        }
    }
}
