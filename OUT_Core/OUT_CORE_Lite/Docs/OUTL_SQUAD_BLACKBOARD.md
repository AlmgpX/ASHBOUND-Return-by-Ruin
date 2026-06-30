# OUTL Squad Blackboard

`OUTL_SquadBlackboard` is a shared data component for actors in one squad. It is not a new AI manager.

It stores:

- shared target id and last known position
- last squad order
- cover reservations so two members do not pick the same cover

Use `OUTL_SquadMember` on each actor and assign the same blackboard. `OUTL_SquadCommander` can still issue simple orders, but tactical actors read the visible order and convert it into input through the normal actor contract.

