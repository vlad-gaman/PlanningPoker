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
        public IActionResult Room(string guid, bool clearSession = false)
        {
            // Clear session if requested (for testing or when user wants to enter different name)
            if (clearSession)
            {
                HttpContext.Session.Remove("Guid");
            }
            
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
                    // Check if there's a person in session but not in room
                    // This happens when user updated name on main page but hasn't joined a room yet
                    var sessionPerson = _peopleManagerService.GetPerson(HttpContext);
                    
                    return View("RoomJoin", new RoomJoinModel()
                    {
                        Guid = guid,
                        Person = sessionPerson // This will be null if no session, or contain the person with updated name
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
