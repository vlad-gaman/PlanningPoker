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
