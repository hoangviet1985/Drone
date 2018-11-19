#include <Servo.h>

Servo ESC9;
Servo ESC10;
Servo ESC11;
Servo ESC13;
int throttle = 0;

void setup()
{
  ESC9.attach(9, 600, 2225);
  ESC10.attach(10, 600, 2225);
  ESC11.attach(11, 600, 2225);
  ESC13.attach(13, 600, 2225);
  
  throttle = 180;
  ESC9.write(throttle);
  ESC10.write(throttle);
  ESC11.write(throttle);
  ESC13.write(throttle);
  delay(10000);
  
  throttle = 0;
  ESC9.write(throttle);
  ESC10.write(throttle);
  ESC11.write(throttle);
  ESC13.write(throttle);
  delay(10000);
  
  throttle = 90;
  ESC9.write(throttle);
  ESC10.write(throttle);
  ESC11.write(throttle);
  ESC13.write(throttle);
  delay(10000);
  
  throttle = 0;
  ESC9.write(throttle);
  ESC10.write(throttle);
  ESC11.write(throttle);
  ESC13.write(throttle);
  
  Serial1.begin(9600);
}


void loop()
{
  if(Serial1.available() > 0)
  {
    String string = Serial.readStringUntil(',');
    int str_len = string.length();
    if(string.charAt(0) == ';' && string.charAt(str_len - 1) == ';')
    { 
      throttle = (string.substring(1, str_len - 1)).toInt();
      ESC9.write(throttle);
      ESC10.write(throttle);
      ESC11.write(throttle);
      ESC13.write(throttle);
    }
    
    // read the input on analog pin 0:
    int pressureSensorValue = analogRead(A0);
    // convert to voltage:
    // float voltage = pressureSensorValue * (5.0 / 1023.0);
    // compute pressure in KPa
    //float pressure = ((pressureSensorValue / 1023.0) * 413.05) + 3.478;
    Serial1.println(pressureSensorValue);
  }
}
