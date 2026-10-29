# Micro Project Layout Design

## Intent

Create an independent Microcontroller-course workspace derived from CPS Project Version 2.1.0. The workspace must clearly separate the Unity application, ESP32 firmware, future STM32 firmware, shared protocol, documentation, artwork, and verification evidence while preserving the working Unity implementation.

The new project identity is **Micro-Project Version 0.1.0**. The original CPS repository and its working checkout remain unchanged.

## Target Structure

```text
Micro/
├─ Unity/
│  ├─ Assets/
│  ├─ Packages/
│  ├─ ProjectSettings/
│  ├─ Library/                 # generated, ignored
│  └─ UserSettings/            # generated, ignored
├─ Firmware/
│  ├─ ESP32/
│  │  ├─ Sensor_Controller/
│  │  ├─ FSR_Reading/
│  │  └─ HX711_Reading/
│  └─ STM32/
│     └─ README.md             # scope placeholder until STM32 code exists
├─ sensor-protocol/
├─ google-apps-script/
├─ art/
├─ sound/
├─ docs/
├─ verification/
├─ README.md
├─ CHANGELOG.md
├─ DESIGN.md
├─ PRODUCT.md
├─ VERSION                     # 0.1.0
├─ .gitattributes
└─ .gitignore                  # repository-level rules
```

## Selected Approach

Use a course monorepo with Unity under `Unity/` and hardware firmware under `Firmware/`. This keeps generated Unity files away from firmware and makes the planned STM32/ESP32 split explicit.

Alternatives considered:

- Keeping Unity at the repository root would remain closest to CPS but would mix Unity, firmware, and course artifacts.
- Renaming namespaces, assembly definitions, and `Assets/CPS` would make implementation names fully match Micro but creates unnecessary risk to serialized Unity references.

## Moves and Cleanup

- Keep `Assets`, `Packages`, and `ProjectSettings` under `Unity/`.
- Move project-wide `docs/` from `Unity/` to the Micro root.
- Move `Sensor_Controller`, `FSR_Reading`, and `HX711_Reading` under `Firmware/ESP32/`.
- Create `Firmware/STM32/README.md` stating that STM32 responsibilities and source are not implemented yet.
- Keep `sensor-protocol`, `google-apps-script`, `art`, `sound`, and `verification` at the Micro root.
- Remove stale generated `Micro.slnx` and root `.vsconfig`; Unity may regenerate IDE files inside `Unity/`.
- Keep a Unity-specific ignore file under `Unity/` and add repository-level ignore rules at the Micro root for IDE, firmware-build, temporary, and generated output.

## Rename Scope

Change current-project identity:

- Root `VERSION`: `0.1.0`.
- Unity `bundleVersion`: `0.1.0`.
- Unity `productName`, `projectName`, `metroPackageName`, and `metroApplicationDescription`: `Micro-Project`.
- Unity Android and Standalone application identifiers: `com.DefaultCompany.microproject`.
- Root README title and current-project wording: Micro-Project, with CPS Project Version 2.1.0 identified as the source baseline.
- Changelog: add Version 0.1.0 as the Micro-course fork baseline.

Preserve implementation and historical identity:

- Keep `Assets/CPS`, namespaces under `CPS.KitchenCatch`, and assembly names such as `CPS.Runtime`.
- Keep existing scene names, gameplay title **Kitchen Catch**, protocol version, profile keys, and Google Sheets payload contracts.
- Do not rewrite archived execution reports, old absolute paths, release evidence, or historical CPS version descriptions as if they originally belonged to Micro.

## Functional Boundaries

This change reorganizes and re-identifies the duplicated workspace only. It does not add STM32 logic, change ESP32 behavior, redesign gameplay, change telemetry schema, initialize Git, publish, push, or modify either CPS source directory.

## Verification

- `C:\Users\Arc\Downloads\Micro\Unity` opens with Unity `6000.3.25f1` and contains `Assets`, `Packages`, and `ProjectSettings`.
- Unity imports and compiles after relocation.
- EditMode retains the Version 2.1.0 baseline of 189 passing tests.
- PlayMode retains 106 passing tests and only the known audio-volume assertion failure unless the renamed product identity legitimately changes a test expectation.
- Every Unity asset still has its matching `.meta` file and scene/build references resolve.
- No stale root solution file points to missing `.csproj` files.
- Root documentation links to moved firmware and Unity paths resolve.
- The CPS source checkout and separate CPS development checkout are not modified.
