using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IAdminSessionResponseMapper
{
    AdminSessionResponse MapSignedIn(string username, string csrfToken);

    AdminSessionResponse MapSignedOut();
}
