namespace PlanningPoker.Service.Models;

/// <summary>What <see cref="Services.IRoomMembership.Leave"/> changed in the room.</summary>
/// <param name="PersonLeft">False when the person was not a member, in which case nothing changed.</param>
/// <param name="Disposed">True when the last member left and the room was disposed.</param>
/// <param name="NewOwnerGuid">Who the room was handed to, when the leaving person owned it.</param>
/// <param name="PersonType">The role the leaving person had in the room, which the leave removes; empty when they were not a member.</param>
/// <param name="NewOwnerPersonType">The new owner's role in the room; null when the room was not handed over.</param>
public sealed record MembershipLeft(bool PersonLeft, bool Disposed, Guid? NewOwnerGuid, string PersonType, string? NewOwnerPersonType);
