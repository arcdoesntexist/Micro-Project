# Versus keyboard and FSR input

Date: 2026-10-29
Status: approved by the user on 2026-10-29.
Target: C:/Users/Arc/Downloads/Micro/Unity.

## Agreed intent

Both keyboard and FSR controls remain available. ESP32 controls P1 using the existing GPIO34/GPIO35 wiring. NUCLEO-G071RB controls P2 through its own USB connection. P2's left FSR is broken and must not generate movement or Ready input. The user can keep testing before replacing it.

## Approaches

- Switch exclusively between keyboard and FSR: simpler source reporting but prevents using a key for the broken hand while using the healthy FSR.
- Combine each direction from keyboard or its enabled FSR: supports mixed controls and keyboard-only testing. This follows the user's choice that both keyboard and FSR should work.

## Proposed behavior

| Player | Left | Right |
| --- | --- | --- |
| P1 | A or ESP32 left FSR | D or ESP32 right FSR |
| P2 | Left Arrow; STM32 left FSR disabled | Right Arrow or STM32 right FSR |

Each direction is a logical OR, never summed. Pressing both directions retains the game's existing neutral movement / Ready behavior. Ready accepts keyboard, sensors, or a mixture. Input while editing profile names must not accidentally ready players. Practice and gameplay use the same mapping. Keyboard works without any connected board.

Expose an explicit COM-port selection and connect/disconnect control per player in Versus Setup. Do not assign a board by the order Windows enumerates ports. Reject selecting the same port for both players. Preserve connections across the setup-to-gameplay transition. Close or release conflicting existing single-player connections when changing modes, without opening a port twice. Surface connection/error and sensor-enabled status to the user.

If a connected sensor stream stops during an active match, clear its pressed state and pause both fields once. The player may reconnect or explicitly continue using keyboard with a fresh countdown. Do not silently keep stale movement or automatically resume. Keyboard-only matches do not require serial telemetry.

## Current firmware compatibility and calibration

Reuse ESP32's existing validated v1 JSON packets and FSR percentage thresholds. Accept the STM32 bench's strictly validated `FSR raw L=<0..4095> R=<0..4095>` lines in its own adapter, without weakening the existing JSON parser. Diagnostics and malformed/out-of-range lines never refresh freshness. STM32 left is ignored even when its raw value looks pressed.

For the healthy STM32 right sensor, expose released/reference raw endpoints and press/release thresholds in a Micro controller configuration. Proposed provisional endpoints are released 4095 and reference 100, based on the supplied log; these require physical confirmation and are not a force calibration. Use the existing 20% press / 15% release hysteresis as the initial proposal. Clamp normalized input to 0..100 and reject invalid configuration. Report FSR strength as relative percentage, never newtons. Label hybrid input truthfully; keyboard must not fabricate measured force.

Keep STM32's flashed raw bench firmware for this step; no new flash is needed to implement the host adapter. Replacement of the broken FSR and final shared JSON firmware remain later work.

## Platform and boundaries

First delivery uses Windows Unity Editor / Windows player, matching current USB bring-up. Two-controller Web Serial requires extending the separate single-port browser bridge and is outside this proposed step; show its unsupported status clearly instead of claiming browser support. Ordinary Single Player keeps its existing input workflow. Original CPS project, game balance, profiles, artwork and upload schema remain unchanged.

## Verification

Tests cover independent players; simultaneous keyboard/FSR controls; ignored broken channel; matching practice/gameplay/Ready inputs; text entry; malformed packets; hysteresis and invalid calibration; stale/disconnected input release and shared pause; keyboard continuation; duplicate port selection; connection cleanup; and Single Player regression. Verify physical STM32 receive on its selected COM port. Only claim physical ESP32 response if that board is connected and exercised. No mock match uploads to the live sheet.

