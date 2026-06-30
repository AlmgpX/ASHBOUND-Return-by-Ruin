# OUTL Cover System

`OUTL_CoverPoint` registers itself with `OUTL_CoverRegistry` on enable. Runtime code queries the registry; it does not scan the scene every tactical tick.

Cover query uses:

- seeker position
- threat position
- radius
- optional sector id
- weapon role
- visibility mask

Reservations are explicit through `OUTL_CoverReservation`. Squad actors should reserve cover through `OUTL_SquadBlackboard` to avoid duplicate cover picks.

