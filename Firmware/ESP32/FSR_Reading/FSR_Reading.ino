#include <Arduino.h>

const uint8_t FSR_LEFT_PIN = 34;
const uint8_t FSR_RIGHT_PIN = 35;
const float LEFT_RELEASED = 551.0f;
const float LEFT_PRESSED = 135.0f;
const float RIGHT_RELEASED = 4095.0f;
const float RIGHT_PRESSED = 156.5f;
const float FILTER_ALPHA = 0.2f;
const unsigned long SAMPLE_INTERVAL_MS = 20;
const unsigned long REPORT_INTERVAL_MS = 100;

int leftRaw;
int rightRaw;
float leftFiltered;
float rightFiltered;
unsigned long lastSample;
unsigned long lastReport;

float pressurePercent(float reading, float released, float pressed) {
  float percent = (released - reading) * 100.0f / (released - pressed);
  return constrain(percent, 0.0f, 100.0f);
}

void reportLevel(const char* side, int raw, float filtered, float released, float pressed) {
  Serial.print("FSR ");
  Serial.print(side);
  Serial.print(": raw=");
  Serial.print(raw);
  Serial.print(" filtered=");
  Serial.print(filtered, 1);
  Serial.print(" level=");
  Serial.print(pressurePercent(filtered, released, pressed), 1);
  Serial.println("%");
}

void setup() {
  Serial.begin(115200);
  analogReadResolution(12);
  leftRaw = analogRead(FSR_LEFT_PIN);
  rightRaw = analogRead(FSR_RIGHT_PIN);
  leftFiltered = leftRaw;
  rightFiltered = rightRaw;
  lastSample = lastReport = millis();
  Serial.println("Relative press level 0-100%, not force in N.");
}

void loop() {
  unsigned long now = millis();
  if (now - lastSample >= SAMPLE_INTERVAL_MS) {
    lastSample = now;
    leftRaw = analogRead(FSR_LEFT_PIN);
    rightRaw = analogRead(FSR_RIGHT_PIN);
    leftFiltered += FILTER_ALPHA * (leftRaw - leftFiltered);
    rightFiltered += FILTER_ALPHA * (rightRaw - rightFiltered);
  }
  if (now - lastReport >= REPORT_INTERVAL_MS) {
    lastReport = now;
    reportLevel("Left", leftRaw, leftFiltered, LEFT_RELEASED, LEFT_PRESSED);
    reportLevel("Right", rightRaw, rightFiltered, RIGHT_RELEASED, RIGHT_PRESSED);
  }
}
