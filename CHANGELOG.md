# Changelog

This file records the project changes between committed versions. Project purpose, gameplay and setup information belong in [README.md](README.md).



## Unreleased — 2026-10-29

### Added

- Single Player / Versus mode selection, independent profiles, two-player practice and Ready flow.
- Local split-screen endless survival with mirrored drop schedules, increasing challenge, shared pause/countdown and per-player results.
- Six authored matching helmet/body color sets and refreshed Home, gameplay and results UI.
- Windows Versus controller panel with separate COM-port selection for ESP32 P1 and NUCLEO-G071RB P2.
- Keyboard and FSR controls combined per direction; the broken STM32 left FSR is disabled while Left Arrow remains available.
- Strict STM32 raw-line adapter, ESP32 JSON validation reuse, hysteresis, stale-input pause and explicit keyboard recovery.
- Complete STM32CubeIDE NUCLEO-G071RB raw FSR bench project, alongside ESP32 sketches and shared protocol documentation.
- Multiplayer Apps Script handling with adjacent P1/P2 rows and alternating match blocks.

### Changed

- Spacebar and Escape can Pause/Resume Versus; controller/settings overlays guard against accidental resume.
- Connections retain independent, generation-scoped queues so retired overflow cannot fault a replacement connection or another player.
- Hybrid results retain the existing payload shape and truthfully identify their input source; FSR/keyboard do not report newton force.

### Verification and limits

- EditMode 221/221 and PlayMode 144/144 passed for the hybrid integration, including native STM32 COM4 receive; the later Spacebar change was confirmed in Play Mode by the user.
- STM32 firmware built with zero warnings/errors and was flashed with successful read-back verification.
- Dual USB is implemented for Windows Unity Editor/player. ESP32 hardware together with STM32, final FSR calibration and a new WebGL build remain unverified.
- STM32 endpoints 4095/100 are provisional. Raw logs, device identifiers, flash backups, Unity caches and build outputs are excluded from this repository snapshot.
- VERSION remains 0.1.0; no new release number or tag is assigned by this import.

## Micro-Project Version 0.1.0 — 2026-10-27

### Added

- Independent Microcontroller-course workspace derived from CPS Project Version 2.1.0.
- Separate `Unity`, `Firmware/ESP32`, and reserved `Firmware/STM32` boundaries.
- Repository-level documentation, protocol, artwork, sound, Google Apps Script and verification areas.

### Changed

- Renamed the visible Unity product and project identity to `Micro-Project`.
- Reset the project and Unity bundle version to `0.1.0` while retaining protocol version 1.
- Moved the combined controller and FSR/HX711 bench sketches under `Firmware/ESP32`.
- Kept `Assets/CPS`, `CPS.KitchenCatch` namespaces and `CPS.*` assembly names to preserve Unity references.

### Not included

- STM32 firmware and STM32/ESP32 responsibility sharing are not designed or implemented in this baseline.
- Historical CPS entries below remain unchanged because they describe the source project at the time of each release.

## Version 2.1.0 — 2026-10-27

### Added

- Global Keyboard / FSR / Load Cell selector on Home and USB recovery controls in Pause.
- One combined ESP32 sketch with FSR on GPIO34/35 and HX711 pairs DT32/SCK18 and DT33/SCK4, at 115200 baud.
- Windows Editor USB serial and a native browser permission button for Web Serial on HTTPS/localhost.
- Strict versioned JSON telemetry, independent sample freshness, asynchronous tare, and reset/reconnect guards.
- Automatic pause on stale input, USB loss or board restart; explicit Resume after recovery.
- Spacebar/Escape Pause/Resume with modal/text-entry guards, retaining A/D/B and arrow controls.
- Source-aware statistics: FSR relative 0–100%; Load Cell estimated Max/Average N; no simulated Newton values.
- Controller settings asset, firmware/host tests, Unity controller tests and USB setup documentation.

### Changed

- Kept the user's replacement left-FSR values: released 4095 and reference press 156.5, replacing 551/135 in the combined sketch.
- Updated combined-firmware test ADC samples and README pin/calibration tables to match the current sensors.
- Used a numeric fallback font in live controller readouts to display the trial font's missing percent glyph.
- Set `VERSION` and Unity `bundleVersion` to `2.1.0`. Telemetry schema remains version 1; HX711 force coefficients are unchanged.

### Verification and limits

- User confirmed successful gameplay with the updated sensor sketch.
- USB implementation evidence: EditMode 189/189; PlayMode 106/107 with one existing audio-volume assertion; JS bridge 7/7; combined firmware host cases 10/10; successful ESP32 compile and WebGL build.
- Automated verification did not upload firmware or open physical serial ports. Load Cell calibration and fault/latency checks remain hardware-dependent.
- MQTT, Node-RED and the planned Versus mode remain outside this version.

## Version 2.0.0 — 2026-10-26

Version 2.0.0 changes CPS Kitchen Catch from a design-and-evidence repository into a directly openable and tested Unity project.

### Difference from Version 1.0.0

| Area | Version 1.0.0 | Version 2.0.0 |
| --- | --- | --- |
| Repository | Design files, production artwork, implementation notes and verification evidence | Complete Unity project with `Assets`, `Packages` and `ProjectSettings` |
| Playable flow | Unity checkout was maintained separately | Boot, profile selection, difficulty selection, gameplay, pause and results are included in the repository |
| Profiles | Proposed workflow | Create, select, rename and delete local profiles |
| Difficulty | Design specification | Easy, Normal and Hard configurations with different speeds, intervals, hazard rates and environments |
| Gameplay | Approved concept and rules | 60-second rounds with food, rubbish, HP, score and D-to-SSS combo ranks |
| Results | Planned statistics | Outcome, score, difficulty badge, item counts, combo rank and detailed round statistics |
| Environments | Initial kitchen artwork | Kitchen, daytime cafe and nighttime cafe with separate ambient decorations |
| Presentation | Character and UI references | Mascot animation, readable two-facing IoTE helmet, eat/hurt/triumph/death effects and redesigned UI |
| Audio and settings | Demo sources and audio direction | Global volume, background-music toggle and optional local demo-audio loading |
| Cloud data | Integration plan | Google Sheets result payload and upload client for completed rounds |
| Automated verification | Reports from development stages | EditMode and PlayMode suites included with the Unity source |

### Added

- Complete Unity 6.3 LTS source snapshot and required `.meta` files.
- Home screen based on the approved Kitchen Catch UI, including profile management and difficulty badges.
- Gameplay HUD, game-rules overlay, pause menu and global audio settings.
- Results screen with summary and expandable detailed round information.
- Three environment presentations linked to difficulty.
- Food and rubbish item tracking in the final result.
- Google Sheets upload payload containing profile, round, score, item, combo and available input statistics.
- Editor authoring utilities and automated EditMode/PlayMode tests.
- `VERSION` file and Unity `bundleVersion` set to `2.0.0`.

### Changed

- Increased the mascot catch area to make food collection more forgiving.
- Corrected the engineer helmet position and provided separate left- and right-facing artwork while keeping `IoTE` readable.
- Updated the menu, gameplay and results layouts to the approved warm kitchen visual system.
- Moved audio controls into global settings rather than storing them per profile.
- Aligned scene difficulty references with the shared GameplayRig prefab.
- Updated the approved lobby, gameplay and running audio levels and added the Chomping catch sound hook.

### Verification

- Unity `6000.3.25f1` EditMode: 151 passed, 0 failed.
- Unity `6000.3.25f1` PlayMode: 100 passed, 0 failed.
- Required Unity assets were checked for matching `.meta` files.
- A fresh WebGL build was not produced during Version 2.0.0 release preparation.

### Known limitations

- ESP32, physical FSR input, serial/MQTT and Node-RED communication are not integrated in this release.
- Keyboard input is simulated `0%`/`100%` input and does not produce calibrated force measurements.
- Local two-player Versus mode remains a design draft and is not part of Version 2.0.0.
- Demo MP3s and `FFSpokenTrial-Regular.ttf` require replacement or license verification before public distribution.

## Version 1.0.0 — 2026-10-20

Initial repository baseline for CPS Kitchen Catch.

### Included

- Approved game concept and Unity implementation specification.
- Character, item, kitchen background and UI production artwork.
- Gameplay, combo-rank, death and result-screen design references.
- Asset provenance, setup notes and staged verification records.
- Classroom demo audio source files and attribution notes.

The playable Unity checkout was maintained outside this repository at this version.
## Unreleased — local Versus gameplay implementation

- Added five generated transparent body-atlas variants alongside original orange; helmet and footwear now share one Character Color selection across Mode Select, Versus Setup and Versus Gameplay. Removed runtime footwear filtering, preserving original Single Player artwork and all twelve animation frames.

- Added a dedicated VersusGameplay scene connected to Start Battle, with equal 16:9 kitchen fields, independent A/D and arrow controls, shared deterministic drops and progressive endless survival.
- Added shared countdown/pause, pause on focus loss, first-death results and same-step draws. Missed food does not remove HP.
- Added per-player score/rank/streak/item summaries, Rematch, Player Setup and Main Menu actions; abandoned matches are not saved.
- Applied the six authored helmet variants and selected shoe colors in gameplay. Reused kitchen decorations and added compact terminal food/pixel fragments.
- Added separate local multiplayer records and the dual-player Sheets payload, with one retry using the same match ID. Single Player storage/upload is unchanged.
- Included VersusGameplay in the WebGL scene list. No WebGL build was produced.
- Subsequent QA fixed the EditMode Newtonsoft assembly reference, missing kitchen/frame Sprite IDs and footwear hue mixing. Added scene/asset/rendered-pixel regression coverage. Automated and visual verification details are in `verification/versus-validation/RESULTS.md`; WebGL, live upload and physical-controller checks remain separate.


