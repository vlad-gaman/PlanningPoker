using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface IPersonService
{
    /// <summary>Resolves the session person; a missing or stale guid yields <c>no_session</c>.</summary>
    ServiceResponse<Person> Get(Guid? guid);

    /// <summary>Updates the person when the guid is known, otherwise creates a new person with a new guid.</summary>
    ServiceResponse<Person> Upsert(Guid? existingGuid, string name, string personType);
}
