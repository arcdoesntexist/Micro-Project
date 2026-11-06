# Single Player USB dropdown and sensor feedback

Date: 2026-11-06, Asia/Bangkok.

## Scope and causes

The user reported a broken COM-port dropdown and unavailable FSR/Load Cell readings in Single Player, and clarified that both sensor modes should use ESP32. The supplied screenshot showed COM4 connected, with N/A values. Windows identified COM4 as ST-LINK and COM3 as the ESP32 CP210x bridge during this check. COM numbers are observations for this machine, not fixed assignments.

The persisted serial-port dropdowns in both Home and Pause lacked the scrolling/masked template used by the repaired Profile lists. ControllerUISetup had cloned an earlier template and did not repair existing panel dropdowns. Opening a port alone also did not mean its telemetry matched the Single Player JSON contract; raw bench input was silently rejected without an actionable explanation.

## Changes

- Reused ProfileDropdownSetup.Configure for serial-port lists in ControllerUISetup. Regenerated only MainMenuCanvas and GameplayCanvas controller templates and synchronized the tested prefabs into the repository copy.
- Added a nonfatal protocol hint when a newly connected port sends raw FSR bench lines instead of valid Single Player telemetry. These lines remain rejected and do not refresh input. Subsequent valid JSON clears the hint.
- Updated the current USB guide with ESP32-port selection, raw-firmware feedback and the existing unload-both-cells/Tare workflow. Firmware, sensor coefficients and gameplay rules were not changed.
- Preserved the earlier uncommitted left-FSR restoration. No commit or push was performed for this follow-up.

## Verification

Actor classes: user screenshot and clarification; Windows device inventory; direct serial read; automated Unity tests and prefab rendering. Tested on Windows with Unity 6000.3.25f1 in the active Micro/Unity project. Tested source/prefabs were matched by hashes in Micro/Micro-Project/Unity; that copy was not compiled independently.

- RED: two prefab regression cases failed because their dropdown templates had no ScrollRect. The protocol regression failed because raw firmware left LastError unset. Both behaviors passed after the fixes.
- Full EditMode: 228 passed, 0 failed.
- Full PlayMode: 146 passed, 0 failed, 1 skipped. The skipped case is the optional physical STM32 test, outside this Single Player ESP32 check.
- A direct COM3 read before the fix captured 61 version-1 JSON frames in approximately three seconds, with the expected calibration identifier. Both Load Cell channels were initially untared.
- The user explicitly confirmed that both Load Cells were unloaded before the physical Tare check. The Unity test selected FSR and COM3 through the real Controller panel buttons, connected NativeSerialTransport, and observed live percentages for both FSR channels.
- The same Unity test selected Load Cell and pressed the real Tare button. Both channels acknowledged the new tare and displayed numeric N readings. This establishes data availability and handshake operation, not reference-load accuracy or human press-response calibration.
- Popup regression showed two distinct readable COM choices and selected the second option successfully. Inspected fresh 720p/1080p screenshots of the repaired popup and physical ESP32 FSR/Load Cell panel.
- Original prefab copies matched before synchronization. Source and prefab hashes matched between both approved copies after testing. Raw reports/backups remain locally under Micro/verification/2026-11-06-singleplayer-usb, outside the repository snapshot.

## Captures

- [Repaired COM dropdown](port-dropdown-1280.png)
- [ESP32 FSR readings](physical-esp32-fsr-1280.png)
- [ESP32 Load Cell readings after Tare](physical-esp32-load-1280.png)

## Limits and next action

No known reference weights or deliberate human press sequence were tested. Existing Load Cell force coefficients remain provisional. No new firmware, WebGL/standalone build or live Sheets submission was made. The test closes the serial connection afterwards; each new Load Cell connection still requires the normal explicit Tare confirmation.

In Single Player, Disconnect any wrong port, choose the ESP32 port (COM3 in this check), and Connect USB. FSR readings should be available from fresh JSON. For Load Cell, unload both sensors and press Tare both before starting. STM32/ST-LINK raw FSR remains part of the separate Versus path.
