using Microsoft.AspNetCore.Http;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Extensions;

public static class HttpContextExtensions
{
    public static Person? GetSessionPerson(this HttpContext context)
    {
        return context.Items.TryGetValue(SessionConstants.PersonItemKey, out var person) ? person as Person : null;
    }

    public static void SetSessionPerson(this HttpContext context, Person person)
    {
        context.Items[SessionConstants.PersonItemKey] = person;
    }
}
