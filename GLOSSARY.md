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
