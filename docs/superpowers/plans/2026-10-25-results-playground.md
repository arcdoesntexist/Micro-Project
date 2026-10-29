# Results Playground Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. User previously chose native execution; preserve that choice.

**Goal:** Preview the approved Result summary and toggleable right-side details in the existing Design Playground.

**Architecture:** Add an isolated static page with its own CSS, JavaScript and layout storage. Reuse existing raster atlas coordinates, font and playground conventions without refactoring Home or Gameplay. All session values are labeled sample data.

**Tech Stack:** HTML, CSS, vanilla JavaScript, existing Python static server, browser verification.

**Spec:** docs/superpowers/specs/2026-10-25-results-drawer-design.md

## Global Constraints

- Prototype only; no Unity, profile, Sheets, audio or gameplay-rule changes.
- Reference stage 1600 x 900; verify 1280 x 720 and 1920 x 1080 viewports.
- Preserve approved illustrated wood/cream/orange UI and FF Spoken Trial.
- Food caught by type only; Easy hides all trash content and unused space.
- Details initially closed, non-modal, never covers summary or primary actions.
- Unavailable force is a dash with Sensor not connected, never a fabricated measurement.
- No new dependency, generated artwork, commit or broad repository cleanup.

## Review Focus

1. Switching Normal/Hard to Easy while details are open must hide trash in both panels.
2. Zero catches and HP-depleted outcomes must not imply a victory or invent force values.
3. Long names, six-digit score and SSS must remain readable without clipping actions.
4. Closing details via Escape or its close button restores focus when focus was inside; hidden controls cannot receive Tab.
5. Invalid saved layout or unavailable localStorage must not prevent opening the prototype.

## Task 1: Result prototype and entry links

**Files:** create `playground/results.html`, `playground/results.css`, `playground/results.js`; modify only navigation links in `playground/index.html` and `playground/gameplay.html`; update `playground/README.md`.

**Interfaces:** page uses existing `art/production/home-ui-atlas-v1.png`, `items-atlas-v1.png`, kitchen art and `playground/FFSpokenTrial-Regular.ttf`. Store layout only under `kitchen-catch-results-layout-v1`. `setDetails(open)` owns visibility, aria-expanded and focus restoration. `renderResult(sample)` derives item totals and score from sample counts using Apple 10, Bread 15, Cake 20; no combo multiplier.

- [ ] Read current full playground sources and relevant impeccable craft guidance before editing; record the inherited visual direction in a development-only results surface brief.
- [ ] Build main summary and right-side details, using live labels over existing artwork. Keep Restart/Main Menu visibly labeled as prototype navigation, not real session writes.
- [ ] Add preview controls for difficulty, outcome, sample preset (standard/zero/long-name-high-score), and details toggle. Use internally consistent presets and a disclosed keyboard sample with unavailable force.
- [ ] Add Arrange mode for the two panel bounds, numeric X/Y/W/H, Save/Reset/Export, validation within the stage and independent storage. Opening/closing uses separate valid panel positions so the normal default never overlaps.
- [ ] Add scoped entry links and instructions; do not alter existing Home/Gameplay saved layout keys or controls.

## Task 2: Browser verification and handoff

**Files:** create `verification/results-playground/RESULTS.md` and screenshots; update this checklist.

- [ ] Run JavaScript syntax validation and any existing playground checks. Check no duplicate element IDs and confirm all referenced local assets exist.
- [ ] In browser test the default collapsed state, Details toggle/close/Escape and keyboard focus. Assert details is hidden initially, aria-expanded changes, hidden fields are not focusable, and primary actions remain visible.
- [ ] Test Easy/Normal/Hard, both outcomes and zero catches. Assert no trash in Easy (including details), score equals counted food points, missing-food is aggregate only, and force reads unavailable.
- [ ] Test long-name/high-score/SSS preset; save/reload/reset layout and malformed saved layout fallback. Test storage failure with a controlled fixture if tool capabilities allow; otherwise explicitly report unverified.
- [ ] Inspect a single batched screenshot round at both target sizes with collapsed/expanded and Easy/Hard states; batch any material fixes and confirm once. Do not add unrelated polish.
- [ ] Obtain the scoped finish review required by impeccable with the spec and screenshots; document findings and any verification limitations. Save evidence and hand over the working Result URL, without claiming Unity integration.

## Plan self-review

Spec content, optional details, conditional trash, sample integrity, layout editing, keyboard access and bounded verification are covered above. Existing rank naming and art must be read from current implementation rather than older design prose. No new visual-world selection is necessary: this extends the approved game UI.
