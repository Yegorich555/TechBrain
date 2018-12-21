#include "ext_cmd.h"
#include "ext_config.h"
#include "extensions.h"

uint8_t outStates[2];

void updatePort(const uint8_t arrNum, const uint8_t num, String strValue)
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
}

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

bool CmdClass::execute(String str)
{
    return true;
}

CmdClass Cmd;
// const structCmd cmd[] = {
//     {"out1", [](Stream &stream __attribute__((unused)), String strValue) { return updatePort(1, IO_OUT1, strValue); }},
//     {"out2", [](Stream &stream __attribute__((unused)), String strValue) { return updatePort(2, IO_OUT2, strValue); }},
//     {"outall", [](Stream &stream, String strValue) { return cmd[0].execute(stream, strValue) && cmd[1].execute(stream, strValue); }},
// };

// int startIndex = str.indexOf('(');
// int endIndex = str.indexOf(")", startIndex + 1);
// bool isError = false;
// if (startIndex == -1 || endIndex == -1)
//     isError = true;
// else
// {
// String cmdStr = str.substring(0, startIndex);
// cmdStr.toLowerCase();
// uint8_t i;
// for (i = 0; i < sizeof(cmd); ++i)
// {
//   if (cmdStr == cmd[i].strCommand)
//   {
//     isError = !cmd[i].execute(stream, str.substring(startIndex + 1, endIndex));
//     break;
//   }
// }

// if (cmd[i].type == cmd_rst)
//   ESP.restart();
// else if (cmd[i].type == cmd_ping)
//   ;
// else if (cmd[i].type == cmd_state)
// {
//   stream.print("State: SN=");
//   stream.print(MY_SN);
//   stream.print(",baud=");
//   stream.print(UART_BAUD);
//   stream.print(",dbg=");
//   stream.print(DEBUG_EN);

//   stream.print(",out1=");
//   stream.print(outStates[0]);
//   stream.print(",out2=");
//   stream.println(outStates[1]);
// }
// else
// {
//   str = str.substring(startIndex + 1, endIndex);
//   uint8_t v = 0;
//   uint16_t v16 = 0;

//   switch (cmd[i].type)
//   {
//   case cmd_ssid:
//     if (EEPROM_EXT.write(e_SSID_Addr, str, e_StrLen))
//       WIFI_SSID_1 = str;
//     break;
//   case cmd_pass:
//     if (EEPROM_EXT.write(e_PASS_Addr, str, e_StrLen))
//       WIFI_PASS_1 = str;
//     break;

//   case cmd_out1:
//     v = (uint8_t)str.toInt();
//     updatePort(1, IO_OUT1, v);
//     break;
//   case cmd_out2:
//     v = (uint8_t)str.toInt();
//     updatePort(2, IO_OUT2, v);
//     break;
//   case cmd_outAll:
//     v = (uint8_t)str.toInt();
//     updatePort(1, IO_OUT1, v);
//     updatePort(2, IO_OUT2, v);
//     break;

//   case cmd_dbg:
//     v = (uint8_t)str.toInt();
//     EEPROM_EXT.write(e_DBG_Addr, v);
//     DEBUG_EN = v != 0;
//     break;
//   case cmd_sn:
//     v = (uint8_t)str.toInt();
//     EEPROM_EXT.write(e_SN_Addr, v);
//     MY_SN = v;
//     break;
//   case cmd_port:
//     v16 = (uint16_t)str.toInt();
//     EEPROM_EXT.write(e_SRVPORT_Addr, v16);
//     SERVER_PORT = v16;
//     break;
//   case cmd_ipl:
//     v = (uint8_t)str.toInt();
//     EEPROM_EXT.write(e_SRVIPL_Addr, v);
//     SERVER_IP_LAST = v;
//     break;
//   case cmd_baud:
//   {
//     uint32_t v32 = (uint32_t)str.toInt();
//     if (BaudRate::isValid(v32))
//       EEPROM_EXT.write(e_BAUD_Addr, BaudRate::toNum(v32));
//     else
//       isError = true;
//   }
//   break;

//   default:
//     isError = true;
//     break;
//   }
//}
// }