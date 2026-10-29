using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class OwnershipTransferredEventMapper(IPersonResponseMapper personMapper) : IOwnershipTransferredEventMapper
{
    public OwnershipTransferredEvent Map(Person newOwner)
    {
        return new OwnershipTransferredEvent { NewOwner = personMapper.Map(newOwner) };
    }
}
