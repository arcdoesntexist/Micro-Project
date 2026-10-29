# Verification summary

Snapshot date: 2026-10-29. This repository contains source and configuration; machine-specific caches, raw logs, device identifiers and firmware backups are intentionally omitted.

- Hybrid input integration: EditMode 221/221 passed; PlayMode 144/144 passed.
- Coverage includes keyboard/FSR merging, disabled STM32 left input, malformed data, hysteresis, freshness, reconnect isolation, duplicate ports, text-entry guards, shared pause/recovery, and Single Player regression.
- A queue-overflow reconnect bug was reproduced with a failing regression, fixed with connection-scoped per-player inboxes, and rechecked by a fresh read-only reviewer before the final passing suites.
- Native Unity receive from a physical NUCLEO-G071RB succeeded. Right input was valid, left input ignored, and no newton-force value was generated. The port was released after testing.
- Controllers and Pause overlays were visually checked at 1280x720 and 1920x1080.
- NUCLEO firmware: full CubeIDE Debug build, zero errors/warnings; programmed and read-back verified.
- The later Spacebar Pause/Resume update was confirmed by the user in Play Mode. The 221/144 suite counts precede that small hotkey change.

Physical ESP32 operation together with STM32, human press/release calibration, a new WebGL/standalone build, live Google Sheets submissions and measured frame-rate profiling were not performed in this verification run. FSR percentages remain provisional and are not newtons.

To run automated Unity checks, use Test Runner with the EditMode and PlayMode tabs. The physical STM32 smoke case is optional and requires MICRO_STM32_TEST_PORT set to an available port; otherwise it is skipped. Close other serial terminals first. Test capture/output directories are ignored by Git.
