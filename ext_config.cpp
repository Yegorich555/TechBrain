#include <Arduino.h>
#include "ext_config.h"

String WIFI_SSID_1 = "";
String WIFI_PASS_1 = "";

unsigned long UART_BAUD = 115200;
bool DEBUG_EN = true;             

uint16_t SERVER_PORT = 1234;
uint8_t SERVER_IP_LAST = 101; //mask server IP = x.x.x.SERVER_IP_LAST
uint8_t MY_SN = 0;