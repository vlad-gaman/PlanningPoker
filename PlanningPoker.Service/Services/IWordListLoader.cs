using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface IWordListLoader
{
    WordLists Load();
}
