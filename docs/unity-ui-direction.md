# Unity UI direction

Scope: MainMenu, Gameplay HUD, Pause/Resume and Results. Mode: Experience during play, Operate in menus/results.

Authority: the user's approved kitchen gameplay mockup and specification, not a new visual-world selection. This is implementation of a precisely specified approved surface; do not reopen a concept tournament. All documents remain in the CPS documentation folder at the user's request.

## Direction contract

THESIS: A playful kitchen catching game whose UI leaves the falling-object field readable. Menu and results make the one-minute loop obvious.

OWN-WORLD: Warm cream panels, cocoa-brown outlines and ink, orange engineer-mascot accent, subdued kitchen background; stronger colour belongs to food, danger and rising ranks. Chunky sprite edges and restrained UI borders, not glass or dashboard cards.

STORY: Choose or create a nickname, choose a difficulty, play with two hand inputs, see why the round ended and what happened, then restart or return. Statistics identify simulated input and unavailable Newtons plainly.

FIRST VIEWPORT: Gameplay keeps profile/three HP at upper left, remaining time at top centre, score/difficulty at upper right, small persistent rank badge near an edge, hand indicators below the running counter. Rank callout is short-lived and offset from incoming food; do not copy the concept's large central text if it blocks play. Results dim the same scene, prioritise outcome/score, then gameplay and left/right statistics, with clearly focused Restart and Main Menu buttons.

FORM: Approved warm-kitchen mockup, inherited directly; no seed key because this is a precisely specified already-approved implementation, not a new/open identity round. Temporary font preserves hierarchy until the user's font arrives. Source artwork is imported separately, never a full mockup used as gameplay.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Acceptance boundaries

- Gameplay reference contains a cheese-shaped illustrative item; the approved rules require Cake. Production uses Cake, Apple and Bread.
- Exact `IoTE` casing and nonmirrored helmet plate win over spelling/casing artefacts in the concept images.
- UI tests verify literal Thai strings; actual glyph/combining-mark appearance remains pending the supplied licensed font.
- No HTML/CSS surface exists inside Unity uGUI. Mechanical HTML/CSS detector findings are not evidence about this UI; use actual Unity/browser captures and a fresh visual review.
- Inspect shipped desktop viewports in a batched round, including 16:9, wide and tall letterboxed shapes. Mobile touch controls are not in scope.
- Review visual placement after production artwork is integrated, not against placeholders alone. Keep animation/performance acceptance separate from static screenshot acceptance.
