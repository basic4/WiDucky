#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <WiFiClient.h>
#include <WiFiServer.h>
#include <ESP8266WiFiSTA.h>
#include <ESP8266WiFiMulti.h>
#include <ESP8266WiFiType.h>
#include <ESP8266WiFiScan.h>
#include <ESP8266WiFiAP.h>
#include <ESP8266WiFiGeneric.h>



//  *********************************************
//  **       WIFI-DUCKY PROTOTYPE   v1.44      **
//  *********************************************
//  **    basic4@privatoria.net   Jun. 2016    **
//  *********************************************
// ESP8266 Module Firmware
// with esp8266 on serial1 hardware port.
int inpx = 0;  // char from wifi
boolean quack =false;
int resetCounter = 0;
int ConnectionNumber = 0;
WiFiServer server (6673);
WiFiClient client;

void ledOn(boolean statex)
{
   switch (statex)
   {
     case true: 
       digitalWrite(LED_BUILTIN,LOW);
       break;
     case false:
       digitalWrite(LED_BUILTIN,HIGH);
       break;
   }
}

void ledPulse(int tim)
{
   ledOn(true);
   delay(tim);
   ledOn(false);
}



void setup() { 
  //enable the wifi chip
  pinMode(LED_BUILTIN, OUTPUT);
  //external reset mode
  //pinMode(12,INPUT_PULLUP);
  WiFi.mode(WIFI_AP);
  WiFi.softAP("WiDucky", "quackings");
  Serial.begin(19200);
  server.begin();
  ledOn(false);

}

void loop() 
{
  //Start the Server listening for client connections
  client = server.available();
  if(client)
  {
    while (client.connected())
     {
          if(!quack)
          {
            //send greeting
            ledOn(true);
            delay(1000);
            //Serial.println("Connected");
            quack = true;
             
                  client.print("**********************************\r\n");
                  client.print("**        WIFI_DUCK v1.44       **\r\n"); 
                  client.print("**********************************\r\n");
                  client.print("Ready...\r\n");
                  client.flush();
                  
             ledOn(false);                 
          }
          
          if(client.available())
          {       
             
              //Data rx and ready to send to the other master
              Serial.write(client.read());
          }
          else
          {
              while(Serial.available() > 0)
              {
                 client.print(Serial.readString());
                 client.flush();
              }       
          }
          
     }

  }
     if(!client)
     {
       ledPulse(5);
       delay(500);
       quack = false;
     }
 
} 













