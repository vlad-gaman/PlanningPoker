using PlanningPoker.Controller.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Controller;

public class PersonResponseMapperTests
{
    [Fact]
    public void Map_MapsPerson()
    {
        var person = new Person { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "obs" };

        var response = new PersonResponseMapper().Map(person);

        Assert.Equal(person.Guid, response.Guid);
        Assert.Equal("Ann", response.Name);
        Assert.Equal("obs", response.PersonType);
    }
}
