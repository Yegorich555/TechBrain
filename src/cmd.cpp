#include <Arduino.h>
#include "cmd.h"
#include "config.h"
#include "extensions.h"

#define E_PUT_COMMIT(addr, v) \
    EEPROM_EXT.put(addr, v);  \
    EEPROM_EXT.commit();

#define E_PUT(s, v) E_PUT_COMMIT(offsetof(struct struct_cfgEEPROM, v), s.v)

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

#define E_PUT_STR(s, v, str)                                                                                            \
    size_t len = str.length() + 1;                                                                                      \
    if (len > sizeof(struct_cfgEEPROM::v))                                                                              \
    {                                                                                                                   \
        /*//todo String result = "Err writeEEprom: maxLength " + len + String(" > ") + maxlength; //todo set to debug*/ \
        return false;                                                                                                   \
    }                                                                                                                   \
    str.toCharArray(s.v, len);                                                                                          \
    E_PUT(s, v);                                                                                                        \
    return true;

bool setWiFiSSID(Stream &stream __attribute__((unused)), String strValue)
{
    E_PUT_STR(cfgEEPROM, WIFI_SSID_1, strValue);
}

bool setWiFiPassword(Stream &stream __attribute__((unused)), String strValue)
{
    E_PUT_STR(cfgEEPROM, WIFI_PASS_1, strValue);
}

bool getPing(Stream &stream __attribute__((unused)), String strValue __attribute__((unused)))
{
    return true;
}

bool getState(Stream &stream, String strValue __attribute__((unused)))
{
    stream.print("State: SN=");
    stream.print(cfgEEPROM.MY_SN);
    stream.print(",baud=");
    stream.print(cfgEEPROM.UART_BAUD);
    stream.print(",dbg=");
    stream.print(cfgEEPROM.DEBUG_EN);
    stream.print(",lsleep=");
    stream.print(cfgEEPROM.LSLEEP_EN);

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
    cfgEEPROM.DEBUG_EN = (uint8_t)strValue.toInt() != 0;
    E_PUT(cfgEEPROM, DEBUG_EN);
    return true;
}

bool setSerialNumber(Stream &stream __attribute__((unused)), String strValue)
{
    cfgEEPROM.MY_SN = (uint8_t)strValue.toInt();
    E_PUT(cfgEEPROM, MY_SN);
    return true;
}

bool setServerPort(Stream &stream __attribute__((unused)), String strValue)
{
    cfgEEPROM.SERVER_PORT = (uint16_t)strValue.toInt();
    E_PUT(cfgEEPROM, SERVER_PORT);
    return true;
}

bool setIPLast(Stream &stream __attribute__((unused)), String strValue)
{
    cfgEEPROM.SERVER_IP_LAST = (uint8_t)strValue.toInt();
    E_PUT(cfgEEPROM, SERVER_IP_LAST);
    return true;
}

bool setSerialBaudRate(Stream &stream __attribute__((unused)), String strValue)
{
    uint32_t v = (uint32_t)strValue.toInt();
    if (BaudRate::isValid(v))
    {
        E_PUT_COMMIT(offsetof(struct struct_cfgEEPROM, UART_BAUD), v); //only after restart will be applied
        return true;
    }
    else
        return false;
}

bool setSleep(Stream &stream __attribute__((unused)), String strValue)
{
    uint16_t v = (uint16_t)strValue.toInt();
    uint64_t maxDeepSleep = ESP.deepSleepMax() / 1000 / 1000;
    if (maxDeepSleep >= v)
        ESP.deepSleep(v * 1000 * 1000);
    else
    {
        stream.print("SleepTime > max");
        stream.print(Print64(stream, maxDeepSleep));
        return false;
    }
    return true;
}

#define switchLightSleep() Cmd._wifi.setSleepMode(cfgEEPROM.LSLEEP_EN ? WIFI_MODEM_SLEEP : WIFI_NONE_SLEEP)
bool setLightSleep(Stream &stream __attribute__((unused)), String strValue)
{
    cfgEEPROM.LSLEEP_EN = (uint8_t)strValue.toInt() != 0;
    E_PUT(cfgEEPROM, LSLEEP_EN);
    switchLightSleep();
    return true;
}

const structCmd cmd[] = {
    {"out1", updatePort1},   //esp_out1(v) - change output 1 from v=0 to 100
    {"out2", updatePort2},   //esp_out2(v) - change output 2
    {"outall", updatePorts}, ///esp_out(v) - change all outputs

    {"ssid", setWiFiSSID},       //esp_ssid(ssidName) - set WiFi ssid
    {"pass", setWiFiPassword},   //esp_pass(password) - set WiFi password
    {"rst", goReset},            //esp_rst() - restart Chip
    {"dbg", setDebug},           //esp_dbg(1/0) - enable/disable debug
    {"sn", setSerialNumber},     //esp_sn(serialNumber) - set serial number for Chip
    {"port", setServerPort},     //esp_port(portNumber) - set serverPort for Chip
    {"ipl", setIPLast},          //esp_ipl(ipLastNumber) - set last number of server's IP address
    {"baud", setSerialBaudRate}, //esp_baud(serialBaudRate) - baudRate for UART

    {"sleep", setSleep},       //esp_sleep(seconds) - deepSleep
    {"lsleep", setLightSleep}, //esp_lsleep(1/0) - enable/disable lightSleep => WIFI_MODEM_SLEEP
    {"ping", getPing},         //esp_ping() - only for testing esp-connection
    {"state", getState},       //esp_state() - get current info
};

void CmdClass::readFromEEPROM()
{
    if (EEPROM_EXT.read(0) == cfgEEPROM.version)
    {
        EEPROM_EXT.get(0, cfgEEPROM);
        if (!BaudRate::isValid(cfgEEPROM.UART_BAUD))
            cfgEEPROM.UART_BAUD = UART_BAUD_DEFAULT;
    }
    else
        E_PUT_COMMIT(0, cfgEEPROM);

    switchLightSleep();
    // UART_BAUD = BaudRate::fromNum(EEPROM_get(e_BAUD_Addr, UART_BAUD), UART_BAUD);
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

void CmdClass::begin(ESP8266WiFiClass &wifi)
{
    _wifi = wifi;
}

CmdClass Cmd;