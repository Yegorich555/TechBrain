#include <Arduino.h>
#include <EEPROM.h>

#include "extensions.h"

byte EEPROM_EXTClass::readByte(int address)
{
  return EEPROM.read(address);
}

int EEPROM_EXTClass::readInt(int address)
{
  byte lowByte = EEPROM.read(address);
  byte highByte = EEPROM.read(address + 1);

  return ((lowByte << 0) & 0xFF) + ((highByte << 8) & 0xFF00);
}

String EEPROM_EXTClass::readStr(int startAddress, uint8_t length)
{
  String str = "";
  for (uint8_t i = 0; i < length; ++i)
  {
    char v = EEPROM.read(startAddress++);
    if (v == 0 || v == 255)
      break;

    str += String(v);
  }
  return str;
}

bool EEPROM_EXTClass::write(int address, uint8_t value)
{
  EEPROM.write(address, value);
  EEPROM.commit();
  return true;
}

bool EEPROM_EXTClass::write(int address, uint16_t value)
{
  byte lowByte = ((value >> 0) & 0xFF);
  byte highByte = ((value >> 8) & 0xFF);

  EEPROM.write(address, lowByte);
  EEPROM.write(address + 1, highByte);
  EEPROM.commit();
  return true;
}

bool EEPROM_EXTClass::write(int startAddress, String str, uint8_t maxlength)
{
  unsigned int len = str.length();
  if (len > maxlength)
  {
    String result = "Err writeEEprom: maxLength " + len + String(" > ") + maxlength; //todo set to debug
    return false;
  }
  for (unsigned int i = 0; i < len; ++i)
    EEPROM.write(startAddress++, str[i]);

  if (len < maxlength)
    EEPROM.write(startAddress, 0);

  EEPROM.commit();
  return true;
}

EEPROM_EXTClass EEPROM_EXT;