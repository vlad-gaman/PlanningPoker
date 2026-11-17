using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public interface IPersonEntityMapper
{
    PersonEntity Map(Guid guid, string name, string personType);
}
