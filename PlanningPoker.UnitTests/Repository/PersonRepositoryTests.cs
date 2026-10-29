using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Repository;

public class PersonRepositoryTests
{
    private static PersonEntity NewPerson(Guid? guid = null) => new() { Guid = guid ?? Guid.NewGuid(), Name = "Ann", PersonType = "dev" };

    [Fact]
    public void Get_Unknown_ReturnsNull()
    {
        Assert.Null(new PersonRepository().Get(Guid.NewGuid()));
    }

    [Fact]
    public void TryAdd_ThenGet_ReturnsStoredEntity()
    {
        var repository = new PersonRepository();
        var person = NewPerson();

        Assert.True(repository.TryAdd(person));
        Assert.Same(person, repository.Get(person.Guid));
    }

    [Fact]
    public void TryAdd_Duplicate_ReturnsFalse()
    {
        var repository = new PersonRepository();
        var person = NewPerson();
        repository.TryAdd(person);

        Assert.False(repository.TryAdd(NewPerson(person.Guid)));
    }

    [Fact]
    public void Remove_RemovesOnlyExisting()
    {
        var repository = new PersonRepository();
        var person = NewPerson();
        repository.TryAdd(person);

        Assert.True(repository.Remove(person.Guid));
        Assert.False(repository.Remove(person.Guid));
        Assert.Null(repository.Get(person.Guid));
    }

    [Fact]
    public void Update_Unknown_ReturnsFalse()
    {
        var found = new PersonRepository().Update(Guid.NewGuid(), _ => 1, out var result);

        Assert.False(found);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Update_MutatesStoredPersonAndReturnsResult()
    {
        var repository = new PersonRepository();
        var person = NewPerson();
        repository.TryAdd(person);

        var found = repository.Update(person.Guid, entity =>
        {
            entity.Name = "Bob";
            return entity.Name;
        }, out var name);

        Assert.True(found);
        Assert.Equal("Bob", name);
        Assert.Equal("Bob", repository.Get(person.Guid)!.Name);
    }

    [Fact]
    public void Update_ConcurrentCallers_AreAtomic()
    {
        var repository = new PersonRepository();
        var person = NewPerson();
        repository.TryAdd(person);

        Parallel.For(0, 500, _ => repository.Update(person.Guid, entity =>
        {
            entity.Connections[Guid.NewGuid().ToString()] = "room";
            return 0;
        }, out _));

        Assert.Equal(500, repository.Get(person.Guid)!.Connections.Count);
    }
}
