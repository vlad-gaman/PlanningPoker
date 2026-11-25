using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface IPersonService
{
    /// <summary>Resolves the session person from their session token; a missing or unknown token yields <c>no_session</c>.</summary>
    ServiceResponse<Person> Get(string? sessionToken);

    /// <summary>
    /// Updates the person when the session token is known, otherwise creates a new person with a new guid and token. A null
    /// <paramref name="cardBack"/> keeps the person's current card back, or gives a new person the default.
    /// </summary>
    ServiceResponse<PersonSession> Upsert(string? sessionToken, string name, string personType, string? cardBack = null);
}
