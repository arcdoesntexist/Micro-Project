# NUCLEO-G071RB CubeIDE project

Import this directory as an existing STM32CubeIDE project (tested with CubeIDE 1.15.1, STM32Cube G0 1.6.3). Build Debug and program the attached NUCLEO-G071RB through its ST-LINK USB connector.

- Left FSR: PA0 / A0. Right FSR: PA1 / A1.
- Each divider: 3.3 V -> 10 kOhm fixed resistor -> ADC input -> FSR -> GND.
- USART2 on PA2/PA3 routes to ST-LINK Virtual COM at 115200 8N1.
- HSI16 clock; ADC1 is configured by fsr_bench.c for sequential 12-bit reads.
- main.c invokes FsrBench_Init and FsrBench_Task inside preserved USER CODE sections.
- Output: FSR raw L=<value> R=<value>, approximately ten lines per second.

The firmware was built and programmed successfully on 2026-10-29. The Unity Windows Versus controller adapter accepts these raw lines directly. Unity currently ignores the broken left sensor and permits keyboard controls alongside the healthy right FSR. Confirm physical released/reference readings before treating percentage values as calibrated. No newton force is produced.

Build output, flash backups and machine-specific debug launch files are intentionally excluded from Git.
