#include <Arduino.h>
#include <esp_random.h>
#include "LoadChannel.h"

const uint8_t FSR_LEFT_PIN = 34;
const uint8_t FSR_RIGHT_PIN = 35;
const float LEFT_RELEASED = 4095.0f;
const float LEFT_PRESSED = 156.5f;
const float RIGHT_RELEASED = 4095.0f;
const float RIGHT_PRESSED = 156.5f;
const float FILTER_ALPHA = 0.2f;
const char CALIBRATION_VERSION[] = "cps-2026-10-26-v1";
LoadChannel loadLeft(-2449.84f);
LoadChannel loadRight(-429.11f);
float fsrLeft;
float fsrRight;
uint32_t bootId;
uint32_t sequence = 0;
uint32_t tareId = 0;
uint32_t lastFsrMs;
uint32_t lastReportMs;

float relativeLevel(float reading, float released, float pressed) {
  return constrain((released - reading) * 100.0f / (released - pressed), 0.0f, 100.0f);
}

void printLoad(const char* side, const LoadChannel& channel, uint32_t now) {
  bool valid = channel.valid(now);
  Serial.print(",\"load"); Serial.print(side); Serial.print("N\":");
  if (valid) Serial.print(channel.force(), 6); else Serial.print("null");
  Serial.print(",\"load"); Serial.print(side); Serial.print("Tared\":");
  Serial.print(channel.tared ? "true" : "false");
  Serial.print(",\"load"); Serial.print(side); Serial.print("Valid\":");
  Serial.print(valid ? "true" : "false");
  Serial.print(",\"load"); Serial.print(side); Serial.print("AgeMs\":");
  if (valid) Serial.print(static_cast<uint32_t>(now - channel.sampleMs)); else Serial.print("null");
}

void publish(uint32_t now) {
  Serial.print("{\"version\":1,\"bootId\":"); Serial.print(bootId);
  Serial.print(",\"sequence\":"); Serial.print(sequence++);
  Serial.print(",\"uptimeMs\":"); Serial.print(now);
  Serial.print(",\"tareId\":"); Serial.print(tareId);
  Serial.print(",\"calibrationVersion\":\""); Serial.print(CALIBRATION_VERSION); Serial.print("\"");
  Serial.print(",\"fsrLeftPercent\":"); Serial.print(relativeLevel(fsrLeft, LEFT_RELEASED, LEFT_PRESSED), 3);
  Serial.print(",\"fsrRightPercent\":"); Serial.print(relativeLevel(fsrRight, RIGHT_RELEASED, RIGHT_PRESSED), 3);
  printLoad("Left", loadLeft, now);
  printLoad("Right", loadRight, now);
  Serial.println("}");
}

void setup() {
  Serial.begin(115200);
  analogReadResolution(12);
  fsrLeft = analogRead(FSR_LEFT_PIN);
  fsrRight = analogRead(FSR_RIGHT_PIN);
  loadLeft.begin(32, 18);
  loadRight.begin(33, 4);
  bootId = esp_random();
  lastFsrMs = lastReportMs = millis();
}

void loop() {
  uint32_t now = millis();
  bool tareRequested = false;
  for (uint8_t readCount = 0; readCount < 64 && Serial.available() > 0; readCount++) {
    int command = Serial.read();
    if (command == 't' || command == 'T') tareRequested = true;
  }
  if (tareRequested) {
    tareId++;
    loadLeft.startTare(now);
    loadRight.startTare(now);
  }
  if (static_cast<uint32_t>(now - lastFsrMs) >= 20) {
    lastFsrMs = now;
    fsrLeft += FILTER_ALPHA * (analogRead(FSR_LEFT_PIN) - fsrLeft);
    fsrRight += FILTER_ALPHA * (analogRead(FSR_RIGHT_PIN) - fsrRight);
  }
  loadLeft.step(now);
  loadRight.step(now);
  if (static_cast<uint32_t>(now - lastReportMs) >= 50) {
    lastReportMs = now;
    publish(now);
  }
}
