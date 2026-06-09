# Companion Follow Priority Update

Date: 2026-06-09

## Summary

Companions now adjust their follow-priority distance based on what the player is doing.

- Player moving: companions prioritize returning to formation sooner, so the group stays tighter while traveling.
- Player recently attacking: companions allow a longer leash before abandoning combat positioning, so they do not snap back too aggressively during fights.
- Player idle: companions use a middle-distance default.

## Runtime Rules

- The companion follow logic reads the player's Rigidbody2D velocity to detect movement.
- The player attack controller exposes a short `IsRecentlyAttacking` window after each attack.
- Companion combat pursuit is skipped when distance to the player exceeds the dynamic priority-follow distance.
- Formation, door traversal, and village boundary handling continue to use the existing follow path.
