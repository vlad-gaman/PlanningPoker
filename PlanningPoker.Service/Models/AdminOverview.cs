using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Models;

public class AdminOverview
{
    public AdminStats Stats { get; init; } = default!;

    public IReadOnlyList<AdminRoom> Rooms { get; init; } = [];

    public IReadOnlyList<IssueEntry> Issues { get; init; } = [];
}
