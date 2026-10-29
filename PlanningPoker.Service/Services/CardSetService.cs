using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;

namespace PlanningPoker.Service.Services;

public class CardSetService : ICardSetService
{
    public ServiceResponse<IReadOnlyList<CardSetOption>> GetAll()
    {
        return ServiceResponse<IReadOnlyList<CardSetOption>>.Success(CardSets.All);
    }
}
