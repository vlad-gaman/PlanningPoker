using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using PlanningPokerUi.Models;

namespace PlanningPokerUi.Pages
{
    public class IndexModel : PageModel
    {
        public Person Person { get; set; }
        public string Guid { get; set; }

        public string Title { get; set; } = "Test Title";

        public IndexModel()
        {
        }

        public void OnGet()
        {

        }
    }
}
