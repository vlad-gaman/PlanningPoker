# Planning Poker

A web app where a team estimates work by voting with cards in a shared room and revealing the votes together.

## Language

**Round**:
One cycle in a room: people vote, the votes are revealed, then cleared to start the next round.
_Avoid_: Game, session

**Reveal**:
The moment the votes in a room are shown to everyone, either after the countdown or when forced.
_Avoid_: Flip, show-down

**Round timer**:
The time elapsed since the room's last reveal, clear or creation. Casting or changing a vote does not reset it.
_Avoid_: Vote timer, idle timer, activity timer

**Abandoned**:
A room or person nobody is using any more, which the sweep removes from memory. A room is abandoned when it has no open connections and neither it was created nor anything in it changed within the abandon time. A person is abandoned when they have no open connections, no surviving room lists them as owner or member, and they were last seen more than the abandon time ago. "Last seen" is set when the person is created, upserted, or loses a connection, never by reading. The abandon time is 24 hours and the sweep runs every 10 minutes; both can be set in the `Sweep` appsettings section. This has nothing to do with the round timer, which only measures the current round.
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
What a person is in the room: Developer, Tester, SRE or Observer. Only Observer does not vote. Developer, Tester and SRE votes count in the All statistics, and each of those roles also has its own breakdown chart.
_Avoid_: Person type, position

**Owner**:
The member who can change the room settings and manage the room. The creator is the owner from creation, but a creator who has not joined is not a member: the first person admitted to a room with no owner among its members becomes owner. A creator who arrives later joins as a regular member and does not get ownership back. When the owner leaves, ownership passes to another member.
_Avoid_: Host, admin, creator (the creator is only the owner until someone else is admitted first)

**Member**:
A person who has joined a room and has at least one open connection to it. Creating a room does not make the creator a member; their first successful join does. A member leaves when their last connection closes, and the room is disposed when its last member leaves.
_Avoid_: Participant, user

**Operator**:
The single person who looks after the running app: they see the live rooms and people, and can close a room or remove a person, through the management page. An operator is not a member of any room and never votes; they cannot see votes. Closing a room tells everyone in it that an operator closed it.
_Avoid_: Admin, user, moderator, owner (the owner belongs to one room)
