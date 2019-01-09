# TechBrain

Project for SmartHome (remote socket switcher, home sensors, etc.)

## Supporting devices

- ESP8266 like uart-to-tcp and socket switcher
- AVR (Attiny13 and Atmega8) like socket switcher by UART responses

## Applications

### windowsApp

- DevEnvironment: VisualStudio, C#
- Purpose: Single server for connection and controlling remote devices

### ESP8266

- DevEnvironment: ArduinoIDE, VSCode, C++
- Purpose: Device for providing tcp-uart connection and outputs switching

### AVR

- DevEnvironment: AtmelStudio, C
- Purpose: Device for outputs switching, sensors monitoring