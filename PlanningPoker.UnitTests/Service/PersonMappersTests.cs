using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Mappers;

namespace PlanningPoker.UnitTests.Service;

public class PersonMappersTests
{
    [Fact]
    public void PersonMapper_MapsEntityToPerson()
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "test" };

        var person = new PersonMapper().Map(entity);

        Assert.Equal(entity.Guid, person.Guid);
        Assert.Equal("Ann", person.Name);
        Assert.Equal("test", person.PersonType);
    }

    [Fact]
    public void PersonEntityMapper_CreatesEntityWithGivenValuesAndNoConnections()
    {
        var guid = Guid.NewGuid();

        var entity = new PersonEntityMapper().Map(guid, "Ann", "obs");

        Assert.Equal(guid, entity.Guid);
        Assert.Equal("Ann", entity.Name);
        Assert.Equal("obs", entity.PersonType);
        Assert.Empty(entity.Connections);
    }
}
