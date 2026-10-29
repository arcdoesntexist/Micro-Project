# Catch Hitbox Change Report

Date: 18 October 2026  
Branch: `codex/kitchen-catch`  
Commit: `c365ec1` (`Make player catch zone more forgiving`)

## Scope

Expanded the player catch zone from `1.5 x 0.5` to `2.0 x 0.8` world units by changing serialized half-extents from `(0.75, 0.25, 0.1)` to `(1.0, 0.4, 0.1)`. Movement limits, mascot scale, object sizes, scoring, difficulty configuration, and spawn speed were not changed.

The approved edited kitchen background was not modified, staged, or committed by this task. Production artwork authoring was not executed, so it could not overwrite the background raster.

## TDD evidence

The regression assertions were added before production changes.

- EditMode RED: `catch-hitbox-edit-red.xml` — `0/1` passed. Expected `(2.00, 0.80, 0.20)`; actual prefab catch-zone size was `(1.50, 0.50, 0.20)`.
- PlayMode RED: `catch-hitbox-play-red.xml` — `0/1` passed. Expected `(2.00, 0.80, 0.20)`; actual default `PlayerController` catch-zone size was `(1.50, 0.50, 0.20)`.
- The first EditMode GREEN attempt did not publish test results because `ProductionArtSetup` incorrectly used the object-reference helper for a `Vector3`; Unity reported `CS1503`. A dedicated serialized-vector helper corrected that implementation error before GREEN verification.
- EditMode focused GREEN: `catch-hitbox-edit-green2.xml` — `1/1` passed.
- PlayMode focused GREEN: `catch-hitbox-play-green.xml` — `1/1` passed.

## Regression verification

- Affected EditMode `PresentationAuthoringTests`: `catch-hitbox-affected-edit.xml` — `9/9` passed.
- First affected PlayMode run: `catch-hitbox-affected-play.xml` — `16/17`; the only failure was an old literal expecting max X `5.75` from the previous `0.75` half-width. The expected approved maximum was updated to `6.0` without changing validation logic.
- Affected PlayMode `FallingObjectTests` rerun: `catch-hitbox-affected-play2.xml` — `17/17` passed.
- Focused `KeyboardPlayerTests.CatchZoneTracksPositionWithoutFacingOrScaleChanges`: `catch-hitbox-keyboard-green.xml` — `1/1` passed after replacing its old size literal.
- Full EditMode: `catch-hitbox-full-edit.xml` — `125/125` passed.
- First full PlayMode run: `catch-hitbox-full-play.xml` — `72/73`; the only failure was the remaining old `1.5 x 0.5` size literal in `KeyboardPlayerTests`.
- Full PlayMode rerun: `catch-hitbox-full-play2.xml` — `73/73` passed.
- Final passing logs contained no matches for compiler errors, unhandled errors, `NullReferenceException`, or failed-run markers.

## Changed files

- `Assets/CPS/Input/PlayerController.cs`
- `Assets/CPS/Editor/ProductionArtSetup.cs`
- `Assets/CPS/Prefabs/GameplayRig.prefab`
- `Assets/CPS/Tests/EditMode/PresentationAuthoringTests.cs`
- `Assets/CPS/Tests/PlayMode/FallingObjectTests.cs`
- `Assets/CPS/Tests/PlayMode/KeyboardPlayerTests.cs`

`ProductionArtSetup.UpdateSharedRig` writes the same approved half-extents as the runtime default and production prefab, preventing future production-art authoring from reverting the catch zone.

## Preservation

The commit was created with an explicit six-file path list. `Assets/CPS/Art/Kitchen/kitchen-background-v1.png` and all pre-existing user changes remained unstaged. No reset, clean, checkout, stash, or add-all operation was used.
