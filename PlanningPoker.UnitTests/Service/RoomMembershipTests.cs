using PlanningPoker.UnitTests.Builders;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class RoomMembershipTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly RoomMembership _membership = Rounds.Membership();
    private readonly Guid _ann = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly Guid _cat = Guid.NewGuid();

    private RoomEntity RoomWith(params Guid[] members)
    {
        var room = new RoomEntity { Id = "blue-fox", OwnerGuid = members.Length > 0 ? members[0] : Guid.Empty, VotingEnabled = true };
        foreach (var member in members)
        {
            _membership.Join(room, member, $"conn-{member}", personType: PersonTypes.Developer);
        }

        return room;
    }

    [Fact]
    public void Join_AdmitsNewPersonAndOwnsEmptyRoom()
    {
        var room = new RoomEntity { Id = "blue-fox", OwnerGuid = Guid.NewGuid() };

        var joined = _membership.Join(room, _ann, "c1", personType: PersonTypes.Developer);

        Assert.True(joined.Admitted);
        Assert.True(joined.PersonAdded);
        Assert.Equal(_ann, room.OwnerGuid);
        Assert.Contains(_ann, room.MemberGuids);
        Assert.Contains("c1", room.MemberConnections[_ann]);
    }

    [Fact]
    public void Join_RefusesDisposedRoom()
    {
        var room = new RoomEntity { IsDisposed = true };

        var joined = _membership.Join(room, _ann, "c1", personType: PersonTypes.Developer);

        Assert.False(joined.Admitted);
        Assert.Empty(room.MemberGuids);
    }

    [Fact]
    public void Join_NewVoterWhileVotingBumpsRevision()
    {
        var room = RoomWith(_ann);
        var before = room.Revision;

        var joined = _membership.Join(room, _bob, "c2", personType: PersonTypes.Developer);

        Assert.True(joined.ResetCountdown);
        Assert.Equal(before + 1, room.Revision);
    }

    [Fact]
    public void Join_ObserverDoesNotBumpRevision()
    {
        var room = RoomWith(_ann);
        var before = room.Revision;

        _membership.Join(room, _bob, "c2", personType: PersonTypes.Observer);

        Assert.Equal(before, room.Revision);
    }

    [Fact]
    public void Join_SecondConnectionOfMemberAddsNoPerson()
    {
        var room = RoomWith(_ann);

        var joined = _membership.Join(room, _ann, "c2", personType: PersonTypes.Developer);

        Assert.False(joined.PersonAdded);
        Assert.Equal(2, room.MemberConnections[_ann].Count);
        Assert.Single(room.MemberGuids);
    }

    [Fact]
    public void Join_OffgridMemberReturnsOnlineAndKeepsTheirVote()
    {
        var room = RoomWith(_ann, _bob);
        room.Votes[_bob] = "5";
        _membership.Drop(room, _bob, Now);
        _membership.GoOffgrid(room, _bob);

        var joined = _membership.Join(room, _bob, "c3", personType: PersonTypes.Developer);

        Assert.True(joined.WasOffgrid);
        Assert.False(joined.ResetCountdown);
        Assert.Empty(room.OffgridGuids);
        Assert.Empty(room.DroppedAt);
        Assert.Equal("5", room.Votes[_bob]);
    }

    [Fact]
    public void Join_RestoresDepartedVoteOfOpenRound()
    {
        var room = RoomWith(_ann, _bob);
        room.Votes[_bob] = "8";
        _membership.Leave(room, _bob, LeaveReason.Deliberate);

        var joined = _membership.Join(room, _bob, "c3", personType: PersonTypes.Developer);

        Assert.True(joined.PersonAdded);
        Assert.Equal("8", room.Votes[_bob]);
        Assert.Empty(room.DepartedVotes);
    }

    [Fact]
    public void Join_DropsDepartedVoteAfterReveal()
    {
        var room = RoomWith(_ann, _bob);
        room.Votes[_bob] = "8";
        _membership.Leave(room, _bob, LeaveReason.Deliberate);
        room.VotingEnabled = false;
        room.Revealed = new RevealedVotesEntity();

        _membership.Join(room, _bob, "c3", personType: PersonTypes.Developer);

        Assert.DoesNotContain(_bob, room.Votes.Keys);
        Assert.Empty(room.DepartedVotes);
    }

    [Fact]
    public void Join_DropsDepartedVoteOfObserver()
    {
        var room = RoomWith(_ann, _bob);
        room.Votes[_bob] = "8";
        _membership.Leave(room, _bob, LeaveReason.Deliberate);

        _membership.Join(room, _bob, "c3", personType: PersonTypes.Observer);

        Assert.DoesNotContain(_bob, room.Votes.Keys);
        Assert.Empty(room.DepartedVotes);
    }

    [Fact]
    public void Return_ClearsDropAndOffgridAndReportsWhetherOffgrid()
    {
        var room = RoomWith(_ann);
        _membership.Drop(room, _ann, Now);

        Assert.False(_membership.Return(room, _ann));
        Assert.Empty(room.DroppedAt);

        _membership.Drop(room, _ann, Now);
        _membership.GoOffgrid(room, _ann);
        Assert.True(_membership.Return(room, _ann));
        Assert.Empty(room.OffgridGuids);
    }

    [Fact]
    public void Drop_KeepsFirstDropTimeAndForgetsConnections()
    {
        var room = RoomWith(_ann);

        _membership.Drop(room, _ann, Now);
        _membership.Drop(room, _ann, Now.AddMinutes(5));

        Assert.Equal(Now, room.DroppedAt[_ann]);
        Assert.DoesNotContain(_ann, room.MemberConnections.Keys);
        Assert.Contains(_ann, room.MemberGuids);
    }

    [Fact]
    public void GoOffgrid_ListsMemberAsOffgridAndKeepsThemAndTheirVote()
    {
        var room = RoomWith(_ann, _bob);
        room.Votes[_bob] = "5";
        _membership.Drop(room, _bob, Now);

        _membership.GoOffgrid(room, _bob);

        Assert.Contains(_bob, room.OffgridGuids);
        Assert.Contains(_bob, room.MemberGuids);
        Assert.Equal("5", room.Votes[_bob]);
    }

    [Fact]
    public void Leave_DeliberateKeepsVoteAsDepartedVote()
    {
        var room = RoomWith(_ann, _bob);
        room.Votes[_bob] = "3";

        var left = _membership.Leave(room, _bob, LeaveReason.Deliberate);

        Assert.True(left.PersonLeft);
        Assert.DoesNotContain(_bob, room.Votes.Keys);
        Assert.Equal("3", room.DepartedVotes[_bob]);
        Assert.DoesNotContain(_bob, room.MemberGuids);
        Assert.DoesNotContain(_bob, room.MemberConnections.Keys);
    }

    [Theory]
    [InlineData(LeaveReason.Timeout)]
    [InlineData(LeaveReason.OperatorRemoval)]
    public void Leave_TimeoutAndOperatorRemovalDiscardTheVote(LeaveReason reason)
    {
        var room = RoomWith(_ann, _bob);
        room.Votes[_bob] = "3";
        room.DepartedVotes[_bob] = "old";
        _membership.Drop(room, _bob, Now);
        _membership.GoOffgrid(room, _bob);

        _membership.Leave(room, _bob, reason);

        Assert.DoesNotContain(_bob, room.Votes.Keys);
        Assert.Empty(room.DepartedVotes);
        Assert.Empty(room.DroppedAt);
        Assert.Empty(room.OffgridGuids);
    }

    [Fact]
    public void Leave_BumpsRevision()
    {
        var room = RoomWith(_ann, _bob);
        var before = room.Revision;

        _membership.Leave(room, _bob, LeaveReason.Deliberate);

        Assert.Equal(before + 1, room.Revision);
    }

    [Fact]
    public void Leave_NonMemberChangesNothing()
    {
        var room = RoomWith(_ann);
        var before = room.Revision;

        var left = _membership.Leave(room, _bob, LeaveReason.Deliberate);

        Assert.False(left.PersonLeft);
        Assert.Equal(before, room.Revision);
    }

    [Fact]
    public void Leave_LastMemberDisposesRoom()
    {
        var room = RoomWith(_ann);

        var left = _membership.Leave(room, _ann, LeaveReason.Deliberate);

        Assert.True(left.Disposed);
        Assert.True(room.IsDisposed);
        Assert.Null(left.NewOwnerGuid);
    }

    [Fact]
    public void Leave_OwnerHandsOverToConnectedMemberOverOffgridOne()
    {
        var room = RoomWith(_ann, _bob, _cat);
        _membership.Drop(room, _bob, Now);

        var left = _membership.Leave(room, _ann, LeaveReason.Deliberate);

        Assert.Equal(_cat, left.NewOwnerGuid);
        Assert.Equal(_cat, room.OwnerGuid);
    }

    [Fact]
    public void Leave_OwnerHandsOverToAnyoneWhenNobodyIsConnected()
    {
        var room = RoomWith(_ann, _bob);
        _membership.Drop(room, _bob, Now);

        var left = _membership.Leave(room, _ann, LeaveReason.Timeout);

        Assert.Equal(_bob, left.NewOwnerGuid);
    }

    [Fact]
    public void Leave_NonOwnerKeepsOwner()
    {
        var room = RoomWith(_ann, _bob);

        var left = _membership.Leave(room, _bob, LeaveReason.Deliberate);

        Assert.Null(left.NewOwnerGuid);
        Assert.Equal(_ann, room.OwnerGuid);
    }

    [Fact]
    public void HandOver_OwnerNotLeavingKeepsRoomWhenNobodyElseIsConnected()
    {
        var room = RoomWith(_ann, _bob);
        _membership.Drop(room, _bob, Now);

        var newOwner = _membership.HandOver(room, ownerLeaving: false);

        Assert.Null(newOwner);
        Assert.Equal(_ann, room.OwnerGuid);
    }

    [Fact]
    public void HandOver_OwnerNotLeavingPassesToConnectedMember()
    {
        var room = RoomWith(_ann, _bob);

        var newOwner = _membership.HandOver(room, ownerLeaving: false);

        Assert.Equal(_bob, newOwner);
        Assert.Equal(_bob, room.OwnerGuid);
    }

    [Fact]
    public void Dispose_MarksRoomDisposed()
    {
        var room = RoomWith(_ann);

        _membership.Dispose(room);

        Assert.True(room.IsDisposed);
    }

    [Fact]
    public void Join_NewPersonTakesTheGivenTypeAsTheirRole()
    {
        var room = new RoomEntity { Id = "blue-fox", OwnerGuid = _ann };

        _membership.Join(room, _ann, "c1", personType: PersonTypes.Tester);

        Assert.Equal(PersonTypes.Tester, room.RoleOf(_ann));
    }

    [Fact]
    public void Join_ReturningMemberKeepsTheRoleTheRoomHas_NotTheProfilesType()
    {
        var room = RoomWith(_ann, _bob);
        room.Roles[_bob] = PersonTypes.Observer;
        var before = room.Revision;

        var joined = _membership.Join(room, _bob, "c3", personType: PersonTypes.Developer);

        Assert.False(joined.PersonAdded);
        Assert.Equal(PersonTypes.Observer, room.RoleOf(_bob));
        Assert.False(joined.ResetCountdown);
        Assert.Equal(before, room.Revision);
    }

    [Fact]
    public void Leave_RemovesTheRoleAndReportsIt()
    {
        var room = RoomWith(_ann, _bob);
        room.Roles[_bob] = PersonTypes.Sre;

        var left = _membership.Leave(room, _bob, LeaveReason.Deliberate);

        Assert.Equal(PersonTypes.Sre, left.PersonType);
        Assert.DoesNotContain(_bob, room.Roles.Keys);
    }

    [Fact]
    public void Leave_OfTheOwner_ReportsTheNewOwnersRole()
    {
        var room = RoomWith(_ann, _bob);
        room.Roles[_bob] = PersonTypes.Tester;

        var left = _membership.Leave(room, _ann, LeaveReason.Deliberate);

        Assert.Equal(_bob, left.NewOwnerGuid);
        Assert.Equal(PersonTypes.Tester, left.NewOwnerPersonType);
    }

    [Fact]
    public void Leave_OfSomeoneWhoIsNotAMember_ReportsNothing()
    {
        var room = RoomWith(_ann);

        var left = _membership.Leave(room, _cat, LeaveReason.Deliberate);

        Assert.False(left.PersonLeft);
        Assert.Equal(string.Empty, left.PersonType);
    }
}
