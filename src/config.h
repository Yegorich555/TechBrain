#ifndef CONFIG_H_
#define CONFIG_H_

#include <stdint.h>
#include <string.h>

#define IO_OUT1 16    //todo redefine
#define IO_OUT2 14    //todo redefine
#define LED_BUILTIN 2 //by default and also Tx1 by default
#define UART_BAUD_DEFAULT 115200

typedef struct struct_cfgEEPROM //stored into eeprom
{
    uint8_t version; //eeprom struct version
    bool DEBUG_EN;
    unsigned long UART_BAUD;
    uint8_t MY_SN; // SerialNumber
    uint16_t SERVER_PORT;
    uint8_t SERVER_IP_LAST; //mask server IP = x.x.x.SERVER_IP_LAST
    char WIFI_SSID_1[30];
    char WIFI_PASS_1[30];
} struct_cfgEEPROM;

extern struct_cfgEEPROM cfgEEPROM;

#define WIFI_SERVER_PORT 80

#define WIFI_SSID_2 "ESPCfg" // the second wifi point if the first doesn't exist
#define WIFI_PASS_2 "atmel^8266"

#define MAX_SRV_CLIENTS 2

#endif /*CONFIG_H_*/