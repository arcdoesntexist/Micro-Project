# Local Demo Audio review

18 October 2026. Reviewed `922c2ef42a606d9956e4d0ed463da4cac55d6e9b..550e54a3b9107185d01757d67e5379ae84e8fa38` in `C:\Users\Arc\Unity\CPS-Project`.

What this change does: It optionally loads five local classroom-demo clips. One persistent object owns music and running loops; the existing mascot source plays the terminal one-shots. Missing resources leave audio silent.

**Overall: Needs fixes.** Spec compliance: Needs fixes for reset behavior and verification. Code quality: Needs fixes for one-shot cleanup and test isolation/coverage. Counts: **0 Critical, 2 Important, 1 Minor**.

## Must fix

1. **Important — Restart carries the previous terminal sound into the next round.** Location: `C:\Users\Arc\Unity\CPS-Project\Assets\CPS\Presentation\MascotPresentation.cs:173` (`ResetPresentation`, lines 166–182), connected to its `PlaySound` at line 194.

   What this is: Triumph/Death now resolve real optional clips and use `audioSource.PlayOneShot`. The same mascot and source survive an in-scene Restart.

   Problem: Results opens after 1.2/1.4 seconds (`GameSessionController.cs:151`), but the supplied clips last 6.713/6.296 seconds (`verification/local-audio-metadata-probe.log:2394` and `:2405`). `ResultPanel.cs:74` calls Restart, which reaches `BeginRound` and `ResetPresentation` (`GameSessionController.cs:78`). Reset stops only Running and never calls `audioSource.Stop()`. Restart immediately after Results therefore carries roughly 5.5/4.9 seconds of the old terminal sound into the new round, including roughly 2.5/1.9 seconds after the three-second countdown. A rapid subsequent death can also overlap another terminal one-shot on this source.

   Fix: Stop the existing mascot audio source during reset; keep the current source and one-shot path. Add a synthetic long-clip regression that starts terminal playback, resets/restarts before it finishes, and checks the source has stopped. The current reset assertions check only the director's Running flag (`LocalDemoAudioTests.cs:114`) and cannot catch this.

   If skipped: The last round's death/fanfare remains audible during a fresh attempt. Merely continuing behind Results is a disclosed listening/timing concern, not a separate defect here; surviving Restart is the concrete lifecycle bug.

## Should fix

2. **Important — The new tests can pass when audio playback is broken.** Location: `C:\Users\Arc\Unity\CPS-Project\Assets\CPS\Tests\PlayMode\LocalDemoAudioTests.cs:62` (music assertions, lines 62–70), `:98` (running assertions, lines 98–119), and `:133` (terminal assertions).

   What this is: The tests check clip selection, flags, volumes and source count through the public director state. Those are useful configuration checks.

   Problem: `IsMusicPlaying` is assigned from `audioUnlocked && clip != null` (`LocalDemoAudio.cs:99`), and `IsRunning` from the desired state (`:123`), independently of observed source playback. Removing the `Play()`/`Stop()` calls still leaves these assertions green. Repeated `Play()` calls that reset the running clip on every movement update also leave the current test green; two components do not prove one uninterrupted playback. Terminal coverage checks only assigned private clip fields, with no assigned mascot AudioSource. All five tests are synchronous `[Test]` methods and none drives the keyboard/mouse Update path or a real scene transition, so they also cannot demonstrate the claimed interaction and persistence behavior.

   Fix: Add a few focused PlayMode checks with sufficiently long synthetic clips: observe the actual sources across yielded frames; confirm playback advances across repeated movement calls and stops on idle/Pause/reset; drive the input adapter's no-input and keyboard/mouse paths; transition MainMenu/Gameplay and verify the same director/two sources. Keep real browser autoplay/audio quality as an explicit manual check. These tests should fail if playback calls are removed or running playback is restarted every frame.

   If skipped: The saved green suite certifies requested state while silent, stuck or repeatedly restarted audio remains undetected. This is a coverage finding, not a claim that actual WebGL playback has already failed.

3. **Minor — Test teardown leaves audio globally unlocked.** Location: `C:\Users\Arc\Unity\CPS-Project\Assets\CPS\Tests\PlayMode\LocalDemoAudioTests.cs:25` (teardown, lines 23–28).

   What this is: Setup resets the static unlock state, and several tests explicitly unlock it. Teardown destroys the director and restores the production Resources resolver.

   Problem: Teardown does not reset `audioUnlocked`. Destroying a director does not reset that static field (`LocalDemoAudio.cs:138`), so a later test that creates a director in MainMenu/Gameplay starts with audio already unlocked and the real resource resolver. This can make later tests depend on order and play/load the local optional assets without a fresh input event. The focused fixture itself correctly injects synthetic/null clips and does not access PlayerPrefs or local MP3s.

   Fix: Restore a locked state during teardown, including failure cleanup, and assert a newly created director remains locked without input. Keep any broader scene/audio tests isolated with their own injected resolver.

   If skipped: A prior synthetic interaction leaks into later boot/scene tests, weakening the pre-interaction guarantee.

## Checks that passed by inspection

- The review package contains the exact six paths reported. The three reviewed source/test files match commit `550e54a` after normalizing line endings; `git diff --check 922c2ef 550e54a` is clean. No implementation code was changed during this review.
- `Resources.Load<AudioClip>` is optional and null-safe. Missing music stops its source; missing Running never starts; missing terminal clips are skipped by the existing guarded one-shot method. There is no serialized reference to the excluded clips.
- The commit adds no MP3, local folder/meta or audio Git LFS pointer. `git ls-tree` contains no `Assets/CPS/Resources/LocalDemoAudio` entries. All five local clip GUIDs and the local folder GUID are absent from committed CPS content. Existing artwork LFS pointers predate this commit and are outside its change. `.git/info/exclude` names both the local directory and its meta.
- `EnsureExists` and the Awake duplicate guard produce one persistent director with two sources in the normal Boot/MainMenu/Gameplay flow. Awake subscribes once to `activeSceneChanged`; OnDestroy unsubscribes and clears the instance. Scene refresh selects Lobby at 0.20 in MainMenu, Gameplay at 0.22 in Gameplay, and stops music in other scenes. An in-scene Restart reuses the director. No new session/event subscription was added.
- Bootstrap creates the director but does not unlock it. Normal runtime unlock callers are actual keyboard/left-mouse polling and the existing Start-button callback; programmatic `StartSelectedGame()` does not unlock. After unlock, both desired music and Running are applied. The public notifier is a trusted signal, not proof that the browser AudioContext actually resumed. Real WebGL autoplay remains unverified, as disclosed in the implementation report.
- Running uses its own looping source at 0.10. `SetMovement` normalizes to -1/0/1 and sends `movement != 0`. Existing input subtraction produces zero for both keys or neither. Pause explicitly sends zero (`GameSessionController.cs:163`); terminal, reset, mascot disable/destroy, scene change and application pause send stop. `ApplyRunning` starts only on a false-to-true desired-state change, so the implementation does not restart the loop every frame. Eat/Hurt reaction animations retain the movement value, and their separate one-shot source does not replace or stack the Running source.
- Existing serialized terminal clips retain priority over the optional resolver. The prefab already supplies a non-looping AudioSource at volume 0.45 with Play On Awake off. Optional terminal audio therefore needs no prefab/authoring GUID or architecture expansion.
- The change is scoped: no new package, global event bus, settings system, profile write, or unrelated gameplay behavior. The small resolver seam is justified by asset-independent tests. The public state getters serve the current tests; see finding 2 for their limitations.
- README and font/audio brief correctly label source URLs as provenance, not licenses; specifically identify the Rockstar/RDR2 origin; leave production rights/font/SFX acceptance pending; and instruct moving/removing `Assets/CPS/Resources/LocalDemoAudio` followed by rebuild. The instructions are truthful: ignored Resources assets still enter a local build while present, and removing them makes these optional hooks silent. Git exclusion alone is not described as a distribution safeguard.

## Saved evidence checked

| Evidence | Observed result |
| --- | --- |
| `verification/local-audio-red.xml` | 5 total, 2 passed, 3 assertion failures: missing Lobby, Triumph and Running synthetic clips |
| `verification/local-audio-running-unlock-red.xml` | 1 passed; correctly not represented as useful RED |
| `verification/local-audio-running-unlock-red2.xml` | 1 failed with expected True / actual False in the running-unlock test |
| `verification/local-audio-green2.xml` | 5/5 passed |
| `verification/local-audio-affected-play.xml` | 18/18 passed |
| `verification/local-audio-full-edit.xml` | 125/125 passed |
| `verification/local-audio-full-play.xml` | 78/78 passed |

The logs name Unity 6000.3.25f1, the correct checkout, runTests, intended platform/filter, and matching XML paths. Checked green logs contain no C# compile errors or test assertion exceptions. Current production edits are timestamped 10:57:21–22 UTC and the test file 10:56:35 UTC; focused GREEN begins 10:57:53 UTC and final suites follow. The stored behavioral REDs and chronology are credible; earlier compile-only attempts are not behavioral evidence. Historical pre-fix source snapshots were not supplied, so chronology cannot independently prove every exact RED-to-code sequence. Saved green totals are real but have the coverage limitations in finding 2.

Verdict: **Needs fixes — fix 1 and 2 before approval; also clean up 3.**

Not checked: No Unity process, browser, playback device or fresh test run was started, per review scope. Listening quality, seamless loops and actual WebGL autoplay are still manual acceptance items. A read-only `git status`/working-tree diff could not run in this sandbox (work-tree/permission errors); committed-tree inspection and direct source-to-commit comparison were used instead. No project changes, commits or Git metadata writes were made; only this requested report was created in the documentation root.
