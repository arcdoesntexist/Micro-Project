# Micro-Project: Kitchen Catch

Micro-Project is a university Microcontroller project derived from CPS Kitchen Catch Version 2.1.0. It uses the existing two-hand arcade game as an application layer for integrating ESP32 and STM32 controllers. The player moves a mascot left and right to catch food, avoid rubbish, build a combo rank and survive a 60-second round. After each round, the system records gameplay and hand-input statistics for later review.

The project is designed as an educational rehabilitation-style activity rather than a clinically validated treatment or medical device. Its purpose is to demonstrate how a physical controller, an interactive application and cloud data collection can operate as one cyber-physical system.

## Why this project exists

The project demonstrates how microcontrollers, physical sensors, a Unity application and external data storage can cooperate as one system. Version 0.1.0 establishes the independent course workspace as a baseline; the current working snapshot adds independent STM32 and ESP32 USB controllers.

The complete concept has three connected parts:

1. **Unity Web Application** — presents the game, profiles, difficulty levels, scoring, combo feedback and round results.
2. **Microcontroller and sensors** — ESP32 supplies P1 FSR values over USB, while NUCLEO-G071RB supplies P2 raw FSR readings over a separate USB serial port. Keyboard can be used alongside the sensors.
3. **Cloud result storage** — stores player, score, time, difficulty and input statistics for later analysis. This project uses a Google Sheets web endpoint rather than Firebase.

## Repository layout

```text
Micro/
├─ Unity/                 Unity game, packages and project settings
├─ Firmware/ESP32/        Combined controller and sensor bench sketches
├─ Firmware/STM32/        NUCLEO-G071RB FSR bench sources and setup guide
├─ sensor-protocol/       Shared telemetry protocol and host checks
├─ google-apps-script/    Google Sheets result receiver
├─ docs/                  Current guides and historical design records
├─ art/ and sound/        Source classroom assets
└─ verification/          Existing verification evidence
```

## Version 0.1.0 baseline

This repository contains the Unity gameplay client inherited from CPS Project 2.1.0, combined ESP32 firmware, documentation and automated tests. Version 0.1.0 separates the Unity, ESP32 and future STM32 workspaces and changes the visible project identity without renaming the stable `CPS.*` implementation namespaces.

| System area | Version 0.1.0 status |
| --- | --- |
| Unity game and menu flow | Implemented |
| Local player profiles | Implemented |
| Easy, Normal and Hard modes | Implemented |
| Score, HP and D-to-SSS combo ranks | Implemented |
| Kitchen, daytime cafe and nighttime cafe environments | Implemented |
| Round results and item statistics | Implemented |
| Google Sheets result upload | Implemented |
| Keyboard simulation for two hand inputs | Implemented |
| ESP32 and physical sensor input | Selectable FSR or Load Cell input from one board |
| USB serial | Windows Unity Editor; Web Serial in a supported desktop browser |
| Force values in newtons | Estimated N for tared, fresh Load Cell input; FSR and keyboard do not report N |
| MQTT and Node-RED communication | Not included in this release |
| STM32 integration | Directory boundary only; responsibilities and firmware not implemented |
| Local two-player Versus mode | Gameplay implementation added after the 0.1.0 baseline; awaiting user playtest |

## Versus keyboard and FSR controllers (Windows)

In Versus Setup, open **CONTROLLERS**, refresh the available COM ports, click each player's port selector to cycle to the correct board, and choose **CONNECT**. P1 uses ESP32 with its existing v1 JSON firmware. P2 uses NUCLEO-G071RB with the raw FSR bench firmware. Assign ports explicitly; do not assign the same port to both players. Close other serial terminals first. Keyboard-only play needs no USB connection.

| Player | Left | Right |
| --- | --- | --- |
| P1 | A or ESP32 GPIO34 FSR | D or ESP32 GPIO35 FSR |
| P2 | Left Arrow; broken STM32 left FSR disabled | Right Arrow or STM32 PA1/A1 FSR |

Keyboard and enabled sensors can be used together in practice, Ready and gameplay. Press both directions to ready. Editing a nickname or opening the Controllers overlay suppresses practice controls. A lost/stale connected stream pauses both fields; use Controllers to reconnect, then Keep Cooking, or explicitly Continue with Keyboard. Resume runs the shared countdown. Continuing with keyboard disconnects both Versus controllers for that match; it does not automatically resume on reconnect.

`Unity/Assets/CPS/Resources/VersusControllerSettings.asset` holds enabled channels, press/release hysteresis (20%/15%), stale timeout (.75 s), and provisional STM32 released/reference endpoints (4095/100). STM32 left remains disabled until replaced and verified. FSR values are relative percentages, never newtons. The STM32 endpoints require physical calibration confirmation. Single Player retains its existing controller settings and USB workflow.

This dual-controller path supports Windows Unity Editor/player. WebGL still supports keyboard for Versus; dual Web Serial is not implemented. See `docs/unity-usb-controller-guide.md` and [verification summary](docs/verification-summary.md) for instructions and verification limits.

## Gameplay

### Local Versus

Open `Unity/Assets/CPS/Scenes/Boot.unity`, enter Play Mode, choose Versus, select two distinct profiles and ready both players. Start Battle now opens `VersusGameplay`. P1 uses A/D; P2 uses Left/Right Arrow. Both players receive the same drops in equal 16:9 kitchen fields. Missed food resets combo without damaging HP; caught rubbish costs one HP. The shared endless challenge increases every 20 active seconds, up to tier 9. First death ends both fields; simultaneous death in one simulation step is a draw.

Pause or Escape pauses both players. Losing application focus also pauses the match. Resume starts a shared three-second countdown. Leaving an unfinished match records nothing. Completed results include scores, HP, highest ranks, best streaks and caught-item counts. Rematch keeps profiles/colors and starts a new match ID and spawn seed.

The `Versus Match` object in the `VersusGameplay` scene exposes challenge tuning, movement speed, catch half-extents, art references, optional Versus Music and the Micro result endpoint. The split-screen view is constructed when Play starts. Assign a competitive track to Versus Music when available; an empty assignment uses the existing gameplay track. A single Character Color choice selects matching authored helmet and footwear sprites. Six body animation sets preserve the twelve original poses; no shoe recolor material is used. Mode Select previews use matching artwork, while ordinary Single Player keeps its original orange sprites. Source images and generation prompts are in `art/production/body-colors`.

Completed matches are stored separately in `Micro.Versus.Results.v1` in PlayerPrefs and submitted as one multiplayer payload. The receiver writes adjacent P1/P2 rows. A failed HTTP attempt is retried once with the same match ID; there is no automatic cross-launch resend of older failed uploads. Results report local/cloud status separately.

The user subsequently authorized automated and visual checks. Compilation, scene integration and rendered 720p/1080p views were checked on 2026-10-28; results and remaining verification limits are recorded in `verification/versus-validation/RESULTS.md`. Current hybrid-controller verification is summarized in [verification summary](docs/verification-summary.md). No two-board physical session, new WebGL build or live Sheets upload was performed in that run.

### Single Player

1. Create a nickname profile or select an existing profile.
2. Choose Easy, Normal or Hard.
3. Move the mascot with the left and right inputs.
4. Catch apples, bread and cake to score and extend the combo.
5. Avoid cans and banana peels. Eating rubbish reduces HP.
6. Survive the 60-second round for a Triumph result, or continue to Results when HP reaches zero.
7. Review the final score, highest rank, item counts and available input statistics.

Difficulty changes the falling speed, spawn interval, rubbish probability and visual environment. Easy contains no rubbish, while Normal and Hard introduce hazards and progressively faster spawning.

### Development controls

- `A` or Left Arrow: simulate the left-hand input.
- `D`, `B` or Right Arrow: simulate the right-hand input.
- Spacebar or Escape: Pause/Resume, with typing, modal and sensor-readiness guards.
- Keyboard strength is binary and displayed as `0%` or `100%`.
- Keyboard input must not be interpreted as measured force in newtons.

### USB controller setup

1. Upload [Sensor_Controller.ino](Firmware/ESP32/Sensor_Controller/Sensor_Controller.ino) to the ESP32, then close Serial Monitor.
2. On Home, open **Controller** and select Keyboard, FSR or Load Cell. This selection is global, independent of the selected profile.
3. In Windows Editor, select the board's COM port and Connect USB. In a Web Serial browser, open the game on HTTPS or localhost and click Connect USB to choose the port. Both use 115200 baud.
4. FSR shows relative press levels from 0–100%. For Load Cell, unload both sensors and press **Tare**, then wait for fresh readings before starting.

| Input | ESP32 pins | Current firmware values |
| --- | --- | --- |
| Left FSR, replacement in 2.1.0 | GPIO34 | Released 4095; reference press 156.5 |
| Right FSR | GPIO35 | Released 4095; reference press 156.5 |
| Left Load Cell, 1 kg | DT32 / SCK18 | −2449.84 counts/g |
| Right Load Cell, 5 kg | DT33 / SCK4 | −429.11 counts/g |

The single firmware supports switching controller type without reflashing. The round locks its controller mode and thresholds. Stale input, USB loss or a board restart pauses the game before further gameplay updates; recover through Controller in Pause, then resume explicitly.

Thresholds are editable in `Unity/Assets/CPS/Resources/ControllerSettings.asset`: FSR press/release 20%/15%, Load Cell 0.50/0.30 N, separately for each hand. Load Cell calibration remains provisional; validate known weights after assembly. Both FSR release readings are at the ADC ceiling, so some wiring faults cannot be distinguished from release.

See the [USB controller guide](docs/unity-usb-controller-guide.md) and [firmware README](Firmware/ESP32/Sensor_Controller/README.md) for details. The bench sketches in `Firmware/ESP32/FSR_Reading` and `Firmware/ESP32/HX711_Reading` retain their earlier standalone calibration workflows.

## Data recorded after a round

The result payload can include:

- Session and profile identifiers
- Display name and difficulty
- End reason, play duration, score and remaining HP
- Food caught, food missed and rubbish eaten
- Maximum combo and highest rank
- Input source and left/right activity duration
- Normalized strength statistics
- Force and balance statistics when a calibrated sensor source supplies them

Profiles are local nicknames, not authenticated accounts. The project does not provide clinical interpretation of the recorded data.

## Technology

- Unity `6000.3.25f1` / Unity 6.3 LTS
- Universal Render Pipeline 2D and Unity uGUI
- Unity Input System
- ESP32, NUCLEO-G071RB, HX711 and USB telemetry (ESP32 JSON; STM32 raw bench)
- Google Apps Script and Google Sheets result endpoint
- Target platform: WebGL in a desktop browser, landscape orientation

## Open and test the project

1. Clone this repository.
2. Add and open the `Unity` directory in Unity Hub using Unity `6000.3.25f1`.
3. Open `Unity/Assets/CPS/Scenes/Boot.unity` from the repository, or `Assets/CPS/Scenes/Boot.unity` inside Unity.
4. Enter Play Mode.

See the [Unity setup and WebGL guide](docs/unity-setup-guide.md) for build instructions and the [playtest checklist](docs/unity-playtest-checklist.md) for the expected flow.

The Unity project consists of `Unity/Assets`, `Unity/Packages` and `Unity/ProjectSettings`. Generated folders such as `Unity/Library`, `Unity/Temp`, `Unity/Logs`, local builds and IDE project files are intentionally excluded.

## Version history

See [CHANGELOG.md](CHANGELOG.md) for Micro-Project Version 0.1.0 and the inherited CPS history. `VERSION` and Unity `bundleVersion` are both `0.1.0`; the wire-protocol version remains `1`.

## Verification

The USB implementation passed 189/189 EditMode tests, 106/107 PlayMode tests, seven JS bridge tests and ten combined-firmware host cases. The remaining PlayMode failure is an existing audio-volume expectation (0.3 expected versus the configured 0.99). WebGL was built successfully and checked in a browser. The user subsequently confirmed gameplay with their replacement left FSR; its host-test ADC inputs now match the revised firmware.

Detailed evidence and remaining calibration/device checks are recorded in [the USB verification report](verification/unity-usb/RESULTS.md). Keyboard/FSR results have no measured force; Load Cell results show Max/Average N without inventing percentage measurements. The numeric controller readouts use a fallback font because the trial font's percent glyph is blank.

## Temporary classroom assets

The repository retains classroom-demo audio sources in `sound` and temporarily embeds `FFSpokenTrial-Regular.ttf`. Their presence records the material used during development and does not establish redistribution rights. Replace them with properly licensed production assets before publishing the game outside the coursework demonstration.

Runtime audio in `Unity/Assets/CPS/Resources/LocalDemoAudio` remains local and ignored by Git. For the classroom setup, copy the supplied MP3s from `sound` into that folder with their existing filenames; otherwise the optional local audio remains silent.

The recorded audio source links and detailed asset provenance are available in [the font and audio brief](docs/unity-font-audio-brief.md) and [the asset provenance report](docs/unity-asset-provenance.md).

