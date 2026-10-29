#pragma once
#include <Arduino.h>
#include "HX711.h"

struct LoadChannel {
  HX711 scale;
  float countsPerGram;
  bool tared = false;
  bool taring = false;
  float offset = 0;
  float tareTotal = 0;
  uint8_t tareCount = 0;
  uint32_t tareSampleMs = 0;
  float samples[3] = {};
  float sampleTotal = 0;
  uint8_t sampleCount = 0;
  uint8_t nextSample = 0;
  uint32_t sampleMs = 0;

  explicit LoadChannel(float factor) : countsPerGram(factor) {}
  void begin(uint8_t data, uint8_t clock) {
    scale.begin(data, clock, true, false);
    scale.power_down();
    scale.power_up();
  }
  void clearAverage() {
    sampleCount = nextSample = 0;
    sampleTotal = 0;
    for (uint8_t i = 0; i < 3; i++) samples[i] = 0;
  }
  void startTare(uint32_t now) {
    tared = false;
    taring = true;
    tareTotal = 0;
    tareCount = 0;
    tareSampleMs = now;
    clearAverage();
  }
  void step(uint32_t now) {
    if (taring && static_cast<uint32_t>(now - tareSampleMs) >= 1000) {
      taring = false;
      tared = false;
    }
    if (!scale.is_ready()) return;
    float raw = scale.read();
    if (taring) {
      tareTotal += raw;
      tareSampleMs = now;
      if (++tareCount == 5) {
        offset = tareTotal / 5;
        taring = false;
        tared = true;
        clearAverage();
      }
      return;
    }
    if (!tared) return;
    if (sampleCount > 0 && static_cast<uint32_t>(now - sampleMs) >= 750) clearAverage();
    float force = (raw - offset) / countsPerGram * 0.00980665f;
    sampleTotal += force - samples[nextSample];
    samples[nextSample] = force;
    nextSample = (nextSample + 1) % 3;
    if (sampleCount < 3) sampleCount++;
    sampleMs = now;
  }
  bool valid(uint32_t now) const {
    return tared && sampleCount == 3 && static_cast<uint32_t>(now - sampleMs) < 750;
  }
  float force() const { return sampleTotal / 3; }
};
