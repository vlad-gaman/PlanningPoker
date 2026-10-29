using PlanningPoker.Common.Models;

namespace PlanningPoker.Service.Services;

public interface ICardSetService
{
    ServiceResponse<IReadOnlyList<CardSetOption>> GetAll();
}
