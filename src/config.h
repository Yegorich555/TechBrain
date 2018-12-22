#ifndef CONFIG_H_
#define CONFIG_H_

#include <stdint.h>
#include <string.h>

#define IO_OUT1 16    //todo redefine
#define IO_OUT2 14    //todo redefine
#define LED_BUILTIN 2 //by default and also Tx1 by default

extern unsigned long UART_BAUD; //stored in eeprom
extern bool DEBUG_EN;             //stored into eeprom

#define e_StrLen 30                          //max-length 30byte
#define e_SSID_Addr 0                        //eeprom address for WIFI_SSID_1
#define e_PASS_Addr (e_SSID_Addr + e_StrLen) //eeprom address for WIFI_PASS_1
#define e_DBG_Addr (e_PASS_Addr + e_StrLen)  //eeprom address for Debug (ON/OFF)
#define e_SN_Addr (e_DBG_Addr + 1)           //eeprom address for Serial Number (max 2 bytes)
#define e_SRVPORT_Addr (e_SN_Addr + 2)       //eeprom address for Server_PORT (2 bytes)
#define e_SRVIPL_Addr (e_SRVPORT_Addr + 2)   //eeprom address for SERVER_IP_LAST
#define e_BAUD_Addr (e_SRVIPL_Addr + 1)      //eeprom address for UART baudRate

extern String WIFI_SSID_1; //stored into eeprom
extern String WIFI_PASS_1; //stored into eeprom

#define WIFI_SSID_2 "ESPCfg" // the second wifi point if the first doesn't exist
#define WIFI_PASS_2 "atmel^8266"

#define MAX_SRV_CLIENTS 2

const uint8_t WIFI_SERVER_PORT = 80;
extern uint16_t SERVER_PORT; //stored into eeprom
extern uint8_t SERVER_IP_LAST;  //stored into eeprom //mask server IP = x.x.x.SERVER_IP_LAST
extern uint8_t MY_SN;           //stored into eeprom - SerialNumber

#endif /*CONFIG_H_*/