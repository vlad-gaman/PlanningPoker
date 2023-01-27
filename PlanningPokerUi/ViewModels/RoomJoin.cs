using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlanningPokerUi.Models;

namespace PlanningPokerUi.ViewModels
{
    public class RoomJoinModel : PageModel
    {
        public string Guid { get; set; }
        public Person Person { get; set; }

        public void OnGet()
        {
        }
    }
}
