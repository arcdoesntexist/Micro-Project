#ifndef MICRO_FSR_BENCH_H
#define MICRO_FSR_BENCH_H

#include "stm32g0xx_hal.h"

HAL_StatusTypeDef FsrBench_Init(ADC_HandleTypeDef *adc, UART_HandleTypeDef *uart);
void FsrBench_Task(void);

#endif
