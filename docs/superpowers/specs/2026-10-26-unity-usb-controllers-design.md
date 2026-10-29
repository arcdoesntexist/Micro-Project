# Unity USB controllers — approved design

Status: design approved by the user on 2026-10-26, including Spacebar Pause/Resume. User has chosen separate controller modes, USB Serial for Editor and WebGL, and one ESP32 carrying both FSRs and both load cells. Existing game rules and approved visual design stay unchanged. Implementation plan review follows this approval.

## Intent and scope

University students/instructors choose Keyboard, FSR or Load Cell in Home, test the selected controller in the existing practice area, then play the same sixty-second game. Display and collect the correct units without mixing simulated keys with sensor force. One combined firmware supports menu switching without reflashing.

No MQTT/HiveMQ/Node-RED integration, hardware upload, new artwork, profile redesign, score/rule changes, clinical interpretation or claimed full-range calibration accuracy. Existing Google Sheets columns and upload behavior remain unchanged. Keep the standalone FSR_Reading and HX711_Reading bench sketches available.

## Approaches considered and selected

- USB Serial: selected by user. Native Serial in Windows Editor; Web Serial API plus a Unity JavaScript bridge in WebGL. Same packet parser and input conversion after transport. Requires an ESP32 physically connected to the computer playing the game and a supported browser.
- MQTT/HiveMQ: discussed but deferred. Fits wireless/cloud workflows but is not necessary for this cabled controller step.
- Separate per-mode firmware: not selected. A combined sketch on the single board is required so the Home mode selector actually switches inputs without firmware changes.

## Confirmed hardware and calibration

| Input | Pins | Calibration |
|---|---|---|
| FSR Left | GPIO34 | released 551, pressed reference 135 |
| FSR Right | GPIO35 | released 4095, pressed reference 156.5 |
| Load Cell CH1, 1 kg | DT32 / SCK18 | -2449.84 counts/g |
| Load Cell CH2, 5 kg | DT33 / SCK4 | -429.11 counts/g |

Approved hand mapping: CH1 is Left and CH2 is Right. This was an author proposal accepted at design approval, not a previously measured fact. FSR normalization remains the approved descending/clamped mapping, 12-bit ADC and alpha-0.2 smoothing. FSR output is relative percent, never N or a percentage of true maximum hand strength. Right released 4095 remains saturated; do not claim sensor-disconnect or light-press-resolution detection from it.

Load Cell uses the existing provisional factors and grams * 0.00980665 for signed diagnostic N. The 83 g discrepancy remains unresolved; phone checks at 228 g were approximately 227.33 g and 227.56 g. No arbitrary scaling by rated 1 kg/5 kg capacity. Negative diagnostic force stays available; the unidirectional gameplay/statistics force is max(0, signed N), never abs(). These are estimates on the mounted sensor, not clinical grip force.

## Controller behavior

- Home selector: Keyboard / FSR / Load Cell. Default Keyboard on first use; remember the selection globally on the computer/browser, not inside a profile. Selected mode is frozen when starting a round and cannot be switched in Pause.
- Keyboard: A or Left Arrow = left; D, B or Right Arrow = right. Preserve Escape/Pause and existing UI navigation. B is an additional alias requested by the user, not a replacement for D.
- Spacebar toggles Pause/Resume, with Escape and on-screen controls retained. Suppress the gameplay shortcut while an InputField/text-entry target or a settings/connection modal is active, and prevent the same Spacebar press also submitting a focused UI button. Resume must obey selected-sensor readiness after disconnect/reset. Terminal/Results screens never resume a round via Spacebar.
- FSR: show 0–100% and collect percentage statistics only; Force N is unavailable. Initial press threshold 20%, release threshold 15%, separately configurable per hand. Hysteresis prevents repeated press/release toggles near one threshold; no remapping/dead-zone distortion of the displayed percentage.
- Load Cell: show Estimated Force (N), collect force Max/Average and press durations. Initial press threshold 0.50 N, release threshold 0.30 N, separately configurable per hand. Do not display an invented 0–100% strength; percentage statistics are unavailable in this mode.
- Thresholds are engineering starting points, not calibrated strength or medical limits. Keep them editable in an Inspector configuration; a new end-user tuning UI is not required.
- One hand pressed moves that way; neither or both means stop, at the existing constant speed. Strength/force never increases movement speed or scoring.
- Keyboard works when Keyboard is selected. No automatic keyboard override/fallback while a sensor mode is selected, because mixing sources would mislabel results. Users can return Home and explicitly select Keyboard.
- Home practice uses the same selected input/thresholds as Gameplay, retaining the existing text-entry, modal and focus guards. Sensor movement and key movement both stop on application focus loss.

## Home connection UI and visual boundary

Operate mode: add a compact Controller control and small connection/settings subpanel using existing wood/cream/orange assets and FF Spoken font, with numeric glyph fallback as needed. Reuse current Home and practice layout; do not replace approved art or obscure Game Rules/Profile actions. Show selected mode, platform connection state, per-hand live values, Connect/Disconnect and Load Cell Tare. The same connection subpanel is available from Pause for recovering a suspended round, but its controller selector is locked to that round's source. Keep keyboard navigation and return focus after closing the panel.

Editor: let the user refresh/select a serial port and Connect; use 115200 baud. Do not automatically scan/open all ports. Report permission/in-use/missing-port errors without freezing the main thread. Closing Serial Monitor may be necessary. Do not assume a particular COM number.

Web: use feature detection, explain unsupported browser/insecure context, and use a real DOM click/touch button for the browser port chooser. A Unity frame-delayed OnClick is not a reliable browser user-activation source. The native browser-facing button may be a small styled overlay aligned with the connection panel; it appears only while that panel is visible and respects the letterboxed canvas bounds. No hidden auto-permission requests or full-screen overlay swallowing gameplay keys.

HTTPS or localhost/loopback is required for Web Serial. No promise of Safari/iPhone support or arbitrary embedded/remote preview support. Permission cancellation keeps Home usable. A successful port-open is not the same as valid telemetry; show Connecting until a supported fresh packet arrives. Keyboard mode remains playable without serial permissions.

## Single-board firmware and packet boundary

Create a new combined Arduino sketch under the documentation workspace (e.g. Sensor_Controller/), using installed Rob Tillaart HX711 0.6.5 and native Arduino APIs only. Do not overwrite the two standalone bench sketches or add a second serial connection. Preserve ADC attenuation/defaults used for calibration.

One newline-delimited JSON telemetry stream at 115200 baud, approximately 20 packets/s; packets contain protocol version, boot identity, sequence number, calibration version, FSR left/right percentages, signed load-cell left/right N, per-load-cell tare/valid state and sample age. Choose clear field names in the implementation plan and share exact fixtures across firmware/parser tests. Maximum accepted line length: 1024 characters. Diagnostic startup lines must not be mistaken for telemetry; valid-state false/null force must never turn into a valid 0 N by JSON defaults.

Read/filter FSR independently every 20 ms. Read at most one sample from each ready HX711 per loop and maintain the three-sample force average without blocking for subsequent samples. The display remains smooth at render rate but new force samples are limited by actual HX711 data rate; do not claim 60 Hz hardware sensing. Avoid the installed library's blocking begin/reset path and do not use blocking multi-sample tare/read helpers in the combined loop.

Combined firmware starts with load cells uncalibrated. The UI instructs the user to remove force from both load cells, then Tare sends `t\n` once. Collect five fresh samples per channel asynchronously with a bounded wait (1000 ms without the next ready sample fails that channel). Reset its smoothing history on successful tare and fail closed on partial tare. FSR remains usable while HX711 is absent or taring. The tiny readiness-to-read physical-disconnect race inside the upstream library remains a documented hardware limitation.

Once the port is connected, switching FSR/Load Cell in Home selects different fields of the same stream, without disconnect/reflash. Tare and connection changes are available in Home or while a round is paused, never during an advancing countdown/Playing/terminal animation. Pause exposes Connect/Reconnect and Tare (with the unload instruction) without changing mode or discarding the suspended round. Resume is disabled until the selected source is fully valid/fresh again; Load Cell reset/reboot requires a new successful tare. Browser permission must not silently resume control after a new device/reset.

## Unity architecture and validity

Extend the existing IPlayerInput/InputSnapshot path with a small selected-controller router and shared packet state; preserve current Initialize/test injection. Both Home practice and GameSessionController consume that path. The connection owner survives scene transitions, owns one port/read loop, and cleans it up on disconnect, Play exit and destruction. No scene-bound worker thread may call Unity APIs.

Native Windows Editor transport is platform-gated so WebGL never links System.IO.Ports. Verify the available Unity runtime/SerialPort assembly before choosing the smallest native reference; if a new binary/package is necessary, document its source/license and ask before installation. Native reads and cleanup have bounded waits and bounded buffering. Web uses a .jslib/browser bridge with a separate bounded line buffer and explicit close/cancel handling; no new browser framework is needed.

Validate schema/version/required fields, finite numeric values, range 0–100 for FSR, per-hand valid/sample-age fields and fresh sequence before replacing the latest packet. Use installed Newtonsoft.Json rather than adding a parser library. Ignore duplicate/out-of-order packets without refreshing their received time; use boot identity for device resets and handle uint32 sequence/millis rollover. Accept a new boot only as a new uncalibrated connection, clearing old held/force state. Discard invalid/oversized lines without unbounded accumulation. Native and Web paths share these rules.

Telemetry becomes stale after 750 ms. For Load Cell, include source sample age as well as packet transit/receive age; a heartbeat carrying old force must not refresh that force's validity. In sensor mode both selected hands must be valid and fresh before Start. FSR validity does not depend on Load Cell calibration/readiness.

On selected sensor loss, invalid selected hand or stale input during Ready/Playing: release movement immediately, pause the countdown/game and show which connection needs attention. No HP/time/force statistics accrue while paused. On reconnect/reset/tare recovery, remain paused until both selected hands are fresh and the user explicitly resumes. Keyboard mode must not be paused merely because serial is disconnected. Focus loss similarly releases held input and preserves the existing pause flow.

## Statistics and compatibility

Freeze the round's input source as Keyboard, FSR or LoadCell. Reuse time-weighted valid-input statistics. Extend HandInput's availability semantics so a valid load-cell press/force can exist without fabricating a percentage sample. Existing constructors and Keyboard behavior retain their defaults.

HUD: Keyboard/FSR show percent; Load Cell shows N and hides percentage fills rather than using N as a fill fraction. Unavailable values show N/A, not 0. Result main/details preserve the approved layout and caught-food/trash rules; label physical force Estimated Force (N), label Keyboard simulated, label FSR relative press level. Handle per-hand availability rather than hiding one valid hand because the other has no force. Store the calibration version with sensor SessionResult snapshots; keep old results loadable. Existing Sheets mapping/columns are unchanged in this step; no N fabrication in Keyboard/FSR uploads.

## Verification and delivery

Tests: firmware normalization, async HX711/tare failure/recovery, JSON fixtures; parser malformed/partial/oversized/nonfinite/range/sequence/reset/stale cases; selected input, hysteresis, keyboard aliases, simultaneous press/focus; force-vs-percent stats; scene transitions, Home practice, pause-on-loss and manual resume. Use production data paths in tests, not just string presence checks.

Compile combined firmware with installed ESP32 core (representative target until exact board variant is confirmed). Run scoped Unity EditMode/PlayMode tests and report known pre-existing audio expectation failures separately. Build WebGL and inspect connection UI at 1280x720 and 1920x1080 in one bounded visual pass. Browser smoke checks include unavailable API, cancellation/error cleanup and JS bridge/stream fixtures; real permission, physical stream latency and USB reset behavior require the user's ESP32 test and must not be claimed from mocks.

Provide combined sketch, Unity changes, wiring/flash/connect instructions and a short test checklist. The user uploads firmware; no automatic flash, port open, publish, GitHub push or unrelated scene/settings overwrite is authorized. Keep .md files under C:/Users/Arc/Downloads/CPS/Unity_Project. Implementation planning follows approval of this design.

## Author choices accepted at design approval

CH1 is Left and CH2 Right. Initial press/release thresholds are FSR 20/15% and Load Cell 0.50/0.30 N, adjustable per hand. Connection loss auto-pauses rather than killing the run or silently switching to keys. Existing warm UI stays; native Web Serial picker requires a DOM button bridge. The user accepted this design and added Spacebar Pause/Resume.

## Builder check

Fresh read-only builder review identified one recovery-flow conflict: Home-only Connect/Tare could not recover a paused Load Cell round after board reset. Resolved as an explicit author choice above: reuse the connection panel from Pause, keep mode locked, allow reconnect/tare only while stopped, and require explicit Resume after valid telemetry. No other important question was reported. The resulting design was approved before implementation planning.
