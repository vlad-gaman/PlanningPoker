using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IOwnershipTransferredEventMapper
{
    OwnershipTransferredEvent Map(Person newOwner);
}
