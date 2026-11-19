# Planning Poker

A web app where a team estimates work by voting with cards in a shared room and revealing the votes together.

## Language

**Room**:
The shared space where a team votes: it has an owner, members, settings and a series of rounds. It is disposed when its last member leaves, and a server restart restores it from the store.
_Avoid_: Session, game, lobby

**Round**:
One cycle in a room: people vote, the votes are revealed, then cleared to start the next round. Any member may reveal or clear, not only the owner, as with physical cards.
_Avoid_: Game, session

**Round history**:
The list, newest first, of the rounds a room has revealed, each with its round number and the average of its votes (a dash when no vote was a number). A round that is cleared without a reveal is not listed. It belongs to the room: everyone in the room sees it, it survives a reconnect and a server restart, and it goes when the room does. It keeps the last 50 rounds and never shows who voted what.
_Avoid_: Past votes, vote log, archive

**Reveal**:
The moment the votes in a room are shown to everyone, either after the countdown or when forced. Any member can force it, and any member can clear the votes afterwards; the owner has no special say here.
_Avoid_: Flip, show-down

**Round timer**:
The time elapsed since the room's last reveal, clear or creation. Casting or changing a vote does not reset it.
_Avoid_: Vote timer, idle timer, activity timer

**Abandoned**:
A room or person nobody is using any more, which the sweep removes from memory. A room is abandoned when it has no open connections and neither it was created nor anything in it changed within the abandon time. A person is abandoned when they have no open connections, no surviving room lists them as owner or member, and they were last seen more than the abandon time ago. "Last seen" is set when the person is created, upserted, or loses a connection, never by reading. The abandon time is 24 hours and the sweep runs every 10 minutes; both can be set in the `Sweep` appsettings section. A room nobody ever joined does not wait for this: the presence sweep deletes it `Presence:NeverJoinedRoomLifetime` (1 minute) after it was created. This has nothing to do with the round timer, which only measures the current round.
_Avoid_: Idle, stale, expired, timed out

**Online**:
A person with a live connection to the room, or one who dropped less than the grace period (10 seconds) ago, so a refresh goes unnoticed. Observers count too.
_Avoid_: Connected, present

**Offgrid**:
A person who is still a member of the room but has had no connection for longer than the grace period. They keep their vote and are shown to others as offgrid, and "everyone voted" is decided without them. A person who returns within the removal window is restored exactly as they were. An owner who goes offgrid hands the room to a connected member at once and does not get it back.
_Avoid_: Away, disconnected, idle

**Removed**:
A person whose membership ended because they stayed offgrid for the removal window (10 minutes, the `Presence` appsettings section). Their vote is discarded and they can rejoin as a new person. Leaving a room on purpose removes the person at once. An **Operator** can also remove a person at once, online or offgrid; that is **removed by operator**, and only the removed person is told it was an operator.
_Avoid_: Exited, kicked

**Restored room**:
A room that came back from the store after a server restart (SQLite file, `Persistence` appsettings section; on App Service point it at `/home`). Its people, votes and round state are kept and every person starts offgrid until they reconnect. A revealed round stays revealed; a countdown that was running is not resumed. A restored room is deleted once all its people are removed or it has had no activity for the `Sweep:AbandonedAfter` period (24 hours).
_Avoid_: Recovered, reloaded

**Voter**:
A person who can vote in a round: a developer, a tester or an SRE.
_Avoid_: Player, participant

**Observer**:
A person in the room who cannot vote. Observers are listed after all voters in the participants table.
_Avoid_: Spectator, viewer

**Role**:
What a person is in the room: Developer, Tester, SRE or Observer. Only Observer does not vote. Developer, Tester and SRE votes count in the All statistics, and each of those roles also has its own breakdown chart. The room holds each member's role: a person's profile gives the role they start with when they join, and changing it in the room changes the room only, so it is not carried to the next join. A role change while voting is open cancels a running countdown; a vote already cast stays, and the reveal counts it under the role held at that moment.
_Avoid_: Person type, position

**Owner**:
The member who can change the room settings and transfer ownership. Revealing and clearing votes are not owner powers: any member can do them. The creator is the owner from creation, but a creator who has not joined is not a member: the first person admitted to a room with no owner among its members becomes owner. A creator who arrives later joins as a regular member and does not get ownership back. When the owner leaves, ownership passes to another member.
_Avoid_: Host, admin, creator (the creator is only the owner until someone else is admitted first)

**Member**:
A person who has joined a room and has at least one open connection to it. Creating a room does not make the creator a member; their first successful join does. A member leaves when their last connection closes, and the room is disposed when its last member leaves.
_Avoid_: Participant, user

**Operator**:
The single person who looks after the running app: they see the live rooms and people, and can close a room or remove a person, through the management page. An operator is not a member of any room and never votes; they cannot see votes. Closing a room tells everyone in it that an operator closed it. Failed sign-ins lock the Operator account for everyone, not just the sender (see [ADR 0002](docs/adr/0002-operator-sign-in-lockout-is-account-wide.md)). A failed or refused sign-in is noted for the Operator with the sender's IP address and, as reported by their browser, the name, role and room settings it has saved; that browser data is a hint, never verified.
_Avoid_: Admin, user, moderator, owner (the owner belongs to one room)

**Takeover**:
Opening a room in a second tab of the same browser makes the older tab give the room up, with a message. It happens in the browser only: another browser or device keeps its connection.
_Avoid_: Duplicate session
