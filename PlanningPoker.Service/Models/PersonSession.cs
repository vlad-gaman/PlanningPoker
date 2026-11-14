namespace PlanningPoker.Service.Models;

/// <summary>A person together with the secret token that identifies their session. Only the session cookie may carry the token.</summary>
public class PersonSession
{
    public Person Person { get; init; } = default!;

    public string SessionToken { get; init; } = default!;
}
