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
        Assert.False(response.IsOffgrid);
    }

    [Fact]
    public void Map_CarriesTheCardBack()
    {
        var person = new Person { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "dev", CardBack = "waves" };

        Assert.Equal("waves", new PersonResponseMapper().Map(person).CardBack);
    }

    [Fact]
    public void Map_CarriesTheOffgridState()
    {
        var person = new Person { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "obs", IsOffgrid = true };

        Assert.True(new PersonResponseMapper().Map(person).IsOffgrid);
    }
}
