@echo off
REM CMDReflector
wmic path Win32_SerialPort Where "Caption LIKE '%%Arduino%%'" Get DeviceID > cmdx.txt
for /f "delims=" %%i in ( 'type cmdx.txt') do SET  port=%%i
echo.capture_port_is_%port%
setlocal ENABLEDELAYEDEXPANSION
set arg1=%1
%1 > msg.txt
for /f "tokens=*" %%A in (msg.txt) do (
set /p x="%%A" <nul >\\.\%port%
echo.>\\.\%port%
)
@echo on

