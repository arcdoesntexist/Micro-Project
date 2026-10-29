# Kitchen Catch local Versus

Status: approved for Micro-Project implementation, 2026-10-28.

## Intent and agreed scope

Add local two-player competitive split-screen to the existing Unity game, retaining Single Player. University demonstrators share one computer. Home offers Single Player / Versus. Each Versus player can select an existing local profile or create a new one using the existing nickname rules. Require two distinct profile IDs, not necessarily distinct display names. Player 1 uses A/D; Player 2 uses Left/Right arrows. Later STM32/ESP32 controllers are planned; this delivery uses keyboard only.

Both fields use the existing kitchen and decorations. This is an endless survival match, not a sixty-second round. Food and trash spawn identically in both fields. Difficulty starts gently and increases with elapsed active play time, emphasizing trash avoidance and food scoring. Missed food does not cost HP; caught trash does. A player's death ends both fields immediately and the survivor wins. The user will provide a new competitive background track later; use existing gameplay music as the temporary fallback and expose a separate Versus music assignment.

## Approaches and choice

The user selected adding Versus alongside Single Player over replacing the whole game with two-player rules. Implement a dedicated Versus scene/match coordinator, reusing existing input snapshots, movement, sprites, score/rank logic and presentation where compatible. One shared spawn schedule feeds two isolated fields; two independently evolving spawners are rejected because they can diverge. Do not run two copies of the existing application/profile singleton or single-player scene flow.

## Proposed defaults requiring design approval

- Three HP per player; caught trash costs one HP. No health recovery or new food power-ups.
- Resolve both fields for the same simulation step before determining the winner. If both reach zero HP in that step, declare a draw; score does not override survival outcome.
- Keep existing Apple/Bread/Cake points and rank thresholds. Missed food resets combo as it does today, but does not remove points or HP. Caught trash resets combo. Trash missed has no penalty.
- Versus has one automatic progression rather than Easy/Normal/Hard selection. Elapsed time and progression tier replace the countdown timer/difficulty badge during play.
- Results show winner/draw and side-by-side per-profile scores, survival duration, remaining HP, highest rank, max combo and caught item counts, with optional existing input statistics. Keyboard force remains unavailable in N.
- Persist completed Versus outcomes locally in a separate record namespace, keyed by a match ID and both profile IDs. Keep single-player score/ranking records separate. Upload a completed match to the Micro Google Apps Script endpoint as one `mode: "multiplayer"` payload containing both players; the endpoint writes adjacent P1/P2 rows to the separate `Multiplayer` sheet.

## Screen flow

Home mode choice leads to a Versus preparation view with Player 1 and Player 2 profile selectors and create actions, control hints, Back and Start. Profile creation selects the new profile for the initiating slot. Start requires two valid distinct IDs. Do not allow deletion/renaming during an active match; snapshot the names/IDs at start. Existing profile management stays accessible from Home.

Start runs one shared 3-2-1 countdown. Each side has independent mascot, HP, score, combo and movement limits. Use equal left/right panels with readable HUDs, a shared elapsed-time/tier label and Pause. Fit a full uniformly scaled 16:9 kitchen field within each half rather than horizontally squashing artwork; remaining vertical space may hold HUD/control labels. Match both fields' effective travel distances, catch geometry and relative spawn coordinates.

Pause and focus loss stop both simulations, spawn schedule and active time. Resume uses one shared countdown. Back to Main Menu abandons the match without recording results. On death freeze both fields, allow the existing death/winner presentation, then show match results. Restart keeps both profiles but creates a new match ID and shared random seed. A new seed changes the next match while both players within it receive the same sequence.

## Simulation and challenge

Use one match clock and a single generated stream of spawn records containing item type, normalized horizontal position, spawn time and fall parameters. Instantiate a copy per field; catching or missing an item on one side must not alter the shared generator or the other side. Process death only after both players' outcomes for that step have been applied, avoiding player-update-order advantage. No further scoring after terminal state. Bound active objects and release resolved objects throughout long sessions.

Builder-selected initial tuning, editable in Inspector: start near existing Normal fall speed, roughly 1.2 seconds between spawns and 20% trash; increase challenge approximately every 20 active seconds toward about 60% trash and a bounded minimum interval. Final speeds/intervals depend on actual catch widths and player speed. Preserve reaction time and a reachable safe route; never deliberately create unavoidable simultaneous trash walls. Existing spawn fairness assumes food-catching obligations and must be assessed against the new survival rules, not copied blindly. Increasing difficulty reaches a safe cap while the match continues indefinitely.

## Audio and implementation boundaries

Use one scene music source and existing global audio settings. Versus music uses an optional assigned AudioClip and falls back to existing gameplay music. Do not play two background tracks. Food/hurt reactions remain per player; terminal presentation should not stack competing full-volume victory/death music. Exact terminal audio priority and UI spacing are builder choices.

The existing input interface is the future controller seam. Keyboard key groups must be independently configurable, preserving the current combined key mapping in Single Player. No serial package, COM port discovery, firmware protocol, network multiplayer or hardware calibration in this phase. Serial integration will need a subsequent platform decision: the current project targets WebGL, so a desktop serial implementation cannot be assumed to work in the browser.

Unity source for this implementation lives at `C:/Users/Arc/Downloads/Micro/Unity`; the original CPS project remains separate. The Micro workspace is not a Git repository. Do not initialize Git, publish, or push. Keep existing single-player and profile migration behavior intact.

## Acceptance

- Existing Single Player still plays with its current controls, timing, HP rules and results.
- Both slots can select/create profiles; duplicate IDs and missing selections block Start.
- P1 and P2 move independently, including simultaneous key presses.
- Both sides receive the same item types, relative positions, timings and fall speeds despite different catches.
- Missed food leaves HP unchanged; caught trash damages only the relevant player.
- Match passes 60 seconds, progresses based on active time, pauses consistently and ends once on first death, with a draw on simultaneous death.
- Rematch resets all per-match state and records once; menu abandonment records nothing. Versus persistence cannot pollute Single Player rankings or Single Player sheet rows. A completed match uploads once with the same match ID used for local duplicate protection.
- At 1280x720 and 1920x1080, both fields and HUDs remain usable with recognizable sprites and no camera cross-rendering. User playtesting determines final difficulty feel; do not claim measured 60 FPS without profiling.
- Music fallback works before the replacement track is supplied and respects global volume/music settings.

Next step: implement the approved plan with focused checks of independent input, mirrored spawning, match outcomes, dual-row upload and single-player regression.
