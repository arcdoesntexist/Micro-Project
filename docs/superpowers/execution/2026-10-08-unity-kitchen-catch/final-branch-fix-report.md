# Final branch review fix report

## Result

Addressed the two binding findings in `final-branch-review.md` in the single authorized fix wave. Commit `990fe9e08f3daa33da8533639b573f7d22e1e523` (`fix: select persisted profile when menu loads`) on `codex/kitchen-catch`, parent `bbbf39c22d3f38fb71d393bb63aa42c0601c9c51`.

Fresh full EditMode: 125/125 passed. Fresh full PlayMode: 72/72 passed. Both have zero failed, skipped, or inconclusive tests and Unity exit 0. All Unity invocations ran serially in the original checkout, after elevated process checks found no open Unity editor. No process was killed.

## Important finding: saved profile selection

Verified the reported root cause in `MainMenuUI.RefreshProfiles`: the dropdown displayed index 0 using `SetValueWithoutNotify`, but no `AppSession.Select` occurred when a fresh session loaded stored profiles. Start validation consequently had no selected profile.

Before changing production code, added `GameSceneFlowTests.FreshSessionSelectsSolePersistedProfileWhenProductionMenuLoads`. It persists exactly one profile through the real `ProfileStore` into injected storage, destroys the initial AppSession, initializes and activates a fresh AppSession against that storage, and verifies one loaded profile with null initial selection. It then loads the authored production MainMenu scene. Assertions check dropdown index 0, the displayed persisted ID, selected identity, Easy difficulty, enabled Start, valid menu configuration, and an unchanged single-profile count. It neither creates a profile after reload nor invokes a dropdown event. No real PlayerPrefs are changed by the fixture.

The regression failed behaviorally before the fix: expected `SelectedProfile` not null, but it was null. XML: 1 total, 0 passed, 1 failed; Unity exit 2. This is the required production TDD RED, not a compilation/environment failure.

The production change is a two-line guard after the displayed dropdown value is assigned: when selection is null and profile IDs exist, select that displayed profile using the menu's current difficulty. Existing selections continue through the previous path without replacement.

Also strengthened the existing authored Gameplay round-trip test: it starts with two profiles and the second selected, then verifies the same player ID, dropdown index 1, enabled Start, and each selected difficulty after returning from Gameplay. The original survival, death, restart, Results navigation, singleton, and play-count assertions remain in place. This protects against a fix that unconditionally replaces a returning player's selection with the first profile.

Focused regression GREEN: 1/1. Affected scene/UI suites GREEN: 20/20, comprising GameSceneFlowTests 12/12 and GameUITests 8/8.

## Minor finding: distribution test hardening

The existing seeded (`723`) thousand-spawn test now compares each food count divided by the actual selected-food count against `1f / 3f`, with an absolute share tolerance of `0.07f` (seven percentage points). The denominator is the food-only list before its synthetic initial path anchor is inserted. Normal/Hard trash selections therefore do not dilute the denominator.

The accepted share interval is approximately 26.33%–40.33%. A 500/250/250 split has shares 50%/25%/25%, so it fails the new assertion. This is broad deterministic regression coverage, not a statistical proof of randomness. Existing seed reproducibility, lane safety, trash, and path checks remain intact.

No `SpawnPolicy` runtime code changed. This strengthens a test against already-correct runtime behavior; no production TDD RED is claimed or required for this finding. SpawnPolicyTests GREEN: 19/19.

## Exact Unity commands and results

Executable: `C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe` (6000.3.25f1). Each argument string below was passed through hidden `Start-Process -PassThru`; the launcher called `WaitForExit()`, `Refresh()`, and printed the actual `ExitCode`. No `-quit` or `-nographics` was used. Each next run started only after the preceding process exited.

```powershell
$taskProcess = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
$taskProcess.Refresh()
Write-Output ('UnityExit=' + $taskProcess.ExitCode)
```

The exact `$taskArgs` values, in execution order:

```text
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.GameSceneFlowTests.FreshSessionSelectsSolePersistedProfileWhenProductionMenuLoads -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-menu-red.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-menu-red.log

-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.GameSceneFlowTests.FreshSessionSelectsSolePersistedProfileWhenProductionMenuLoads -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-menu-green.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-menu-green.log

-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.SpawnPolicyTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-spawn-green.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-spawn-green.log

-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.GameSceneFlowTests;CPS.KitchenCatch.Tests.GameUITests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-affected-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-affected-play.log

-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-full-edit.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-full-edit.log

-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-full-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/final-fix-full-play.log
```

Every XML was parsed directly, including failures, totals, and actual test fixtures. The corresponding terminal log completion and process exit were inspected; exit codes alone were not treated as passing evidence.

| XML/log stem under `verification/` | XML UTC start–end, 18 Oct 2026 | Total | Passed | Failed | Skipped/inconclusive | Unity exit |
|---|---|---:|---:|---:|---:|---:|
| `final-fix-menu-red` | 09:31:49–09:31:52 | 1 | 0 | 1 | 0/0 | 2 |
| `final-fix-menu-green` | 09:32:39–09:32:40 | 1 | 1 | 0 | 0/0 | 0 |
| `final-fix-spawn-green` | 09:33:16–09:33:16 | 19 | 19 | 0 | 0/0 | 0 |
| `final-fix-affected-play` | 09:33:47–09:33:59 | 20 | 20 | 0 | 0/0 | 0 |
| `final-fix-full-edit` | 09:34:28–09:34:30 | 125 | 125 | 0 | 0/0 | 0 |
| `final-fix-full-play` | 09:35:02–09:35:15 | 72 | 72 | 0 | 0/0 | 0 |

Logs contain the previously documented licensing access-token-unavailable and entitlement Code 404 messages. Scans of all six logs found no C# compiler errors/warnings, compilation failure, NullReferenceException, MissingReferenceException, AssertionException, unhandled exception, or crash marker. The menu RED's expected assertion is recorded in XML; it is not a runtime game exception. The five GREEN logs explicitly report `Test run completed. Exiting with code 0 (Ok). Run completed.` Git reported its normal LF-to-CRLF working-copy notice; `diff --check` and staged `diff --check` passed.

## Changed paths and commit scope

Committed exactly these existing files in `C:/Users/Arc/Unity/CPS-Project`:

- `Assets/CPS/UI/MainMenuUI.cs`: guarded initial selection; two production lines.
- `Assets/CPS/Tests/PlayMode/GameSceneFlowTests.cs`: fresh persisted-profile regression and stronger existing return assertions.
- `Assets/CPS/Tests/EditMode/SpawnPolicyTests.cs`: actual-food share denominator and tolerance.

Commit summary: three files, 45 insertions, two deletions. No new asset or source file was created, so no new meta was required. No dependency, abstraction, comment, emoji, project setting, package, generated package asset, build output, scene, prefab, or runtime capability was added. This report is outside the Unity repository at the required documentation location and is not part of the game commit.

## User-change preservation

The preflight index was empty. Recorded the exact dirty inventory and full SHA-256 hashes before the first Unity launch. After both full suites, all seven hashes matched byte-for-byte:

| Existing user file | SHA-256 before and after |
|---|---|
| `Assets/Settings/UniversalRP.asset` | `167D20E3285F9071DC9BCB0A4FEE7CDF8BB335DD849EC96E4B3FA39BD401F266` |
| `ProjectSettings/EditorBuildSettings.asset` | `036EC2E1562BB649FC133AA17F55696119465F5D2628CF5EB5DE55B297C02C51` |
| `ProjectSettings/ProjectSettings.asset` | `33427C0BDEE1CF0EB6F872A3E130D222325AFA23362328F6E4DD2A63F44B2A86` |
| `ProjectSettings/ShaderGraphSettings.asset` | `5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7` |
| `ProjectSettings/URPProjectSettings.asset` | `C317FE22B9488785C0611F0642429922E5D324430D929BFFB41648130F30C218` |
| `ProjectSettings/PackageManagerSettings.asset` (untracked) | `B6801EE33298097F67FE42A6C8A2FBB0A9BB0E74A3B9AD5B7BDB04A4FD148CAF` |
| `ProjectSettings/SceneTemplateSettings.json` (untracked) | `BB9098B3BFCDE78D93E264B96F7B77B5430E64E8C57A7AAD7F5A5D9C3945E16A` |

All four existing deletions remain absent: `Assets/Editor.meta`, `Assets/Editor/HubForceResolve.cs`, `Assets/Editor/HubForceResolve.cs.meta`, and `docs/superpowers/specs/2026-10-08-unity-kitchen-catch-design.md` inside the Unity repository.

Staged the three explicit owned paths only; compared the staged path list against that exact allowlist, checked staged whitespace, and committed. Post-commit status contains precisely the original eleven dirty/untracked entries, with no remaining CPS source diff. No add-all, reset, restore, clean, stash, push, merge, or PR action was used. Existing build outputs remain uncommitted.

## Concerns and acceptance boundaries

No unresolved code/test concern was found within these two findings. The persisted-profile regression uses a real ProfileStore with controlled storage and the production MainMenu scene in Unity PlayMode; it is not a fresh browser reload test. WebGL builds and browser acceptance were not rerun for this fix wave, so existing generated builds predate this source fix.

Previously disclosed foreground 60 FPS, genuine 30/60 comparison, hands-on mouse/keyboard and difficulty feel, licensed font/Thai shaping, audio, and unexplained development-only shader messages remain unchanged acceptance items. Physical sensor, MQTT, transport, and cloud work remain outside this phase.
