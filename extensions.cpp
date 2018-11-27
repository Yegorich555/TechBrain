#include <Arduino.h>
#include "extensions.h"

byte EEPROM_EXTClass::readByte(const int address)
{
  return read(address);
}

int EEPROM_EXTClass::readInt(const int address)
{
  byte lowByte = read(address);
  byte highByte = read(address + 1);

  return ((lowByte << 0) & 0xFF) + ((highByte << 8) & 0xFF00);
}

String EEPROM_EXTClass::readStr(int startAddress, const uint8_t length)
{
  String str = "";
  for (uint8_t i = 0; i < length; ++i)
  {
    char v = read(startAddress++);
    if (v == 0 || v == 255)
      break;

    str += String(v);
  }
  return str;
}

bool EEPROM_EXTClass::write(const int address, uint8_t value)
{
  baseWrite(address, value);
  commit();
  return true;
}

bool EEPROM_EXTClass::write(const int address, uint16_t value)
{
  byte lowByte = ((value >> 0) & 0xFF);
  byte highByte = ((value >> 8) & 0xFF);

  baseWrite(address, lowByte);
  baseWrite(address + 1, highByte);
  commit();
  return true;
}

bool EEPROM_EXTClass::write(int startAddress, String str, const uint8_t maxlength)
{
  unsigned int len = str.length();
  if (len > maxlength)
  {
    String result = "Err writeEEprom: maxLength " + len + String(" > ") + maxlength; //todo set to debug
    return false;
  }
  for (unsigned int i = 0; i < len; ++i)
    baseWrite(startAddress++, str[i]);

  if (len < maxlength)
    baseWrite(startAddress, 0);

  commit();
  return true;
}

EEPROM_EXTClass EEPROM_EXT;