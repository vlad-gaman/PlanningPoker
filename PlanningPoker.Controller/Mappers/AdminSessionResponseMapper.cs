using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public class AdminSessionResponseMapper : IAdminSessionResponseMapper
{
    public AdminSessionResponse MapSignedIn(string username, string csrfToken)
    {
        return new AdminSessionResponse { Authenticated = true, Username = username, CsrfToken = csrfToken };
    }

    public AdminSessionResponse MapSignedOut()
    {
        return new AdminSessionResponse { Authenticated = false };
    }
}
