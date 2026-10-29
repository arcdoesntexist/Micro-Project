# Background cleanup and catch-hitbox review

Date: 18 October 2026  
Reviewed range: `990fe9e08f3daa33da8533639b573f7d22e1e523..922c2ef`  
Verdict: **Approved**  
Counts: **Critical 0 / Important 0 / Minor 0**

What this change does: Removes the three approved baked hanging utensils while preserving the kitchen stage composition. Expands the catch zone to `2.0 x 0.8` world units, with depth `0.2`, and persists the same value in the production prefab and its authoring path.

## Findings

No actionable findings in this bounded change. Looks good. Ship.

## Code and test evidence

- `C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Input/PlayerController.cs:12` sets default half-extents `(1, .4, .1)`. The real `CatchZone` property at line 18 doubles these into world-space bounds. Movement speed, movement limits, catch offset, facing/scale treatment, and reset behavior remain unchanged.
- `C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Prefabs/GameplayRig.prefab:153` persists the identical half-extents. The neighboring mascot width and catch offset remain `.9` and `(0, .5, 0)`.
- `C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Editor/ProductionArtSetup.cs:391` writes the approved half-extents during `UpdateSharedRig`. The helper at line 627 uses `SerializedProperty.vector3Value` and applies the change; the existing prefab save at line 411 persists it. This does not alter unrelated production authoring behavior.
- `C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/EditMode/PresentationAuthoringTests.cs:83` checks the persisted production prefab through its actual `CatchZone.size`. `C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/PlayMode/FallingObjectTests.cs:37` checks a newly added runtime component. `C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/PlayMode/KeyboardPlayerTests.cs:126` checks actual bounds and preserves the subsequent scale, rotation, and translation assertions. These assertions test runtime bounds rather than just serialized literals. The authoring setter/save path was inspected; the tests do not execute a fresh production-art rebuild.
- `C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/PlayMode/FallingObjectTests.cs:153` correctly expects maximum X `6` for a player clamped at X `5` with catch half-width `1`. The lane at X `6` remains invalid for the conservative catcher-center reachability contract; the adjacent test at line 161 explicitly distinguishes edge coverage from a reachable centered path. The spawner continues to pass actual catch half-width and half-height into the existing spawn policy.
- The seven-file diff contains no changes to object sizes, character scale, scoring, difficulty configuration, movement, or speed. The only production code changes are the default and authoring persistence of the approved bounds.

Existing XML was parsed without running Unity: focused RED files each record the expected old `1.5 x .5` bounds failure; final `verification/catch-hitbox-full-edit.xml` reports **125/125 passed**, and `verification/catch-hitbox-full-play2.xml` reports **73/73 passed**. The affected EditMode and PlayMode reruns report **9/9** and **17/17**. The changed test cases are present and passed in the final suites. Earlier failures match the obsolete bounds expectations described in the implementation report. Final full-suite logs contain no matches for compiler errors, `NullReferenceException`, unhandled errors, or failed-run markers.

## Raster and provenance evidence

Inspected the actual shipping PNG with `view_image`, and compared it visually with the original raster stored at `.git/lfs/objects/fa/c3/fac398c28ffab2b26dbb511fefe4e1e506ec1d8b5a7227a2e1b10e385ace3731`.

The upper-left frying pan, wooden slotted spatula, and metal whisk are absent. The horizontal rail and all three hooks remain in their original locations. Replacement tile grout aligns with the adjacent wall, and warm sunlight and leaf shadows continue coherently. The window, cabinetry, shelves, jars, utensil crock, cutting boards, right saucepan, and countertop retain their framing and composition. No unwanted objects, text, characters, UI, food, or steam appear. This is visual composition verification, not a claim that every unaffected pixel is byte-identical after image editing.

The PNG header confirms exact **1672 x 941** dimensions. The shipping raster and `art/production/kitchen-background-v1.png` both hash to `448bc5cb75baf0f92b8efa8913e817b3a4f34b0e6eb9fe70d2fce4eaa501e291`, matching the reviewed LFS pointer. The provenance update records the localized edit, exact prompt, preserved dimensions, source/import copy, and retained Unity metadata. No `.meta` change is included in this diff.

Not checked: hands-on gameplay feel remains user acceptance; no Unity execution, browser build, or fresh art-authoring rebuild was performed for this review. This limitation is not a code defect.
