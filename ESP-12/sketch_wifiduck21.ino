#include <Keyboard.h>
#include <SoftwareSerial.h>
//  *********************************************
//  **       WIFI-DUCKY PROTOTYPE   v1.22      **
//  *********************************************
//  **    basic4@privatoria.net   Jun. 2016    **
//  *********************************************
SoftwareSerial ESP(14,15); 
// creates a virtual serial port on 10/11
// connect ESP-01 module TX to D10
// connect ESP-01 module RX to D11
// connect Vcc to 5V, GND to GND (via 5/3.3v conf)
int inpx = 0;  // char from Esp8266
int multi = 0; // command type
int kill = 0;  // kill the operation
String fversion="WifiDuck v1.22";

void setup() { 
  //enable the wifi chip
  ESP.begin(19200);
  pinMode(13, OUTPUT);
  digitalWrite(13,HIGH);
  Serial.begin(19200);
  Keyboard.begin();
  delay(3000);

}

void loop() 
{
      if(ESP.available()> 0)
      {         
         byte inpx = ESP.read();
         //Serial.println(inpx);
         switch(inpx)
         {
             case 250:
                 sendData(fversion + "\r\n");
                 delay(1000);  
                 multi = 0;
                 break;
            case 251:
                //Start of a multi-key command (eg. CTRL + ALT + DEL)
                multi = 1;
                break;
            case 252:
                 //The KEY command on (windows 'ALT' keycodes)
                 multi = 0;
                 Keyboard.press(130);
                 delay(50);
                 break;
            case 253:
                 //Switches KEY command off
                 multi = 0;
                 Keyboard.release(130);
                  delay(10);
                  break;
            case 254:
                 //End of a multi-key command
                 multi = 0;
                 Keyboard.releaseAll();
                 delay(5);
                 break;
             case 255:
                 //we are signaled to end operations
                 multi = 0;
                 kill = 1;    
                 break;
             default:
                  if(multi == 0)
                  { 
                     //Normal Single character code
                     Keyboard.write(inpx);
                     delay(20);
                  }
                  else
                  {
                     //Part of a multi-key command 
                     Keyboard.press(inpx);
                     delay(30);
                  }
                  break;
         }

      }
      //Check for output from the attacker
      //via the open serial open connection.
      //if so write this back via wifi
      //to attacker device.
      if(Serial.available() > 0)
      {
          //this is using stream so we should be ok...
          sendData(Serial.readString());
      }
} 

void sendData(String inx)
{
   int lx = inx.length();
   if(lx > 0)
   {
      ESP.print(inx);  
   } 
}


