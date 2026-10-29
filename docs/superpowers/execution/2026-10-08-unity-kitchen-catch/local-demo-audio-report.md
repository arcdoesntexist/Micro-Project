# Local demo audio execution report

18 October 2026. Unity checkout: `C:\Users\Arc\Unity\CPS-Project`, branch `codex/kitchen-catch`. Commit: `550e54a3b9107185d01757d67e5379ae84e8fa38` (`feat: add optional local demo audio hooks`). This is a temporary, local classroom demo integration; no audio distribution rights were established.

## Reconstructed state and scope

I resumed an interrupted run without resetting the checkout. On arrival, `MascotPresentation.cs` was modified; `LocalDemoAudio.cs`, its meta, `Resources.meta`, and `LocalDemoAudioTests.cs` plus its meta were untracked. Five MP3s and their Unity metas already existed in `Assets/CPS/Resources/LocalDemoAudio`; the directory meta also existed locally. No audio report or root README existed. The saved Unity XML/logs below predated this handoff and were checked against their command lines, XML totals, failure messages, and source timestamps. Production/test source last edits at 17:56–17:57 preceded the final green runs at 17:57–18:00. The metadata probe later added and removed one temporary Editor script without changing production code or tests.

The implementation loads `LobbyMusic_BG`, `GameplayMusic_BG`, `Running`, `Triumph`, and `Death` by Resources path. A single persistent director manages the two looping sources. `MascotPresentation` keeps its existing terminal one-shot audio path and optionally fills missing Triumph/Death clip fields at runtime. Missing files leave the path silent. Existing eat/hurt/rank/UI/countdown/go behavior was not changed. No prefab or `ProductionArtSetup` edit was needed because the prefab already has an `AudioSource` and code resolves the optional clips at runtime; no excluded asset GUID is serialized.

## TDD and regression evidence

Unity version in logs: `6000.3.25f1`. Every listed test command used `-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform <mode> -testFilter <filter> -testResults <XML> -logFile <log>`; the actual arguments are recorded at the top of each named log in `C:\Users\Arc\Downloads\CPS\Unity_Project\verification`.

| Run | Filter / purpose | XML result |
| --- | --- | --- |
| `local-audio-red.xml` | PlayMode `LocalDemoAudioTests`, first behavioral RED | 5 total, 2 passed, 3 failed |
| `local-audio-running-unlock-red2.xml` | PlayMode running-loop test, tightened pre-interaction RED | 1 total, 0 passed, 1 failed |
| `local-audio-green2.xml` | PlayMode `LocalDemoAudioTests` final focused GREEN | 5 total, 5 passed |
| `local-audio-affected-play.xml` | PlayMode audio, presentation, and session flow | 18 total, 18 passed |
| `local-audio-full-edit.xml` | Full `CPS.KitchenCatch.Tests` EditMode | 125 total, 125 passed |
| `local-audio-full-play.xml` | Full `CPS.KitchenCatch.Tests` PlayMode | 78 total, 78 passed |

The first behavioral RED failed on the missing injected Lobby clip, missing Triumph clip, and missing Running clip. It was an assertion failure in executable tests, not merely a missing-symbol compile error. Earlier `local-audio-red-compile*.log` attempts were compile failures and are not counted as behavioral RED. The first running-unlock check (`local-audio-running-unlock-red.xml`) passed because it had not yet asserted the pre-interaction state; the revised `red2` failed as intended (`Expected True, But was False` after unlock) before the final production edit. The focused and full green XMLs came after that edit. Tests use synthetic `AudioClip.Create` clips and an injected resolver; they do not depend on the local MP3 files.

The final staged diff was checked with `git diff --cached --check` and an exact six-path allowlist. No compiler error or test failure appeared in the green XML/logs. Unity licensing/access-token messages in the logs are environment diagnostics, not game test failures. No new code or test edits were made after those suite runs; the later `Resources.meta` change only removed trailing spaces.

## Unity-observed local clip metadata

A temporary Editor-only probe called `Resources.Load<AudioClip>("LocalDemoAudio/" + name)` for each local file. Unity logged these values in `verification/local-audio-metadata-probe.log`, lines containing `LOCAL_AUDIO_PROBE`. The probe file and its generated meta were removed after Unity exited; `git status` confirms neither remains. The log reports successful batchmode exit. The surrounding PowerShell `Start-Process -Wait` remained attached to a child process after `Unity.exe` had exited, so its wrapper was interrupted; this does not substitute for an audible playback check.

| Clip | Unity length | Samples | Frequency | Channels | Load state at probe |
| --- | ---: | ---: | ---: | ---: | --- |
| Triumph | 6.713 s | 296,064 | 44,100 Hz | 2 | Unloaded |
| Death | 6.296 s | 277,632 | 44,100 Hz | 2 | Unloaded |
| GameplayMusic_BG | 70.113 s | 3,091,968 | 44,100 Hz | 2 | Unloaded |
| LobbyMusic_BG | 103.628 s | 4,569,984 | 44,100 Hz | 2 | Unloaded |
| Running | 28.421 s | 1,253,376 | 44,100 Hz | 2 | Unloaded |

Unity resolved all five names; `Unloaded` means the audio data was not preloaded at probe time. The test suite verifies source selection, loop flags, volumes (Lobby 0.20, Gameplay 0.22, Running 0.10), unlock behavior, no duplicate director/sources, and stopping on idle/terminal/reset/disable. Code inspection confirms session Pause calls `SetMovement(0)` and scene transitions stop Running. Human listening, seamless loop quality, actual output volume on the demo device, and WebGL audio unlock with these local MP3s were not measured. The terminal clips are longer than the 1.2/1.4-second visual terminal delays and may continue into Results.

## Commit boundary and preservation

The commit contains exactly:

- `Assets/CPS/Presentation/LocalDemoAudio.cs`
- `Assets/CPS/Presentation/LocalDemoAudio.cs.meta`
- `Assets/CPS/Presentation/MascotPresentation.cs`
- `Assets/CPS/Resources.meta`
- `Assets/CPS/Tests/PlayMode/LocalDemoAudioTests.cs`
- `Assets/CPS/Tests/PlayMode/LocalDemoAudioTests.cs.meta`

`.git/info/exclude` contains `/Assets/CPS/Resources/LocalDemoAudio/` and `/Assets/CPS/Resources/LocalDemoAudio.meta`. `git check-ignore -v` confirmed the directory, directory meta, MP3, and MP3 meta coverage. `git ls-files Assets/CPS/Resources/LocalDemoAudio` returned nothing. The staged path allowlist and staged diff scan found no `.mp3`, local directory/meta, or Git LFS pointer. The five local MP3s, their five metas, and the local directory meta remain outside the commit. Their five GUIDs have no references in committed CPS assets. No tracked `.gitignore` was changed.

The pre-existing user changes remain uncommitted: deletions of `Assets/Editor.meta`, `Assets/Editor/HubForceResolve.cs`, its meta, and the old Unity-copy design spec; modifications to `Assets/Settings/UniversalRP.asset`, `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`, and `ProjectSettings/URPProjectSettings.asset`; untracked `ProjectSettings/PackageManagerSettings.asset` and `ProjectSettings/SceneTemplateSettings.json`. Final `git status --short` contains the same path set as the initial status and none of those paths entered the commit. Pre-run byte hashes were not captured, so path-level status is the available preservation evidence. No add-all, reset, clean, or stash was used.

The separate documentation root `C:\Users\Arc\Downloads\CPS\Unity_Project` now has `README.md` with the five source URLs and disable steps; `docs/unity-font-audio-brief.md` records the temporary-demo status. This report and those Markdown files are outside the Unity Git commit, per project document-location rules.

## Rights and removal

Source URLs in the README are attribution/provenance only, not licenses. Death traces to Rockstar Games' *Red Dead Redemption 2*. Production or redistributable use is not approved for any of the five MP3s. To disable the local audio before a distributable build, move or delete `Assets/CPS/Resources/LocalDemoAudio` from the Unity project and rebuild; the optional code will remain silent. Eat, hurt, rank-up, UI confirm, countdown, and go SFX, as well as a licensed production font and production audio, are still outstanding.

## Review fix round 1 — 18 October 2026

Review: `local-demo-audio-review.md` found one terminal lifecycle bug and two test coverage/isolation gaps. Follow-up commit: `296c3af5b1bab4330ce011e58cb5e793bf7ae188` (`fix: stop terminal audio on reset and verify playback`). It contains exactly `Assets/CPS/Presentation/LocalDemoAudio.cs`, `Assets/CPS/Presentation/MascotPresentation.cs`, and `Assets/CPS/Tests/PlayMode/LocalDemoAudioTests.cs`.

`ResetPresentation()` now calls `Stop()` on the existing mascot `AudioSource`. A six-second synthetic Death/Triumph test starts the terminal one-shot, resets before it finishes, checks playback stopped, then starts and resets another terminal sound. The focused pre-fix run `local-audio-fix1-reset-red.xml` failed 0/1 at `Expected: False, But was: True` after reset. After the one-line fix, `local-audio-fix1-reset-green.xml` passed 1/1. This prevents the previous round's sound carrying into a rapid Restart while retaining the current one-shot path.

The PlayMode fixture now checks real `AudioSource.isPlaying`, `timeSamples`, clip, loop, volume, and component identity using only injected synthetic `AudioClip.Create` clips. It checks music starts and stops, Running advances without rewinding on repeated movement, and Running stops for idle/Pause-equivalent zero movement, terminal, reset, and disable. Keyboard and left-mouse presses are queued through the Input System fixture and passed to the component's actual `Update` handler deterministically. Empty Unity scenes named MainMenu and Gameplay are activated through `SceneManager` to exercise `activeSceneChanged`, same-director persistence, two-source identity, and music switching. This is a Unity scene-event check, not an authored-scene or browser AudioContext test. The fixture resets `audioUnlocked` in teardown's `finally` path and includes a fresh-director/no-input locked check. The public test-only clip/state getters were removed; production retains only the private Running transition state.

Mutation checks demonstrate that the new assertions are sensitive to the intended failures. Temporarily replacing music `Play()` with `Stop()` produced `local-audio-fix1-music-play-mutant-red.xml` (0/1, expected playing but false). Temporarily calling Running `Play()` on every movement update produced `local-audio-fix1-running-restart-mutant-red.xml` (0/1, timeline rewound from over 5,600 samples to 940). Temporarily replacing Running `Stop()` with `Play()` produced `local-audio-fix1-running-stop-mutant-red.xml` (0/1, expected stopped but playing). Each mutation was restored before final runs. A no-clip diagnostic showed Unity could report a source playing in the same frame after a stop; observing after an audio frame resolved it, so no unnecessary `playOnAwake` production change was retained.

Final serial Unity `6000.3.25f1` runs, using the same `-batchmode -projectPath -runTests -testPlatform -testFilter -testResults -logFile` pattern documented above:

| XML in `verification` | Result |
| --- | --- |
| `local-audio-fix1-final2-focused.xml` | Audio PlayMode fixture 9/9 passed |
| `local-audio-fix1-final2-affected-play.xml` | Audio, presentation, session flow PlayMode 22/22 passed |
| `local-audio-fix1-final2-full-edit.xml` | Full EditMode 125/125 passed |
| `local-audio-fix1-final2-full-play.xml` | Full PlayMode 82/82 passed |

All four final Unity processes exited 0. The full PlayMode log search found no C# error, assertion exception, or failed test. `git diff --cached --check` passed before commit; the staged allowlist contained only the three paths above and no MP3, excluded local audio metadata, or LFS pointer. `.git/info/exclude` still covers the local audio directory and directory meta. Final Unity `git status --short` contains the same pre-existing user-dirty path set as before this fix, with no owned audio path left uncommitted. Rights, live listening, seamless loop quality, and actual WebGL audio unlock remain unverified.
