#include <Arduino.h>
#include "cmd.h"
#include "config.h"
#include "extensions.h"

template <typename T>
const T &EEPROM_put(int const address, const T &t)
{
    EEPROM_EXT.put(address, t);
    EEPROM_EXT.commit();
    return t;
}

template <typename T>
T &EEPROM_get(int const address, T &t)
{
    return EEPROM_EXT.get(address, t);
}

uint8_t outStates[2];
bool updatePort(const uint8_t arrNum, const uint8_t num, String strValue)
{
    uint8_t v = (uint8_t)strValue.toInt();
    //todo check value
    outStates[arrNum - 1] = v;
    if (v == 100)
        digitalWrite(num, HIGH);
    else if (v == 0)
        digitalWrite(num, LOW);
    else
        analogWrite(num, 1023 * v / 100);

    return true;
}

bool updatePort1(Stream &stream __attribute__((unused)), String strValue)
{
    return updatePort(1, IO_OUT1, strValue);
}
bool updatePort2(Stream &stream __attribute__((unused)), String strValue)
{
    return updatePort(2, IO_OUT2, strValue);
}
bool updatePorts(Stream &stream __attribute__((unused)), String strValue)
{
    return updatePort1(stream, strValue) &&
           updatePort2(stream, strValue);
}

bool setWiFiSSID(Stream &stream __attribute__((unused)), String strValue)
{
    if (EEPROM_EXT.write(e_SSID_Addr, strValue, e_StrLen))
    {
        WIFI_SSID_1 = strValue;
        return true;
    }
    return false;
}

bool setWiFiPassword(Stream &stream __attribute__((unused)), String strValue)
{
    if (EEPROM_EXT.write(e_PASS_Addr, strValue, e_StrLen))
    {
        WIFI_PASS_1 = strValue;
        return true;
    }
    return false;
}

bool getPing(Stream &stream __attribute__((unused)), String strValue __attribute__((unused)))
{
    return true;
}

bool getState(Stream &stream, String strValue __attribute__((unused)))
{
    stream.print("State: SN=");
    stream.print(MY_SN);
    stream.print(",baud=");
    stream.print(UART_BAUD);
    stream.print(",dbg=");
    stream.print(DEBUG_EN);

    stream.print(",out1=");
    stream.print(outStates[0]);
    stream.print(",out2=");
    stream.println(outStates[1]);
    return true;
}

bool goReset(Stream &stream __attribute__((unused)), String strValue __attribute__((unused)))
{
    ESP.restart();
    return true;
}

bool setDebug(Stream &stream __attribute__((unused)), String strValue)
{
    DEBUG_EN = (uint8_t)strValue.toInt() != 0;
    EEPROM_put(e_DBG_Addr, DEBUG_EN);
    return true;
}

bool setSerialNumber(Stream &stream __attribute__((unused)), String strValue)
{
    MY_SN = (uint8_t)strValue.toInt();
    EEPROM_put(e_SN_Addr, MY_SN);
    return true;
}

bool setServerPort(Stream &stream __attribute__((unused)), String strValue)
{
    SERVER_PORT = (uint16_t)strValue.toInt();
    EEPROM_put(e_SRVPORT_Addr, SERVER_PORT);
    return true;
}

bool setIPLast(Stream &stream __attribute__((unused)), String strValue)
{
    SERVER_IP_LAST = (uint8_t)strValue.toInt();
    EEPROM_put(e_SRVIPL_Addr, SERVER_IP_LAST);
    return true;
}

bool setSerialBaudRate(Stream &stream __attribute__((unused)), String strValue)
{
    uint32_t v = (uint32_t)strValue.toInt();
    if (BaudRate::isValid(v))
    {
        EEPROM_put(e_BAUD_Addr, BaudRate::toNum(v));
        return true;
    }
    else
        return false;
}

const structCmd cmd[] = {
    {"out1", updatePort1},   //esp_out1(v) - change output 1 from v=0 to 100
    {"out2", updatePort2},   //esp_out2(v)  - change output 1
    {"outall", updatePorts}, ///esp_out(v) - change all outputs

    {"ssid", setWiFiSSID},       //esp_ssid(ssidName) - set WiFi ssid
    {"pass", setWiFiPassword},   //esp_pass(password) - set WiFi password
    {"rst", goReset},            //esp_rst() - restart Chip
    {"dbg", setDebug},           //esp_dbg(1)/esp_dbg(0) - enable/disable debug
    {"sn", setSerialNumber},     //esp_sn(serialNumber) - set serial number for Chip
    {"port", setServerPort},     //esp_port(portNumber) - set serverPort for Chip
    {"ipl", setIPLast},          //esp_ipl(ipLastNumber) - set last number of server's IP address
    {"baud", setSerialBaudRate}, //esp_baud(serialBaudRate) - baudRate for UART

    {"ping", getPing},   //esp_ping() - only for testing esp-connection
    {"state", getState}, //esp_state() - get current info
};

void CmdClass::readFromEEPROM()
{
    EEPROM_get(e_DBG_Addr, DEBUG_EN);
    UART_BAUD = BaudRate::fromNum(EEPROM_get(e_BAUD_Addr, UART_BAUD), UART_BAUD);
    WIFI_SSID_1 = EEPROM_EXT.readStr(e_SSID_Addr, e_StrLen);
    WIFI_PASS_1 = EEPROM_EXT.readStr(e_PASS_Addr, e_StrLen);
    EEPROM_get(e_SN_Addr, MY_SN);
    EEPROM_get(e_SRVPORT_Addr, SERVER_PORT);
    EEPROM_get(e_SRVIPL_Addr, SERVER_IP_LAST);
}

bool CmdClass::execute(Stream &stream, String str) //esp_cmd(strVal) pattern
{
    int startIndex = str.indexOf('(');
    int endIndex = str.indexOf(")", startIndex + 1);

    if (startIndex == -1 || endIndex == -1)
        return false;
    String cmdStr = str.substring(0, startIndex);
    cmdStr.toLowerCase();

    for (uint8_t i = 0; i < sizeof(cmd); ++i)
    {
        if (cmdStr == cmd[i].strCommand)
            return cmd[i].execute(stream, str.substring(startIndex + 1, endIndex));
    }
    return false;
}

CmdClass Cmd;