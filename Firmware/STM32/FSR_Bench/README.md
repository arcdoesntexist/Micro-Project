# NUCLEO-G071RB FSR controller bring-up

Approved architecture: ESP32 controls P1 and NUCLEO-G071RB controls P2. Each board has two FSR sensors and its own USB serial connection to the computer. The host maps ports to players. The boards do not need UART wires between them.

## Pin assignments

| Board | Left FSR ADC | Right FSR ADC | Serial to computer |
| --- | --- | --- | --- |
| ESP32 | GPIO34, unchanged | GPIO35, unchanged | Existing USB connection, 115200 8N1 |
| NUCLEO-G071RB | PA0 / A0 / CN8 pin 1 / ADC1_IN0 | PA1 / A1 / CN8 pin 2 / ADC1_IN1 | USART2 PA2 TX + PA3 RX through ST-LINK Virtual COM, 115200 8N1 |

For the Nucleo, use its ST-LINK USB Micro-B connector CN2. PA2/PA3 are already routed to ST-LINK when the default SB16/SB18 bridges are ON. These are different from Arduino D0/D1 (PC5/PC4). Keep SWD programming pins available. Do not connect the ESP32 TX/RX to the Nucleo for this architecture.

Official reference: ST UM2324 Rev 5, Table 8 and Table 12: https://www.st.com/resource/en/user_manual/dm00452640-stm32-nucleo-64-boards-with-stm32g07xrb-mcus-stmicroelectronics.pdf

## Two local divider circuits on STM32

For each FSR, wire:

```text
3.3 V -- 10 kOhm fixed resistor --+-- PA0 (left) or PA1 (right)
                                |
                               FSR
                                |
                               GND
```

Use one fixed resistor and one FSR per ADC channel. Both circuits use the Nucleo's 3.3 V/GND. With this orientation the reading is high when released and decreases when pressed, matching the ESP32 firmware's direction. ADC inputs must stay within 0–3.3 V. Keep the working ESP32 wiring unchanged.

## STM32CubeIDE project settings

1. Create a new STM32 project with Board Selector `NUCLEO-G071RB`, C, STM32CubeIDE. Keep SWD enabled.
2. For the initial bench project use HSI16, SYSCLK/HCLK/PCLK = 16 MHz. No external crystal is required. This module selects an ADC clock of PCLK/4 = 4 MHz.
3. Enable ADC1 IN0 so CubeMX adds the ADC HAL drivers and `hadc1`. The bench module owns ADC1: it explicitly configures PA0 and PA1 as analog inputs, uses one configurable sequencer rank, and selects each channel before reading. No DMA, ADC interrupt, continuous conversion or other ADC consumer is used for this bench step.
4. Enable USART2 Asynchronous on PA2/PA3, 115200 baud, 8 data bits, no parity, one stop bit, no flow control. CubeMX generates `huart2`.
5. Generate code. Copy `fsr_bench.h` into `Core/Inc` and `fsr_bench.c` into `Core/Src`.
6. Add the include inside `main.c` USER CODE Includes:

```c
#include "fsr_bench.h"
```

After `MX_GPIO_Init()`, `MX_ADC1_Init()` and `MX_USART2_UART_Init()`, inside USER CODE 2:

```c
if (FsrBench_Init(&hadc1, &huart2) != HAL_OK)
{
    Error_Handler();
}
```

Inside the generated `while (1)` USER CODE 3 block:

```c
FsrBench_Task();
```

The module configures ADC1 explicitly, performs its internal ADC calibration and prints raw left/right readings about ten times per second. Internal ADC calibration is not FSR pressure calibration. Integer formatting avoids requiring newlib floating-point printf support. ADC waits and UART transmission have bounded timeouts. Do not add an independent ADC sampling task to this bench project.

## Read the new sensors

Flash the project yourself after checking wiring. Open the ST-LINK COM port in a serial terminal at 115200 8N1:

```text
FSR raw L=4090 R=4087
FSR raw L=1300 R=4086
```

Numbers above are examples, not calibration constants. Capture several seconds with both sensors released, then left pressed at a comfortable repeatable reference while right is released, then right pressed while left is released. Send the output log so each STM32 channel can have its own released/reference endpoints. ESP32's ADC endpoints are not copied to the new sensors.

This is an ADC/serial bring-up module, not a complete generated CubeIDE project or the final Unity JSON controller. It has not been flashed or tested on a physical board. The existing combined ESP32 firmware is retained, including GPIO34/35 and its established FSR constants. After collecting STM32 endpoints, the next integration step is matching JSON telemetry and two independent USB inputs in Unity, with both sides pausing if a required stream becomes stale. Close serial terminals before the game opens their ports.
