using Microsoft.AspNetCore.Http;
using PlanningPokerUi.Models;
using PlanningPokerUi.Services;
using System;
using System.Threading.Tasks;

namespace PlanningPokerUi.Middleware
{
    public class SessionMiddleware : IMiddleware
    {
        private readonly PeopleManagerService _peopleManagerService;

        public SessionMiddleware(PeopleManagerService peopleManagerService)
        {
            _peopleManagerService = peopleManagerService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var guidAsString = context.Session.GetString("Guid");
            if (string.IsNullOrWhiteSpace(guidAsString))
            {
                var guid = Guid.NewGuid();
                context.Session.SetString("Guid", guid.ToString());
                _peopleManagerService.AddPerson(guid, new Person()
                {
                    Guid = guid
                });
            }

            await next(context);
        }
    }
}
