# HX711 calibrated serial readings

Approved in conversation: use the 525 g-derived factors, cross-checked against the user's 228 g phone, for provisional Estimated Force (N). Modify only the standalone HX711 sketch; no Unity, MQTT or FSR changes and no firmware upload.

## Approaches

- Provisional factors from the existing data: selected by the user. DT32/SCK18 (1 kg) uses -2449.84 counts/g; DT33/SCK4 (5 kg) uses -429.11 counts/g. The phone check computed 227.33 g and 227.56 g respectively.
- Collect repeated unloaded/loaded measurements first: considered; not selected for this trial. The 83 g results remain inconsistent, so no full-range accuracy claim is made.

## Behavior

Serial at 115200 baud reports each channel's signed grams and Estimated Force (N), calculated as grams * 0.00980665. Preserve negative readings for diagnosis instead of clamping or taking absolute values. Tare with no applied load at startup and on `t`/`T` from Serial Monitor; ignore newline characters. Never reuse historical tare offsets from the logs.

Implementation choices proposed for this small change: bounded readiness checks before tare, independent channel readiness/reporting, and a short measurement average for stable bench readings. A channel whose startup tare failed must not report a calibrated value until a successful tare. Keep factors visible as constants. No automatic zero tracking, persistence, game sensitivity mapping, new library or hardware writes.

Verification includes conversion/sign tests and disconnected-channel behavior with a host stub if practical, plus an Arduino compile using the installed board core when available. Neither compilation nor stub tests establish physical calibration accuracy.
