# STM32 Firmware

Target: NUCLEO-G071RB, STM32CubeIDE. This board is the P2 controller and reads two FSR sensors; ESP32 remains P1 on its existing GPIO34/GPIO35. Both boards connect to the computer by separate USB serial ports.

The first bring-up module is [FSR_Bench](FSR_Bench/README.md): PA0/A0 and PA1/A1 ADC inputs, USART2 through ST-LINK Virtual COM at 115200 8N1. It includes source files to add to a generated CubeIDE project and instructions for collecting raw calibration readings.

The bench module is not the final Unity telemetry firmware. No physical board has been flashed or tested by this change. Once actual sensor reference values are collected, final streaming will follow the shared [sensor protocol](../../sensor-protocol/README.md) and Unity's planned two-port player routing.
