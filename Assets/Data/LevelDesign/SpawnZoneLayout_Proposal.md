# Spawn Zone Layout Proposal (Campaign + Infinite)

This proposal matches your current runtime components:
- `SpawnZone` supports `Rectangle`/`Circle` and `ZoneType` = `Enemy`, `PowerUp`, `Both`.
- Zones can enforce `minDistanceFromPlayer`, optional `maxDistanceFromPlayer`, and `mustBeOffScreen`.

The goal is a readable, fair arena that:
- telegraphs threat directions (clear “lanes” around the player),
- creates safe/unsafe pockets for risk–reward power-up decisions,
- supports both curated (campaign) and scaling (infinite) wave flow.

---

## Coordinate & Scale Assumptions

- World units: Unity 2D world space (X right, Y up).
- Arena bounds for this layout: **Rect(-30, -20, 60, 40)**.
  - Center at (0, 0).
  - This plays nicely with a camera that can be bounded (recommended).

### Recommendation (important)
If the player/camera are not bounded, the “map layout” becomes less meaningful because `mustBeOffScreen` is evaluated relative to the *current* camera view. For campaign/infinite that want authored pacing, consider enabling camera boundaries:
- In `SmartCameraConfig`, set `useBoundaries = true`
- Set `worldBoundaries = Rect(-30, -20, 60, 40)` (or larger, but keep consistent)

---

## Top-Down Map (ASCII)

Legend:
- `E#` = Enemy zone
- `P#` = Power-up zone
- `B#` = Both (enemy + power-up)
- `S` = Suggested player start / early safe space

```
Y=+20  +--------------------------------------------------------------+
       |          E7 (NW)                 E1 (N)            E8 (NE)    |
       |       (circle)               (rect band)           (circle)   |
       |                                                              |
       |   B1 (left risk/reward)              B2 (right risk/reward)   |
       |        (circle)                            (circle)           |
       |                                                              |
Y=  0  |   E5 (W)        P1 (safe-ish)   S   P2 (safe-ish)       E6 (E)|
       | (rect band)       (circle)         (circle)              (band)|
       |                                                              |
       |                 P3 (contested)                               |
       |                    (circle)                                 |
       |                                                              |
       |          E9 (SW)                 E2 (S)            E10 (SE)   |
       |       (circle)               (rect band)           (circle)   |
Y=-20  +--------------------------------------------------------------+
           X=-30                     0                       X=+30
```

Design intent:
- The outer ring (E1/E2/E5/E6 + corners) produces consistent “pressure from the edges” without unfair spawns on top of the player.
- The inner pair of **Both** zones (B1/B2) create optional risk: enemies can appear there, but power-ups can also appear there.
- Power-up zones P1/P2 are biased toward the midline for accessibility and to avoid “dead runs” to corners.
- P3 is intentionally *slightly* more contested to create mid-game decision points.

---

## Zone Specs (Ready to Create in Scene)

All positions are the SpawnZone GameObject’s `transform.position`.

### Enemy Zones (outer pressure)
These should generally keep `mustBeOffScreen = true`.

| ID  | Type  | Shape | Position (x,y) | Size / Radius | minDist | maxDist | mustBeOffScreen | Weight |
|-----|-------|-------|----------------|---------------|---------|---------|-----------------|--------|
| E1  | Enemy | Rect  | (0, 18)        | size (54, 6)  | 7       | 0       | true            | 1.2    |
| E2  | Enemy | Rect  | (0, -18)       | size (54, 6)  | 7       | 0       | true            | 1.2    |
| E5  | Enemy | Rect  | (-27, 0)       | size (6, 34)  | 7       | 0       | true            | 1.0    |
| E6  | Enemy | Rect  | (27, 0)        | size (6, 34)  | 7       | 0       | true            | 1.0    |
| E7  | Enemy | Circle| (-24, 14)      | r = 5         | 8       | 0       | true            | 0.8    |
| E8  | Enemy | Circle| (24, 14)       | r = 5         | 8       | 0       | true            | 0.8    |
| E9  | Enemy | Circle| (-24, -14)     | r = 5         | 8       | 0       | true            | 0.8    |
| E10 | Enemy | Circle| (24, -14)      | r = 5         | 8       | 0       | true            | 0.8    |

Notes:
- The “bands” (E1/E2/E5/E6) stabilize pacing: enemies tend to come from cardinal directions, which is readable.
- The corner circles add variation and prevent predictability without feeling random.

### Mixed Zones (risk–reward pockets)
Use `ZoneType = Both`.

| ID | Type | Shape  | Position (x,y) | Size / Radius | minDist | maxDist | mustBeOffScreen | Weight |
|----|------|--------|----------------|---------------|---------|---------|-----------------|--------|
| B1 | Both | Circle | (-14, 8)       | r = 5         | 5       | 0       | false           | 0.6    |
| B2 | Both | Circle | (14, 8)        | r = 5         | 5       | 0       | false           | 0.6    |

Notes:
- `mustBeOffScreen = false` here is intentional: these are “visible-threat” pockets near the midline.
- If these feel too punishing early, drop their weights or raise `minDist`.

### Power-up Zones (recovery + tempo)
Use `ZoneType = PowerUp`.

| ID | Type    | Shape  | Position (x,y) | Size / Radius | minDist | maxDist | mustBeOffScreen | Weight |
|----|---------|--------|----------------|---------------|---------|---------|-----------------|--------|
| P1 | PowerUp | Circle | (-10, 0)       | r = 4         | 2       | 0       | false           | 1.0    |
| P2 | PowerUp | Circle | (10, 0)        | r = 4         | 2       | 0       | false           | 1.0    |
| P3 | PowerUp | Circle | (0, -8)        | r = 4         | 3       | 0       | false           | 0.8    |

Notes:
- Power-ups should usually be allowed on-screen (otherwise players may never see them spawn).
- Keeping power-ups near the midline reduces “empty traversal time” and keeps the game aggressive.

---

## Campaign Mode: 10 Levels + Boss (how to use this single map)

Rather than building 10 totally different maps, you can keep **one arena** and change **which zones are active / their weights** per level. This preserves mastery (“I know this space”) while still escalating difficulty.

Suggested progression (use as a tuning guide):

1. **Level 1 (onboarding)**: enable E1, E2 only. Enable P1/P2. Disable B1/B2 and corners.
2. **Level 2**: add E5/E6 at low weight (0.5). Keep strong power-up access.
3. **Level 3**: enable corner zones E7/E8 (top corners only). Introduce first shooter enemy in waves.
4. **Level 4**: enable B1 (left pocket) at low weight (0.3) → teaches risk pockets.
5. **Level 5**: enable B2 (right pocket). Slightly reduce wave-complete power-up chance.
6. **Level 6**: enable all corners. Increase enemy zone weights modestly (+10–15%).
7. **Level 7**: raise `minDistanceFromPlayer` slightly down (e.g., 7→6) to increase pressure.
8. **Level 8**: increase mixed-zone weight (0.6→0.8) to force sharper positioning.
9. **Level 9**: reduce P1/P2 weights (1.0→0.8) so power-ups skew more contested.
10. **Level 10 (pre-boss)**: full layout, highest weights on E1/E2, and mixed zones active.

### Boss Level (same map, different rules)
- Disable *most* enemy zones (or reduce to 2) so the boss is the main focus.
- Keep **one** power-up zone active (P3) so recovery is possible but not constant.
- Optional: keep one mixed pocket (B1 or B2) active for adds, if the boss needs support pressure.

---

## Infinite Mode: Difficulty Scaling Using the Same Layout

Use the full layout from the start (or unlock corners after a few waves) and scale with:
- **Spawn counts** per wave
- **Weights** (increase outer ring weights first; mixed zones later)
- **Power-up availability** (slightly lower over time, but avoid starving the player)

Boss cadence recommendation:
- Every **5–7 waves** (variable) spawn a boss.
- On boss waves: temporarily reduce normal enemy spawns, or restrict enemy zones to the outer ring so the boss fight reads cleanly.

---

## Practical Setup Checklist (in Unity)

1. Create an empty GameObject `SpawnZones` at (0,0,0).
2. Under it, create child empties named `E1`, `E2`, ..., `P3`.
3. Add `SpawnZone` to each child and input:
   - `shape`, `zoneType`, `size/radius`
   - `minDistanceFromPlayer`, `mustBeOffScreen`, `weight`
4. For power-up zones, ensure `mustBeOffScreen = false`.
5. Define arena borders (recommended for a multi-screen arena):
       - Add an empty GameObject `ArenaBounds` at (0,0,0)
       - Add `ArenaBounds2D` and set `Bounds = Rect(-30, -20, 60, 40)` (or your preferred size)
       - This will auto-create 4 `BoxCollider2D` walls so the player/enemies (Rigidbody2D) can’t leave the arena.
6. (Recommended) In your `SmartCameraConfig`, enable boundaries to match the same arena bounds.

If you want, I can also add an **editor utility** that creates these SpawnZone GameObjects automatically from a single config file, so you can generate/iterate layouts fast.