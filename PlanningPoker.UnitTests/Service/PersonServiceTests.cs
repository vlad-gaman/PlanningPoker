using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class PersonServiceTests
{
    private readonly PersonRepository _repository = new();
    private readonly PersonService _service;

    public PersonServiceTests()
    {
        _service = new PersonService(_repository, new PersonMapper(), new PersonEntityMapper());
    }

    [Fact]
    public void Get_NullGuid_ReturnsNoSession()
    {
        var response = _service.Get(null);

        Assert.False(response.IsSuccess);
        Assert.Equal(ErrorCodes.NoSession, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Get_StaleGuid_ReturnsNoSession()
    {
        var response = _service.Get(Guid.NewGuid());

        Assert.Equal(ErrorCodes.NoSession, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Get_KnownGuid_ReturnsPerson()
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "test" };
        _repository.TryAdd(entity);

        var response = _service.Get(entity.Guid);

        Assert.True(response.IsSuccess);
        Assert.Equal("Ann", response.Data!.Name);
        Assert.Equal("test", response.Data.PersonType);
    }

    [Fact]
    public void Upsert_NoExistingPerson_CreatesWithNewGuid()
    {
        var response = _service.Upsert(null, " Ann ", "dev");

        Assert.True(response.IsSuccess);
        Assert.NotEqual(Guid.Empty, response.Data!.Guid);
        Assert.Equal("Ann", response.Data.Name);
        Assert.NotNull(_repository.Get(response.Data.Guid));
    }

    [Fact]
    public void Upsert_StaleGuid_CreatesNewPersonWithDifferentGuid()
    {
        var stale = Guid.NewGuid();

        var response = _service.Upsert(stale, "Ann", "obs");

        Assert.NotEqual(stale, response.Data!.Guid);
    }

    [Fact]
    public void Upsert_ExistingPerson_UpdatesInPlace()
    {
        var created = _service.Upsert(null, "Ann", "dev").Data!;

        var updated = _service.Upsert(created.Guid, "Bob", "obs").Data!;

        Assert.Equal(created.Guid, updated.Guid);
        Assert.Equal("Bob", _repository.Get(created.Guid)!.Name);
        Assert.Equal("obs", _repository.Get(created.Guid)!.PersonType);
    }
}
