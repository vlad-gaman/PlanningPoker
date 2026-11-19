using PlanningPoker.Service.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Common.Constants;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Services;

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
        Assert.False(person.IsOffgrid);
    }

    [Fact]
    public void PersonMapper_CarriesTheOffgridState()
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "test" };

        Assert.True(new PersonMapper().Map(entity, isOffgrid: true).IsOffgrid);
    }

    [Fact]
    public void PersonEntityMapper_CreatesEntityWithGivenValuesAndNoConnections()
    {
        var guid = Guid.NewGuid();

        var now = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var entity = new PersonEntityMapper(new Microsoft.Extensions.Time.Testing.FakeTimeProvider(now), new SessionTokenGenerator()).Map(guid, "Ann", "obs");

        Assert.Equal(guid, entity.Guid);
        Assert.Equal("Ann", entity.Name);
        Assert.Equal("obs", entity.PersonType);
        Assert.Empty(entity.Connections);
        Assert.Equal(now, entity.LastSeenAt);
    }

    [Fact]
    public void PersonEntityMapper_GivesEachPersonTheirOwnSessionToken()
    {
        var mapper = new PersonEntityMapper(TimeProvider.System, new SessionTokenGenerator());

        var first = mapper.Map(Guid.NewGuid(), "Ann", "dev");
        var second = mapper.Map(Guid.NewGuid(), "Bob", "dev");

        Assert.Equal(SessionTokenConstants.EncodedLength, first.SessionToken!.Length);
        Assert.NotEqual(first.SessionToken, second.SessionToken);
        Assert.DoesNotContain(first.Guid.ToString(), first.SessionToken);
    }

    [Fact]
    public void PersonSessionMapper_CarriesThePersonAndTheToken()
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), SessionToken = "token-ann", Name = "Ann", PersonType = "dev" };

        var session = new PersonSessionMapper(new PersonMapper()).Map(entity);

        Assert.Equal(entity.Guid, session.Person.Guid);
        Assert.Equal("Ann", session.Person.Name);
        Assert.Equal("token-ann", session.SessionToken);
    }

    [Fact]
    public void PersonMapper_MapsTheRoleInTheRoomInsteadOfTheProfilesType()
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "dev" };

        var person = new PersonMapper().Map(entity, isOffgrid: true, "obs");

        Assert.Equal("obs", person.PersonType);
        Assert.Equal("Ann", person.Name);
        Assert.True(person.IsOffgrid);
    }

    [Fact]
    public void PersonMapper_RetypesAPersonAndKeepsTheRest()
    {
        var person = new Person { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "dev", IsOffgrid = true };

        var retyped = new PersonMapper().Map(person, "test");

        Assert.Equal("test", retyped.PersonType);
        Assert.Equal(person.Guid, retyped.Guid);
        Assert.Equal("Ann", retyped.Name);
        Assert.True(retyped.IsOffgrid);
    }
}
