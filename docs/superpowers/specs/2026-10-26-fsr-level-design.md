# FSR relative pressure level

Small design approved in chat: use separate descending raw ranges from Yahoo.txt, confirmed by the user as fully released at Left 551 and Right 4095. Left pressed reference is 135; Right is 156.5. Output is a relative 0–100% level, not N or a percentage of actual hand strength.

Approaches: whole-ADC mapping was considered but rejected in favor of these measured per-side ranges, so Left can use its actual travel. Clamp readings outside the endpoints to 0–100%. Preserve raw readings for diagnosis. Right's released ADC is at the 12-bit ceiling, so fine response at light pressure remains unverified.

Implementation choices: retain GPIO34/35, explicitly select 12-bit ADC without changing attenuation; sample every 20 ms, apply a simple exponential filter (alpha 0.2) seeded from the first actual samples, and print raw/filtered/percentage at 115200 baud every 100 ms. No dead-zone remapping, tare, persistence, extra libraries, HX711, Unity or MQTT changes. Constants remain visible for manual adjustment.

Verify actual sketch behavior with host ADC/Serial stubs and compile for installed generic ESP32. Do not upload or claim physical calibration quality. Keep the user's native execution preference and existing checkout.
