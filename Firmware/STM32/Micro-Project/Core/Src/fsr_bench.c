#include "fsr_bench.h"
#include <stdio.h>

static ADC_HandleTypeDef *input_adc;
static UART_HandleTypeDef *output_uart;
static uint32_t last_report_ms;

HAL_StatusTypeDef FsrBench_Init(ADC_HandleTypeDef *adc, UART_HandleTypeDef *uart)
{
    if (adc == NULL || uart == NULL || adc->Instance != ADC1 || uart->Instance != USART2)
        return HAL_ERROR;

    __HAL_RCC_GPIOA_CLK_ENABLE();
    __HAL_RCC_ADC_CLK_ENABLE();
    GPIO_InitTypeDef gpio = {0};
    gpio.Pin = GPIO_PIN_0 | GPIO_PIN_1;
    gpio.Mode = GPIO_MODE_ANALOG;
    gpio.Pull = GPIO_NOPULL;
    HAL_GPIO_Init(GPIOA, &gpio);

    adc->Init.ClockPrescaler = ADC_CLOCK_SYNC_PCLK_DIV4;
    adc->Init.Resolution = ADC_RESOLUTION_12B;
    adc->Init.DataAlign = ADC_DATAALIGN_RIGHT;
    adc->Init.ScanConvMode = ADC_SCAN_DISABLE;
    adc->Init.EOCSelection = ADC_EOC_SINGLE_CONV;
    adc->Init.LowPowerAutoWait = DISABLE;
    adc->Init.LowPowerAutoPowerOff = DISABLE;
    adc->Init.ContinuousConvMode = DISABLE;
    adc->Init.NbrOfConversion = 1;
    adc->Init.DiscontinuousConvMode = DISABLE;
    adc->Init.ExternalTrigConv = ADC_SOFTWARE_START;
    adc->Init.ExternalTrigConvEdge = ADC_EXTERNALTRIGCONVEDGE_NONE;
    adc->Init.DMAContinuousRequests = DISABLE;
    adc->Init.Overrun = ADC_OVR_DATA_OVERWRITTEN;
    adc->Init.SamplingTimeCommon1 = ADC_SAMPLETIME_160CYCLES_5;
    adc->Init.SamplingTimeCommon2 = ADC_SAMPLETIME_160CYCLES_5;
    adc->Init.OversamplingMode = DISABLE;
    adc->Init.TriggerFrequencyMode = ADC_TRIGGER_FREQ_HIGH;
    if (HAL_ADC_Init(adc) != HAL_OK || HAL_ADCEx_Calibration_Start(adc) != HAL_OK)
        return HAL_ERROR;

    input_adc = adc;
    output_uart = uart;
    last_report_ms = HAL_GetTick();
    return HAL_OK;
}

static HAL_StatusTypeDef read_channel(uint32_t channel, uint32_t *value)
{
    ADC_ChannelConfTypeDef config = {0};
    config.Channel = channel;
    config.Rank = ADC_REGULAR_RANK_1;
    config.SamplingTime = ADC_SAMPLINGTIME_COMMON_1;
    if (HAL_ADC_ConfigChannel(input_adc, &config) != HAL_OK || HAL_ADC_Start(input_adc) != HAL_OK)
        return HAL_ERROR;
    HAL_StatusTypeDef status = HAL_ADC_PollForConversion(input_adc, 3);
    if (status == HAL_OK) *value = HAL_ADC_GetValue(input_adc);
    HAL_StatusTypeDef stopped = HAL_ADC_Stop(input_adc);
    return status == HAL_OK ? stopped : status;
}

void FsrBench_Task(void)
{
    if (input_adc == NULL || output_uart == NULL) return;
    uint32_t now = HAL_GetTick();
    if ((uint32_t)(now - last_report_ms) < 100) return;
    last_report_ms = now;

    uint32_t left = 0;
    uint32_t right = 0;
    char line[64];
    int length;
    if (read_channel(ADC_CHANNEL_0, &left) == HAL_OK && read_channel(ADC_CHANNEL_1, &right) == HAL_OK)
        length = snprintf(line, sizeof(line), "FSR raw L=%lu R=%lu\r\n", (unsigned long)left, (unsigned long)right);
    else
        length = snprintf(line, sizeof(line), "FSR ADC_ERROR\r\n");
    if (length > 0 && (size_t)length < sizeof(line))
        (void)HAL_UART_Transmit(output_uart, (uint8_t *)line, (uint16_t)length, 20);
}
