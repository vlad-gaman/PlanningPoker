using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class PersonServiceTests
{
    private readonly PersonRepository _repository = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    private readonly PersonService _service;

    public PersonServiceTests()
    {
        var personMapper = new PersonMapper();
        _service = new PersonService(_repository, personMapper, new PersonSessionMapper(personMapper), new PersonEntityMapper(_time, new SessionTokenGenerator()), _time);
    }

    [Fact]
    public void Get_NullToken_ReturnsNoSession()
    {
        var response = _service.Get(null);

        Assert.False(response.IsSuccess);
        Assert.Equal(ErrorCodes.NoSession, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Get_UnknownToken_ReturnsNoSession()
    {
        var response = _service.Get(new SessionTokenGenerator().Generate());

        Assert.Equal(ErrorCodes.NoSession, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Get_KnownToken_ReturnsPerson()
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), SessionToken = "token-ann", Name = "Ann", PersonType = "test" };
        _repository.TryAdd(entity);

        var response = _service.Get("token-ann");

        Assert.True(response.IsSuccess);
        Assert.Equal(entity.Guid, response.Data!.Guid);
        Assert.Equal("Ann", response.Data.Name);
        Assert.Equal("test", response.Data.PersonType);
    }

    [Fact]
    public void Get_PublicGuidOfAKnownPerson_ReturnsNoSession()
    {
        var created = _service.Upsert(null, "Ann", "dev").Data!;

        var response = _service.Get(created.Person.Guid.ToString());

        Assert.Equal(ErrorCodes.NoSession, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Get_PersonStoredWithoutToken_IsNotReachable()
    {
        _repository.TryAdd(new PersonEntity { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "dev" });

        Assert.Equal(ErrorCodes.NoSession, Assert.Single(_service.Get("").Errors).Code);
        Assert.Equal(ErrorCodes.NoSession, Assert.Single(_service.Get(null).Errors).Code);
    }

    [Fact]
    public void Upsert_NoExistingPerson_CreatesWithNewGuidAndToken()
    {
        var response = _service.Upsert(null, " Ann ", "dev");

        Assert.True(response.IsSuccess);
        Assert.NotEqual(Guid.Empty, response.Data!.Person.Guid);
        Assert.Equal("Ann", response.Data.Person.Name);
        Assert.Equal(SessionTokenConstants.EncodedLength, response.Data.SessionToken.Length);
        Assert.Same(_repository.Get(response.Data.Person.Guid), _repository.GetBySessionToken(response.Data.SessionToken));
    }

    [Fact]
    public void Upsert_AddFails_ReturnsPersonNotStoredAndStoresNothing()
    {
        var repository = new InterleavingPersonRepository(_repository) { RejectAdds = true };
        var personMapper = new PersonMapper();
        var service = new PersonService(repository, personMapper, new PersonSessionMapper(personMapper), new PersonEntityMapper(_time, new SessionTokenGenerator()), _time);

        var response = service.Upsert(null, "Ann", "dev");

        Assert.False(response.IsSuccess);
        Assert.Null(response.Data);
        Assert.Equal(ErrorCodes.PersonNotStored, Assert.Single(response.Errors).Code);
        Assert.Empty(_repository.Guids());
    }

    [Fact]
    public void Upsert_NewPersonWithoutCardBack_GetsTheDefault()
    {
        var created = _service.Upsert(null, "Ann", "dev").Data!;

        Assert.Equal(CardBacks.Default, created.Person.CardBack);
    }

    [Fact]
    public void Upsert_NewPersonWithCardBack_KeepsIt()
    {
        var created = _service.Upsert(null, "Ann", "dev", CardBacks.Dots).Data!;

        Assert.Equal(CardBacks.Dots, _repository.Get(created.Person.Guid)!.CardBack);
    }

    [Fact]
    public void Upsert_ExistingPerson_ChangesCardBackOnlyWhenGiven()
    {
        var created = _service.Upsert(null, "Ann", "dev", CardBacks.Waves).Data!;

        _service.Upsert(created.SessionToken, "Ann", "dev");
        Assert.Equal(CardBacks.Waves, _repository.Get(created.Person.Guid)!.CardBack);

        _service.Upsert(created.SessionToken, "Ann", "dev", CardBacks.Nouveau);
        Assert.Equal(CardBacks.Nouveau, _repository.Get(created.Person.Guid)!.CardBack);
    }

    [Fact]
    public void Upsert_UnknownToken_CreatesNewPersonWithDifferentToken()
    {
        var unknown = new SessionTokenGenerator().Generate();

        var response = _service.Upsert(unknown, "Ann", "obs");

        Assert.NotEqual(unknown, response.Data!.SessionToken);
    }

    [Fact]
    public void Upsert_PublicGuidOfAKnownPerson_CreatesNewPersonAndLeavesTheOriginalUnchanged()
    {
        var original = _service.Upsert(null, "Ann", "dev").Data!;

        var response = _service.Upsert(original.Person.Guid.ToString(), "Bob", "obs").Data!;

        Assert.NotEqual(original.Person.Guid, response.Person.Guid);
        Assert.Equal("Ann", _repository.Get(original.Person.Guid)!.Name);
    }

    [Fact]
    public void Upsert_ExistingPerson_UpdatesInPlaceAndKeepsTheToken()
    {
        var created = _service.Upsert(null, "Ann", "dev").Data!;

        var updated = _service.Upsert(created.SessionToken, "Bob", "obs").Data!;

        Assert.Equal(created.Person.Guid, updated.Person.Guid);
        Assert.Equal(created.SessionToken, updated.SessionToken);
        Assert.Equal("Bob", _repository.Get(created.Person.Guid)!.Name);
        Assert.Equal("obs", _repository.Get(created.Person.Guid)!.PersonType);
    }

    [Fact]
    public void Upsert_NewPerson_StampsLastSeenAtWithNow()
    {
        var created = _service.Upsert(null, "Ann", "dev").Data!;

        Assert.Equal(_time.GetUtcNow(), _repository.Get(created.Person.Guid)!.LastSeenAt);
    }

    [Fact]
    public void Upsert_ExistingPerson_RestampsLastSeenAt()
    {
        var created = _service.Upsert(null, "Ann", "dev").Data!;
        _time.Advance(TimeSpan.FromHours(5));

        _service.Upsert(created.SessionToken, "Ann", "dev");

        Assert.Equal(_time.GetUtcNow(), _repository.Get(created.Person.Guid)!.LastSeenAt);
    }

    [Fact]
    public void Get_DoesNotStampLastSeenAt()
    {
        var created = _service.Upsert(null, "Ann", "dev").Data!;
        var stamped = _repository.Get(created.Person.Guid)!.LastSeenAt;
        _time.Advance(TimeSpan.FromHours(5));

        _service.Get(created.SessionToken);

        Assert.Equal(stamped, _repository.Get(created.Person.Guid)!.LastSeenAt);
    }
}
