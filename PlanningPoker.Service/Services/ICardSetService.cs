using PlanningPoker.Common.Models;

namespace PlanningPoker.Service.Services;

public interface ICardSetService
{
    ServiceResponse<IReadOnlyList<CardSetOption>> GetAll();

    /// <summary>The cards of one set in display order; an unknown set fails validation.</summary>
    ServiceResponse<IReadOnlyList<CardOption>> GetCards(string cardSet);
}
