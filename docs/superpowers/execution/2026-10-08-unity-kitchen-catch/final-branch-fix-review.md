# Final branch fix re-review

What this change does: Selects the displayed persisted profile when a fresh session mounts MainMenu. It also tests that returning from Gameplay preserves a nonzero profile selection and difficulty, and tightens the seeded food-distribution assertion.

Reviewed only the supplied one-commit package `bbbf39c22d3f38fb71d393bb63aa42c0601c9c51..990fe9e08f3daa33da8533639b573f7d22e1e523`, the original final review, fix report, relevant approved specification/constraints, connected production code, fixtures, and existing XML evidence. Expected use is one local player in a desktop browser. No Git, Unity, browser, build, or test execution was performed. The only write is this report.

## Assessment

- Spec compliance: **Pass for this fix wave**
- Code quality: **Pass**
- Overall: **Approved**
- Original findings: **2 Addressed, 0 Open**
- New fix-introduced findings: **0 Critical, 0 Important, 0 Minor**

## Original finding 1: sole saved profile cannot be selected after reload

**Addressed.** In [MainMenuUI.cs:88](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/UI/MainMenuUI.cs:88), the new guard selects `profileIds[selected]` only when the session has no selected profile and the populated list is nonempty. In that case `selected` is the same initial index zero assigned to the dropdown. The selected ID therefore comes directly from the displayed list, and `AppSession.Select` resolves it against the same store. Selection is established before `RefreshStatus`, removing the missing-profile validation error and allowing Start when the authored dependencies are valid. The empty-list path remains safe.

The existing-selection path retains both identity and difficulty. `RefreshProfiles` first takes `app.SelectedDifficulty` when a selection exists, finds that profile's actual index, and assigns the dropdown without notification. The new null guard does not replace that selection. `AppSession.Select` only assigns session references and difficulty; it neither saves nor edits a stored profile.

The new [GameSceneFlowTests.cs:82](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/PlayMode/GameSceneFlowTests.cs:82) regression creates one profile through the real ProfileStore serializer, then destroys the original AppSession and initializes a fresh instance from the same injected storage. It explicitly establishes one loaded profile and null selection before loading the authored MainMenu. It checks index zero, one option, the persisted ID in the visible caption, the selected identity, Easy difficulty, enabled Start, valid configuration, and an unchanged profile count. It does not create another profile after reload, select a profile through a helper, or invoke a dropdown event to manufacture success. These are actual production-menu outcomes rather than a test-only copy of the guard.

Removing the production guard leaves the session selection null: `AppSession.Initialize` only loads the store, and MainMenu's existing `SetValueWithoutNotify` does not select a profile. The non-null assertion then fails. Independently inspected `verification/final-fix-menu-red.xml` records exactly that behavioral failure at test line 109, with one test and one failure; `final-fix-menu-green.xml` records the same named test passing. The failure is not a compile error or a test-environment failure.

The strengthened [GameSceneFlowTests.cs:116](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/PlayMode/GameSceneFlowTests.cs:116) round-trip test creates two profiles and selects the second before mounting MainMenu. Each Easy/Normal/Hard round then returns through the real Results menu action and checks the same player ID, difficulty, dropdown index one, enabled Start, selected difficulty tint, and singleton count. The helper only creates a profile when selection is null and sets the requested difficulty before Gameplay; it does not repair state after the return being asserted. This would catch an unconditional replacement with the first profile. It is complementary preservation coverage; the fresh-session regression is the test that must fail when the new guard is removed.

Storage isolation is sound within these added/modified tests. Setup and the reloaded session supply read, write, and flush delegates before activation. Both stores in the persistence regression use only a private dictionary and no-op flush; no delegate falls back to PlayerPrefs. AppSession.Awake sees an initialized store, and the fixture destroys the replacement session during cleanup. The added preselection and round results in the round-trip test likewise use Setup's injected store. No new real PlayerPrefs access or cleanup deletion was introduced.

## Original finding 2: food counts admit a 500/250/250 split

**Addressed.** [SpawnPolicyTests.cs:68](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/EditMode/SpawnPolicyTests.cs:68) now filters the actual accepted food selections and checks every definition's `count / foods.Count` against one third with absolute tolerance `0.07f`. The float cast prevents integer division. Counts and denominator cover the same accepted food events, exclude trash in Normal/Hard, and are evaluated before insertion of the synthetic path anchor.

The accepted interval is approximately 26.33% to 40.33%. Shares of 50%, 25%, and 25% from the original 500/250/250 counterexample fail. Seed 723, the fixed thousand accepted choices, and the existing reproducibility check make this a deterministic regression assertion. Seven percentage points permits ordinary sample variation while rejecting the identified two-to-one bias; it does not claim to prove statistical uniformity. Existing path, lane, and trash checks remain. The supplied diff changes no SpawnPolicy runtime logic.

## Fix scope and verification

The only runtime delta is the two-line MainMenu guard. It adds no persistence mutation, platform API, dependency, asset, scene, settings change, build hook, or release/debug path. The remaining changes are confined to test assemblies. No concrete new breakage, overreach, or data-loss risk was found in this diff.

Independently parsed existing XML evidence:

| Evidence under `verification/` | Passed / total | Failed | Skipped / inconclusive |
|---|---:|---:|---:|
| `final-fix-menu-red.xml` | 0 / 1 | 1, expected behavioral RED | 0 / 0 |
| `final-fix-menu-green.xml` | 1 / 1 | 0 | 0 / 0 |
| `final-fix-spawn-green.xml` | 19 / 19 | 0 | 0 / 0 |
| `final-fix-affected-play.xml` | 20 / 20 | 0 | 0 / 0 |
| `final-fix-full-edit.xml` | 125 / 125 | 0 | 0 / 0 |
| `final-fix-full-play.xml` | 72 / 72 | 0 | 0 / 0 |

The full PlayMode XML explicitly includes both the fresh-session regression and the strengthened authored round-trip test as Passed. Process exit codes, file hashes, and log scans remain the implementer's reported evidence; this review did not rerun those checks.

## WebGL rebuild judgment

**Not a blocking gap for approval of this source fix.** The production change calls an already-used runtime method with an existing string ID and enum inside ordinary C# control flow. It adds no reflection, new generic instantiation, serialization shape, conditional compilation, browser integration, or asset reference that creates a distinct WebGL/IL2CPP compilation concern. Production-scene PlayMode coverage exercises the changed menu lifecycle and validation path, with a demonstrated behavioral RED followed by GREEN and passing full suites. That is proportionate verification for this bounded change.

Existing WebGL output nevertheless predates the fix and still contains the old behavior. Approval here must not be represented as proof that those generated builds contain the fix or that a fresh browser reload was tested. Rebuild and verify the updated artifact before distributing or presenting it as the corrected WebGL release. This artifact currency requirement is distinct from an open defect in this fix diff. Previously disclosed browser/performance/font/audio/manual acceptance items remain outside this re-review and are not newly closed.

Verdict: **Ship this source fix.** Both binding findings are addressed; no new fix-introduced findings.
