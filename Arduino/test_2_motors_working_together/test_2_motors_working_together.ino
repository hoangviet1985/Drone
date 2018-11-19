#include <Servo.h>

Servo ESCa;
//Servo ESCb;
int throttle = 0;

void setup()
{
  ESCa.attach(11, 600, 2225);
  //ESCb.attach(13, 600, 2225);
  throttle = 180;
  ESCa.write(throttle);
  delay(10000);
  
  throttle = 0;
  ESCa.write(throttle);
  delay(10000);
  
  throttle = 90;
  ESCa.write(throttle);
  delay(10000);
  
  throttle = 0;
  ESCa.write(throttle);
  //ESCb.write(throttle);
  
  Serial1.begin(9600);
}


void loop()
{
  if(Serial1.available() > 0)
  {
    String string = Serial1.readStringUntil(',');
    int str_len = string.length();
    if(string.charAt(0) == ';' && string.charAt(str_len - 1) == ';')
    { 
      throttle = (string.substring(1, str_len - 1)).toInt();
      if(throttle >= 0 && throttle <= 180)
      {
        ESCa.write(throttle);
        //ESCb.write(throttle);
      }
    }
    
    // read the input on analog pin 0:
    int pressureSensorValue = analogRead(A0);
    // convert to voltage:
    // float voltage = pressureSensorValue * (5.0 / 1023.0);
    // compute pressure in KPa
    //float pressure = ((pressureSensorValue / 1023.0) * 413.05) + 3.478;
    Serial.println(pressureSensorValue);
  }
}
