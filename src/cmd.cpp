#include <Arduino.h>
#include "cmd.h"
#include "config.h"
#include "extensions.h"

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

bool setWiFiSSID(Stream &stream, String strValue)
{
    if (EEPROM_EXT.write(e_SSID_Addr, strValue, e_StrLen))
    {
        WIFI_SSID_1 = strValue;
        return true;
    }
    return false;
}

bool setWiFiPassword(Stream &stream, String strValue)
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
    uint8_t v = (uint8_t)strValue.toInt();
    EEPROM_EXT.write(e_DBG_Addr, v); //todo bool
    DEBUG_EN = v != 0;
    return true;
}

bool setSerialNumber(Stream &stream __attribute__((unused)), String strValue)
{
    uint8_t v = (uint8_t)strValue.toInt();
    EEPROM_EXT.write(e_SN_Addr, v);
    MY_SN = v;
    return true;
}

bool setServerPort(Stream &stream __attribute__((unused)), String strValue)
{
    uint16_t v = (uint16_t)strValue.toInt();
    EEPROM_EXT.write(e_SRVPORT_Addr, v);
    SERVER_PORT = v;
    return true;
}

bool setIPLast(Stream &stream __attribute__((unused)), String strValue)
{
    uint8_t v = (uint8_t)strValue.toInt();
    EEPROM_EXT.write(e_SRVIPL_Addr, v);
    SERVER_IP_LAST = v;
    return true;
}

bool setSerialBaudRate(Stream &stream __attribute__((unused)), String strValue)
{
    uint32_t v32 = (uint32_t)strValue.toInt();
    if (BaudRate::isValid(v32))
    {
        EEPROM_EXT.write(e_BAUD_Addr, BaudRate::toNum(v32));
        return true;
    }
    else
        return false;
}

const structCmd cmd[] = {
    {"out1", updatePort1},       //esp_out1(v) - change output 1 from v=0 to 100
    {"out2", updatePort2},       //esp_out2(v)  - change output 1
    {"outall", updatePorts},     ///esp_out(v) - change all outputs
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
    DEBUG_EN = EEPROM_EXT.readByte(e_DBG_Addr) != 0;
    UART_BAUD = BaudRate::fromNum(EEPROM_EXT.read(e_BAUD_Addr), UART_BAUD);
    WIFI_SSID_1 = EEPROM_EXT.readStr(e_SSID_Addr, e_StrLen);
    WIFI_PASS_1 = EEPROM_EXT.readStr(e_PASS_Addr, e_StrLen);
    MY_SN = EEPROM_EXT.readByte(e_SN_Addr);
    SERVER_PORT = EEPROM_EXT.readInt(e_SRVPORT_Addr);
    SERVER_IP_LAST = EEPROM_EXT.readByte(e_SRVIPL_Addr);
}

bool CmdClass::execute(Stream &stream, String str) //esp_cmd(strVal) pattern
{
    int startIndex = str.indexOf('(');
    int endIndex = str.indexOf(")", startIndex + 1);

    if (startIndex == -1 || endIndex == -1)
        return true;
    String cmdStr = str.substring(0, startIndex);
    cmdStr.toLowerCase();

    for (uint8_t i = 0; i < sizeof(cmd); ++i)
    {
        if (cmdStr == cmd[i].strCommand)
            return !cmd[i].execute(stream, str.substring(startIndex + 1, endIndex));
    }
    return false;
}

CmdClass Cmd;