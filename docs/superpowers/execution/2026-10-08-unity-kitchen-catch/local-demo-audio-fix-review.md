# Local Demo Audio fix review

18 October 2026. Scoped re-review of `550e54a3b9107185d01757d67e5379ae84e8fa38..296c3af5b1bab4330ce011e58cb5e793bf7ae188`, using the supplied exact diff package, original review, updated execution report, connected source, and saved XML/log evidence.

What this change does: Reset now stops the mascot's existing one-shot source. The audio tests observe actual Unity source playback and timeline movement, exercise input and scene events, and restore the static unlock state after every test. Unused public test-state properties were removed.

**Overall: Approved. Spec compliance: Approved. Code quality: Approved.** All three original findings are Addressed. Open/new findings: **0 Critical, 0 Important, 0 Minor**.

## Original findings

| Finding | Status | Evidence |
| --- | --- | --- |
| 1. Important — terminal audio survives Restart | Addressed | `C:\Users\Arc\Unity\CPS-Project\Assets\CPS\Presentation\MascotPresentation.cs:174` calls `audioSource.Stop()` during reset, guarded for a missing source. |
| 2. Important — assertions check intent instead of playback | Addressed | `C:\Users\Arc\Unity\CPS-Project\Assets\CPS\Tests\PlayMode\LocalDemoAudioTests.cs:95` observes music `isPlaying` and `timeSamples`; line 220 checks Running continues past its earlier sample position after another movement call; lines 225–242 observe stopping. |
| 3. Minor — teardown leaks a static unlock | Addressed | `C:\Users\Arc\Unity\CPS-Project\Assets\CPS\Tests\PlayMode\LocalDemoAudioTests.cs:42` resets unlock in `finally`, alongside InputTestFixture cleanup. |

Finding 1: The connected in-scene Restart path is unchanged: `ResultPanel.cs:74` calls `GameSessionController.Restart` (`GameSessionController.cs:171`), which calls `BeginRound`, whose line 78 resets the same mascot. The one-line stop therefore reaches the actual user path without changing terminal timing or creating another source. The new long-clip test (`LocalDemoAudioTests.cs:264`) assigns the real AudioSource, starts a six-second synthetic Death one-shot, yields, observes playback, resets and observes silence, then repeats with Triumph. The saved pre-fix test fails specifically because the source remains playing after reset; the fixed test passes. Continuing behind Results remains a disclosed timing/listening concern, but it no longer carries through reset into the next round.

Finding 2: The replacement tests are meaningfully behavioral. Music is checked before unlock, after yielded audio time, after a scene selection change, and after an unsupported scene stops it. Running uses a ten-second clip, captures its sample position after 0.12 seconds, repeats the movement request, then confirms samples progressed instead of rewinding after another 0.05 seconds. It observes actual stopped/playing states for idle, movement restart, terminal and disable. Reset's terminal source is covered separately. Pause still sends the same zero-movement stop call at `GameSessionController.cs:163`; the unchanged connection plus source-level stop coverage is proportionate to this fix.

The saved mutation tests directly answer the original false-positive concern: replacing music Play with Stop fails the playing assertion; restarting Running on every request fails the timeline assertion (expected greater than 5,644 samples, actual 940); replacing Running Stop with Play fails the stopped assertion. No intent flag can satisfy these source assertions by itself.

Input and lifecycle coverage is also proportionate. `KeyboardAndMousePressUnlockMusicThroughUpdate` (`LocalDemoAudioTests.cs:137`) creates Input System test devices, checks no-input silence, sends keyboard and mouse presses through InputTestFixture, invokes the production Update handler, yields and observes playback. The mouse half recreates a locked director. This tests the C# input path deterministically, not browser-trusted DOM interaction. `ActiveSceneChangesReuseDirectorAndSwitchMusic` (line 167) uses actual SceneManager active-scene events with empty MainMenu/Gameplay scenes and checks the same director, identical two source components, and switched playing clip. It exercises the relevant persistent object/event path without authored scene side effects or PlayerPrefs. It is correctly described as an event-level check rather than a complete authored-scene/browser test.

Finding 3: Setup installs a null resolver and locked state before each test. Teardown destroys directors and synthetic clips, restores the resolver, and resets unlock in `finally`, even if normal cleanup fails. `FreshDirectorRemainsSilentWithoutInput` (line 123) checks a new director with an available synthetic Lobby clip stays silent when Update sees no input. Created scenes are restored/unloaded by UnityTearDown (line 48). The added fixture uses synthetic clips and injected/null resolution; it has no PlayerPrefs access or dependency on local MP3s.

## Fix scope and regression inspection

The package changes only `LocalDemoAudio.cs`, `MascotPresentation.cs`, and `LocalDemoAudioTests.cs`. It adds no audio assets, GUID references, package, runtime branch for tests, or new production test hook. The old public clip/playing/loop/volume getters are removed; tests inspect native AudioSources instead. The remaining `AudioUnlocked` accessor serves the mascot's production one-shot guard, and the existing resolver remains the asset-loading/test-isolation seam. A source search finds no remaining callers of the removed getters.

The director's music logic retains the same clip, volume and playback conditions while using a local desired-state variable. Running's existing transition state is now private; the start/stop guard and disable cleanup retain their behavior. The singleton, scene subscription/unsubscription, optional resource loader, real-input polling, loop flags and volumes are unchanged. No new fix-induced breakage or unnecessary architecture was found.

## Saved verification

| XML under `C:\Users\Arc\Downloads\CPS\Unity_Project\verification` | Result |
| --- | --- |
| `local-audio-fix1-reset-red.xml` | 1 total, 0 passed, 1 failed: expected stopped, source still playing |
| `local-audio-fix1-reset-green.xml` | 1/1 passed |
| `local-audio-fix1-music-play-mutant-red.xml` | 1 total, 0 passed, 1 failed: expected playing, source stopped |
| `local-audio-fix1-running-restart-mutant-red.xml` | 1 total, 0 passed, 1 failed: timeline rewound |
| `local-audio-fix1-running-stop-mutant-red.xml` | 1 total, 0 passed, 1 failed: expected stopped, source playing |
| `local-audio-fix1-final2-focused.xml` | 9/9 passed |
| `local-audio-fix1-final2-affected-play.xml` | 22/22 passed |
| `local-audio-fix1-final2-full-edit.xml` | 125/125 passed |
| `local-audio-fix1-final2-full-play.xml` | 82/82 passed |

The final logs identify Unity 6000.3.25f1, the intended checkout, matching platform/filter and XML/log destinations. The final saved XMLs have no failures, and final log searches found no C# compile errors or assertion exceptions. The current source timestamps precede the final runs: mascot 15:29:38 UTC, director 15:40:23 UTC, tests 15:44:53 UTC; final focused/full runs begin 15:45:49–15:47:45 UTC. The RED, mutation and final GREEN sequence is credible. No-audio-listener warnings in the saved synthetic-scene runs mean these are engine playback/timeline checks, not listening evidence; the report does not claim otherwise.

Verdict: **Approved.** No original finding remains Open and no new actionable finding was identified in this fix.

Not checked: No fresh tests, Unity/browser run, Git command or project edit was performed during this re-review. Commit scope is assessed from the supplied exact diff package rather than a fresh Git query. Actual browser AudioContext unlock, device listening, seamless looping and distribution licensing remain the disclosed manual gaps. Attribution still does not establish a license, and the existing remove-local-folder/rebuild instruction remains applicable. Only this requested documentation report was written.
