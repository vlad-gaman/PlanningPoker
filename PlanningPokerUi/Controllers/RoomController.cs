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


        public IActionResult Room(string guid, FormViewModel formViewModel = null)
        {
            var existingPerson = _peopleManagerService.GetPerson(HttpContext);
            if (!string.IsNullOrWhiteSpace(formViewModel.Name))
            {
                existingPerson.CopyFrom(formViewModel);
            }

            if (_roomsService.DoesRoomExist(guid))
            {
                if (existingPerson != null)
                {
                    return View("Room", new RoomViewModel()
                    {
                        Guid = guid,
                        Person = existingPerson
                    });
                }
                else
                {
                    var newPerson = _peopleManagerService.CreatePerson(HttpContext);

                    return View("RoomJoin", new RoomJoinModel()
                    {
                        Guid = guid,
                        Person = newPerson
                    });
                }
            }

            return Redirect("/");
        }
    }
}
