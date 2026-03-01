# Tool Crate — Tool Balance Config

> Quick-reference for all 8 tools. Update this when tweaking ToolData SO values.

## Tool Stats Table

### Tier 1 — Weight 100

| Tool | Type | Damage | Damage 2 | Cooldown | Range | Pierce | Notes |
|------|------|--------|-----------|----------|-------|--------|-------|
| **Hammer** | Melee (single) | 8 | — | 0.7s | 3u | No | Simple slam. OverlapBox hit. |
| **Screwdriver** | Ranged (projectile) | 4 | — | 0.3s | 10u | No | Fires spinning screw projectile. |
| **Tape Measure** | Melee (2-phase) | 3 (extend) | 5 (retract) | 0.5s | 4u | Yes | Phase 1: extend forward. Phase 2: retract with more damage. |
| **Nail Gun** | Ranged (rapid-fire) | 1 | — | 0.1s | 12u | No | Very fast, low-damage projectiles. |

### Tier 2 — Weight 95

| Tool | Type | Damage | Damage 2 | Cooldown | Range | Pierce | Notes |
|------|------|--------|-----------|----------|-------|--------|-------|
| **Blowtorch** | Ranged (beam) | 2 | — | 0.1s | 3u | No | Continuous damage beam. |
| **Vacuum** | Utility (2-phase) | 0 (suck) | 10 (shoot) | 0s | 4u | No | Phase 1: suck small enemies (1s). Phase 2: shoot them as projectiles. |
| **Magnet** | Utility (pull) | 1 | — | 0.1s | 50u | Yes | Pulls enemies toward player. |

### Tier 3 — Weight 90

| Tool | Type | Damage | Damage 2 | Cooldown | Range | Pierce | Notes |
|------|------|--------|-----------|----------|-------|--------|-------|
| **Chainsaw** | Melee (hold) | 5 | — | 0.1s | 3u | No | Rapid hits while held. |

## 2-Phase Tool Animator Setup

Tools with 2 attack phases use **one shared animator controller** with a `SecondaryPhase` bool:

### Animator Parameters
| Parameter | Type | Purpose |
|-----------|------|---------|
| `Attack` | Trigger | Start the attack |
| `SecondaryPhase` | Bool | `true` = transition to phase 2 state |

### Animator States & Transitions
```
Idle ──(Attack trigger)──► Phase1Attack ──(SecondaryPhase=true)──► Phase2Attack ──(exitTime)──► Idle
                           └──(exitTime, single-phase tools)──► Idle
```

### ToolData Fields
| Field | Purpose |
|-------|---------|
| `attackAnimator` | Single animator controller with Idle / Phase1Attack / Phase2Attack states |
| `damage` | Phase 1 damage |
| `secondaryDamage` | Phase 2 damage |

### Code Flow
1. Player attacks → `PlayerToolHandler` triggers `Attack` (phase 1 clip plays)
2. Tool script detects phase change → calls `OnRequestSecondaryAnimation`
3. `PlayerToolHandler` sets `SecondaryPhase = true` → animator transitions to Phase2Attack
4. Tool script finishes → calls `OnRequestPrimaryAnimation`
5. `PlayerToolHandler` sets `SecondaryPhase = false` → animator returns to Idle

## Vacuum Extended Config (VacuumToolConfig SO)

| Param | Value | Description |
|-------|-------|-------------|
| suckDuration | 1.0s | How long the suck phase lasts |
| suckRange | 3u | Column width for detecting enemies |
| shootSpeed | 10 | Projectile speed of shot enemies |
| shootDamage | 10 | Damage per shot enemy (overrides secondaryDamage) |
| shootInterval | 0.15s | Delay between each enemy shot |

## Weapon Hold Positions (per ToolData SO)

| Tool | holdDistance | yOffset |
|------|-------------|---------|
| Hammer | 0.5 | 0.2 |
| Chainsaw | 1.0 | 0.0 |
| Screwdriver | 1.0 | 0 |
| Tape Measure | 0.5 | 0.0 |
| Nail Gun | 1.0 | 0.0 |
| Blowtorch | 1.0 | 0.0 |
| Vacuum | 0.5 | 0.0 |
| Magnet | 0.5 | 0.0 |
