using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface ICardBackChangedEventMapper
{
    CardBackChangedEvent Map(CardBackChangeResult result);
}
