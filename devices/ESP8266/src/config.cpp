#include <Arduino.h>
#include "config.h"

struct_cfgEEPROM cfgEEPROM = {
    .version = 1,
    .DEBUG_EN = true,
    .UART_BAUD = UART_BAUD_DEFAULT,
    .MY_SN = 0,
    .SERVER_PORT = 1234,
    .SERVER_IP_LAST = 101,
    .WIFI_SSID_1 = {},
    .WIFI_PASS_1 = {},
    .LSLEEP_EN = true,
};