#ifndef CMD_H_
#define CMD_H_

#include <Arduino.h>
#include <stdint.h>
#include <string.h>

const String strCmd_Start = "esp_";

// struct structCmd
// {
//     String strCommand;
//     bool (*execute)(Stream &stream, String strValue);
// };

class CmdClass
{
  public:
    bool execute(String str);
    void readFromEEPROM();
};

extern CmdClass Cmd;

// extern const structCmd cmd[];
// // typedef enum cmd_e
// // {
// //     cmd_out1 = 0, //change output 1
// //     cmd_out2,     //change output 2
// //     cmd_outAll,   //change all outputs
// //     cmd_ssid,     //set WiFi ssid
// //     cmd_pass,     //set WiFi password
// //     cmd_rst,      //reset chip
// //     cmd_dbg,      //turn debug on/off
// //     cmd_sn,       //set serial number
// //     cmd_port,     //set serverPort,
// //     cmd_ipl,      //last number of server's IP address
// //     cmd_baud,     //baudRate for UART

// //     cmd_ping, //ping - only for testing esp-connection
// //     cmd_state //info about current state
// // } cmd_e;

// // const String strCmd_Start = "esp_"; //Pattern: esp_out1(0)
// // const structCmd cmd[] = {
// //     //str only in lowerCase!!!
// //     {"out1", cmd_out1},     //from out1(0) to out1(100)
// //     {"out2", cmd_out2},     //from out2(0) to out2(100)
// //     {"outall", cmd_outAll}, //for all outs
// //     {"ssid", cmd_ssid},     //esp_pass(ssidName)
// //     {"pass", cmd_pass},     //esp_pass(password)
// //     {"rst", cmd_rst},       //esp_rst()
// //     {"dbg", cmd_dbg},       //esp_dbg(0) - esp_dbg(1)
// //     {"sn", cmd_sn},         //esp_sn(serialNumber)
// //     {"port", cmd_port},     //esp_port(portNumber)
// //     {"ipl", cmd_ipl},       //esp_ipl(ipLastNumber)
// //     {"baud", cmd_baud},     //cmd_baud(serialBaudRate)

// //     {"ping", cmd_ping},   //cmd_ping()
// //     {"state", cmd_state}, //cmd_state()
// // };

#endif /* CMD_H_ */