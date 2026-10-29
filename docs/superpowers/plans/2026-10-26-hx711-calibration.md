# HX711 Calibration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Read two provisionally calibrated load cells in grams and Estimated Force (N), with Serial tare.

**Architecture:** Keep the existing HX711 sketch and installed HX711 library. Use separate factors and tare-valid state per channel; share only the small read/report and tare helpers. No transport or Unity changes.

**Tech Stack:** Arduino C++, existing HX711 library, installed arduino-cli.

**Spec:** docs/superpowers/specs/2026-10-26-hx711-calibration-design.md

## Global Constraints

- DT32/SCK18: 1 kg, -2449.84 counts/g. DT33/SCK4: 5 kg, -429.11 counts/g.
- 115200 baud; signed grams; N = grams * 0.00980665; `t`/`T` requests unloaded tare.
- No upload, dependency installation, Unity, MQTT or FSR changes.

## Review Focus

- Missing channel cannot prevent reporting from the working channel.
- Failed startup tare cannot yield apparently calibrated measurements.
- Tare failure must be reported and a later explicit tare must allow recovery.
- Negative factors must yield positive grams for the observed negative loaded counts; negative measured force must remain signed.
- Serial line endings must not repeatedly trigger tare.

### Task 1: Calibrated bench sketch

**Files:** modify `HX711_Reading/HX711_Reading.ino`; create `HX711_Reading/README.md`; add focused host verification under `verification/hx711-calibration/` if a host compiler is available.

**Interfaces:** consume the installed HX711 API (`begin`, readiness/timeout, `tare`, `set_scale`, `get_units`); expose readable Serial output and `t`/`T` commands only.

- [x] Inspect the installed HX711 header and available ESP32 cores; do not install or upload anything.
- [x] Add focused tests for the five Review Focus cases before changing behavior, using a stubbed HX711/Serial interface where practical.
- [x] Implement visible per-channel constants, startup/user tare, independent channel output, bounded initial readiness waits, grams/N conversion and explicit unavailable/uncalibrated status.
- [x] Run the focused checks and compile for an installed compatible ESP32 target; document any missing tools instead of claiming a board build passed.
- [x] Document pins, factors, unloaded startup/tare instructions, 83 g inconsistency, and the user's physical 83/228/525 g check. No commit or flash is part of this task.
