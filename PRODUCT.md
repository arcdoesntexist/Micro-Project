# CPS Kitchen Catch

<!-- impeccable:product-schema 1 -->

This record consolidates the user's answered brainstorming rounds and approved Unity specification; it introduces no new feature or visual choice. The detailed authority remains [the approved specification](docs/superpowers/specs/2026-10-08-unity-kitchen-catch-design.md).

## Platform

web

Unity WebGL, desktop browser, landscape. The Unity Editor is the development environment, not another shipped UI platform.

## Users

University students and instructors trying a CPS/Sensor coursework demonstration.

## Product Purpose

Control a mascot left and right, catch food and avoid rubbish during a sixty-second round, then inspect score and hand-input statistics.

## Operating Context

Create or choose a local nickname profile, choose Easy/Normal/Hard, play, inspect results, restart or return to the menu. Keyboard simulates the two hand inputs during the Unity-only phase. Future physical sensors and external MQTT/Node-RED/Sheets integration are outside this phase.

## Capabilities and Constraints

- Three HP; surviving the round yields Triumph, zero HP yields a cartoon death and Results. Both routes show the same statistics and navigation.
- Constant-speed movement; input strength does not change movement speed or score. Keyboard strength is simulated 0/100, not a force measurement.
- Local profiles support duplicate names and unique IDs; no password, account, cloud sync, or clinical interpretation.
- Fixed 16:9 playfield and UI viewport, letterbox/pillarbox for other browser shapes; target 60 FPS rendering with stepped sprite animation.
- Font and audio choice belongs to the user and is pending. No distribution license or Thai glyph coverage is assumed for a temporary font.

## Brand Commitments

Original cream blob mascot with orange feet and a tiny orange engineer helmet bearing `IoTE`. Warm kitchen, pixel-cartoony artwork, expressive eating and cartoon effects. DMC-style rank progression is adapted to original kitchen names and graphics, not ripped game assets.

## Evidence on Hand

Approved character, gameplay, rank and death references are beside the specification. Generated production sources and exact prompts are inventoried in [asset provenance](docs/unity-asset-provenance.md). References are concepts, not playable screenshots or benchmark evidence.

## Product Principles

- Keep the one-minute game understandable and replayable.
- Keep the falling objects and safe movement path legible.
- Report input source and units honestly.
- Keep the coursework scope lean and preserve existing project edits.

## Accessibility & Inclusion

Thai and Latin nicknames, plain-text names without markup interpretation, mouse and keyboard menu navigation, clearly visible focus and HP states. Font acceptance requires the user's licensed font and visual verification of Thai combining marks.
