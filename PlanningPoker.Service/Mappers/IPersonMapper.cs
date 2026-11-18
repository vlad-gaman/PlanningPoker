using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IPersonMapper
{
    Person Map(PersonEntity entity);

    Person Map(PersonEntity entity, bool isOffgrid);

    /// <summary>Maps a person as seen in a room, where <paramref name="personType"/> is their role there instead of their profile's type.</summary>
    Person Map(PersonEntity entity, bool isOffgrid, string personType);

    /// <summary>The same person in another role.</summary>
    Person Map(Person person, string personType);
}
