#include <Keyboard.h>
//  *********************************************
//  **       WIFI-DUCKY PROTOTYPE   v1.44      **
//  *********************************************
//  **    basic4@privatoria.net   sept 2016    **
//  *********************************************
//cactus micro version
int inpx = 0;  // char from Esp8266
int multi = 0; // command type
int kill = 0;  // kill the operation
String fversion="WifiDuck v1.44";
int stableCount = 0;
int mode = 0; //serial to keyboard mode

void setup() { 
  //enable the wifi chip
  pinMode(13, OUTPUT);
  digitalWrite(13,HIGH);
  Serial.begin(19200);
  Serial1.begin(19200);
  Keyboard.begin();
  delay(8000);
  while (stableCount < 50)
  {
      if(Serial1.available() > 0)
      {
        byte garble = Serial1.read();
      }
      delay(20);
      stableCount++; 
  }

}

void loop() 
{
      if(Serial1.available()> 0)
      {         
         byte inpx = Serial1.read();
         if(mode ==1)
         {
             Serial.write(inpx);
         }
         else
         {
           switch(inpx)
           {
               case 250: 
                   sendData(fversion);
                   delay(10);  
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
                   //Set MODE command to serial coms only
                   multi = 0;
                   mode = 1;
                   break;
               case 249:
                  //Set MODE command back to serial keyboard
                  Serial.println("<KEYMODE>");
                  mode = 0;
                  multi = 0;
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

      }
      //Check for output from the attacker
      //via the open serial open connection.
      //if so write this back via wifi
      //to attacker device.
      if(Serial.available() > 0)
      {
          //this is using stream so we should be ok...
          String res = Serial.readString();
          if(res.startsWith("<KEYMODE>") || res.startsWith("<QUIT>"))
          {
            mode = 0;
          }
          sendData(res);   
      }
} 

void sendData(String inx)
{
   int lx = inx.length();
   if(lx > 0)
   {
      Serial1.println(inx);  
   } 
}

