# Single Player Micro-DB cutover — 2026-11-06

## Scope and change

Actor: Codex automated checks on Windows, Unity 6000.3.25f1, native Editor Test Runner. Updated both `Micro/Unity` and `Micro/Micro-Project/Unity`.

Single Player retained the CPS deployment URL in `GameplayRig.prefab` and the `GameSceneSetup.ResultEndpoint` authoring constant. Both now use the existing Micro deployment already used by Versus. This prevents scene regeneration from restoring the inherited destination. No payload contract or Apps Script source change was required: a payload without `mode` routes to `Singleplayer` in the existing receiver.

Destination spreadsheet: `1GLTRyTaMMfnfRoNknXkFWjkIuikdSJeVK7jJvoXXrXU`, tab `Singleplayer`. See [receiver setup](../../../google-apps-script/README.md).

## Fresh verification

- Regression first failed against the inherited CPS endpoint, then passed after the cutover.
- EditMode: 5/5 passed (GameSceneAuthoringTests and ResultUploadPayloadTests). Checks cover prefab destination, authoring constant, matching Versus endpoint, shared scene configuration and payload contract.
- PlayMode: ResultUploaderTests 4/4 passed, with injected submissions; finished-round submission, session deduplication, empty endpoint and unsubscribe behavior are covered.
- Existing Apps Script contract self-tests passed in a local Node VM without Google service bindings. Additional assertions checked the Micro spreadsheet ID and Singleplayer tab. This verifies local source behavior, not the deployed script version.

Raw XML, logs and before-change backups are local under `Micro/verification/2026-11-06-micro-db-cutover/` and are not repository artifacts.

## Limits and next action

No valid result was posted to a live deployment and no historical rows were moved or deleted. Live persistence into Micro-DB remains to be confirmed with the user's next completed Single Player round. Restart Play Mode after loading the updated project so the uploader reads the new prefab configuration. No new player build or firmware flash was performed.