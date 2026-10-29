# Task 5 fix round 1 re-review

## Finding verdicts

1. **Preflight can approve a different catcher than the player the controller moves** — **ADDRESSED**. `Assets/CPS/Flow/GameSessionController.cs:47-49` rejects distinct non-null player references before delegating to the spawner's existing validation, which rejects a null spawner player. `BeginRound` uses that same preflight at line 63; `PrototypeMenu.ValidationError` uses it on the shared prefab at `Assets/CPS/Flow/PrototypeMenu.cs:12-13`. The added menu and round tests both passed in `verification/task5-fix-green-play.xml`.

2. **GameplayViewport does not implement the required production Canvas viewport handoff** — **ADDRESSED**. `Assets/CPS/UI/GameplayViewport.cs:9,20-29` owns the serialized Canvas, attaches it to the viewport camera and restores reference resolution 1920×1080 with `Expand` scaling. Both authored scenes wire that reference and change their scaler mode; future authoring does the same in `Assets/CPS/Editor/GameSceneSetup.cs:155-162`. The native PlayMode check passed at 1024×768 and 2560×1080: Canvas and camera pixel rectangles matched, Canvas scale was 0.5333334 and 0.9999999 respectively, and root layout stayed 1920×1080. This establishes the Task 5 handoff; Task 6's broader UI-size coverage remains separate.

## New breakage in the fix diff

None found. The Canvas scaler dereference at `Assets/CPS/UI/GameplayViewport.cs:26-29` has a scaler in both authored scenes and in `CreateCanvas`; the old camera-only use remains valid when no Canvas is assigned.

## Out-of-scope observations

The earlier unexplained first capture with a red player and unavailable human/browser acceptance remain unresolved outside this fix. They do not block this scoped re-review.

## Checks

- Read the supplied fix package once, with its one commit, nine-path stat and full diff. Did not rerun Git or Unity.
- Focused route check outside the diff: `PrototypeMenu.ValidationError` reads the shared prefab's controller preflight, and `BeginRound` calls that preflight before starting the round. This resolves the concrete risk that the new identity check might not guard both entry points.
- Inspected `verification/task5-fix-green-edit.xml` (106/106), `task5-fix-green-play.xml` (55/55), and `task5-fix-final-authoring.xml` (1/1), all with zero failures/skips. The three added PlayMode tests passed; the viewport test XML contains the native bounds and scale measurements. These artifacts support the reported result without a suite rerun.

**Fix round: All findings addressed, no new Critical/Important breakage.**
