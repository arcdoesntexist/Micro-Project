# STM32 left FSR restored

Date: 2026-11-06, Asia/Bangkok.

## Request and scope

The user reported repairing the STM32 left FSR and approved enabling it in both the active `Micro/Unity` project and the Git repository's `Micro/Micro-Project/Unity` copy. Both copies were checked before editing; relevant source/settings matched and the repository was clean. No commit, push, firmware change or documentation migration is included in this follow-up.

No project AGENTS.md or canonical docs index/status/current-plan files were present in these roots. Existing README and USB controller guide were used; no missing handoff documents were invented.

## Changes

- Enabled `stm32LeftEnabled` in both shipped VersusControllerSettings assets. The settings class's fallback default is unchanged; the Resources asset owns the deployed configuration.
- P2's Controllers hint now follows the configured left-channel enable flag, rather than always stating that the left FSR is disabled.
- Keyboard and sensor merging is unchanged. Updated regression coverage exercises independent left/right input, simultaneous input, mixed keyboard/FSR and Ready.
- Tests for intentionally disabled left input explicitly select that scenario and restore the original shared settings afterwards. The optional hardware smoke case checks validity against the configured enable flag.
- Updated the current README/control guide and added a dated changelog entry; older disabled-sensor records remain historical.

## Observations and verification

Actor classes: user-provided hardware log and repair report; automated Unity tests; source/configuration readback. These establish different facts.

- Parsed 464 complete raw pairs from the supplied attachment. Both channels include readings near 4095 and low-value bands. Across the 51 pairs where both channels are below 500, the medians are L=50 and R=105. These are observations from unlabeled samples, not measured-force calibration or newly chosen reference endpoints.
- Fresh targeted Unity results: editmode : 13 passed, 0 skipped, 0 failed; playmode : 16 passed, 1 skipped, 0 failed
- Tested project: the active `Micro/Unity` copy, Unity 6000.3.25f1 on Windows. The repository copy was synchronized and checked by source/settings hashes, not compiled separately.
- Test scope includes restored channels, disabled-channel fallback, parser validation, hysteresis, stale release, keyboard/FSR merging, Ready and existing pause/reconnect/keyboard behavior.
- Visual check: Inspected fresh generated Controllers screenshots at 1920x1080 and 1280x720. P2 shows left/right FSR support and the hint fits its panel. Captures: [1080p](controllers-1080.png), [720p](controllers-720.png).
- Raw local test reports and before-copies remain under `Micro/verification/2026-11-06-stm32-left-restored/`, outside the committed source snapshot. No personal raw log or credentials were copied into the documentation.

## Limits and next action

The shared released/reference endpoints remain provisional 4095/100, with existing 20% press and 15% release hysteresis. This change enables a repaired channel; it does not establish force in newtons or separate physical calibration per hand. The optional direct hardware smoke case was not enabled for this test run. Human operation of the repaired sensor in the game remains a user check.

Open Versus, select/connect the STM32 COM port for P2, and test left, right, both (Ready), and release. Keyboard remains available. Restart Play Mode after the settings reload so the Controllers hint is rebuilt from the new configuration.
