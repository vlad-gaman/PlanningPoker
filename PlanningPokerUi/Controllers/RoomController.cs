using Microsoft.AspNetCore.Mvc;
using PlanningPokerUi.Models;
using PlanningPokerUi.Services;
using PlanningPokerUi.ViewModels;

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


        [HttpGet]
        public IActionResult Room(string guid)
        {
            var person = _peopleManagerService.GetPerson(HttpContext);

            if (_roomsService.DoesRoomExist(guid))
            {
                if (person != null)
                {
                    var room = _roomsService.GetRoom(guid);
                    return View("Room", new RoomViewModel(room, person));
                }
                else
                {
                    return View("RoomJoin", new RoomJoinModel()
                    {
                        Guid = guid,
                        Person = person
                    });
                }
            }

            return RedirectPermanent("/");
        }

        [HttpPost]
        public IActionResult Room(string guid, FormViewModel formViewModel)
        {
            if (!string.IsNullOrEmpty(formViewModel?.Name))
            {
                var person = _peopleManagerService.CreatePerson(HttpContext);
                person.CopyFrom(formViewModel);
            }

            // Redirect to GET to prevent form resubmission dialog
            return RedirectToAction("Room", new { guid = guid });
        }
    }
}
