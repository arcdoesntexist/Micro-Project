# Result summary with optional details

Status: approved by the user on 2026-10-25. The user selected a main summary with a right-side details panel, closed initially.

## Approved integration follow-up

The user subsequently approved implementation in Unity and supplied `kitchen-catch-results-layout.json` and `kitchen-catch-results-text.json`; copies are in `verification/results-unity/`. This supersedes the prototype-only scope below for this integration. Preserve the supplied positions, replace plain difficulty text with the existing cocoa badge and one/two/three green/gold/orange stars matching Home, and add per-item catch collection to the real round snapshot. Keep existing score rules, Restart/Menu behavior, profile recording and Sheets payload intact. Item order is Apple, Bread, Cake, Can, BananaPeel. Missing historical detail must display unavailable rather than invented counts.

The illustrated typography stays FF Spoken for headings and actions. The numeric details use the built-in readable font for percent and unavailable-value coverage, matching the symbol fallback needed by the prototype. No new artwork or dependency is needed.

## Intent

Make a completed round easy to understand at a glance while preserving all existing statistics for students and instructors. Show which foods were caught, using recognizable game sprites and counts rather than another dense text line.

## Approaches

- Single panel with every statistic: no extra interaction, but too crowded once item counts are added.
- Separate Round / Input Stats tabs: more space, but hides the summary while viewing input statistics.
- Selected by the user: keep the summary visible and toggle a secondary panel beside it. Details begin closed; the main actions remain available when expanded.

## Content and interaction

- Main panel: player name, difficulty, Triumph or HP depleted, prominent score, Highest Rank, Max Combo, food sprites and caught counts by type, Restart and Main Menu.
- Food types: Apple, Bread, Cake. Show caught counts only, including zero counts; do not add per-type missed counts. Zero-count presentation is a proposed implementation default.
- Normal and Hard also show a separate group for trash eaten (can and banana peel), distinct from food. Easy hides the whole trash group and closes the space. Do not imply trash adds points.
- Details panel: played time, remaining HP, total food caught and missed, total trash eaten in Normal/Hard, left/right mean and maximum strength, pressed duration, simultaneous-press duration, balance, and mean/maximum force in N when available.
- Details toggles a panel on the right, never covers the summary or primary actions. Close control and Escape dismiss it; keyboard focus returns to the opener if it was inside the panel. No keyboard trap: this is a non-modal panel.
- Opening may shift the main panel to make room. Closing restores its resting position. Use restrained motion and respect reduced-motion preferences in the prototype.

## Visual constraints

Inherit the approved Home/Gameplay illustrated wood and cream panels, orange primary actions, cocoa text, FF Spoken Trial and existing kitchen/item assets. Landscape 16:9 stage, prototype reference 1600 x 900. Keep score and outcome more prominent than statistics; use the established rank identity. No replacement visual world or new generated artwork is required by this design.

## Scope and non-goals

Next deliverable is a Design Playground prototype using explicitly labeled sample data, not Unity implementation. Reuse existing playground layout-editing conventions where practical; keep its saved layout separate from Home/Gameplay. Preview both outcomes, all difficulties, zero catches, and expanded/collapsed details.

Do not change scoring, rank thresholds, profiles, Google Sheets, audio, or production Unity files. Existing Unity results store aggregate counts only; future integration needs per-type collection and snapshot handling. Historical results must not be presented as having known per-type counts. Force unavailable means a dash with Sensor not connected, not a fabricated measurement or zero.

## Acceptance and remaining builder choices

At 1280 x 720 and 1920 x 1080, verify readable labels, no overlap, stable main actions, correct Easy trash omission, consistent sample totals, keyboard opening/closing and no unavailable-force claims. Final spacing, concise English labels and subtle transitions are builder choices within the approved UI system. User approval of the prototype remains necessary before Unity integration.
