#include <Servo.h>

Servo ESC9;
Servo ESC10;
Servo ESC11;
Servo ESC13;

int base_throttle = 0;
int offset_change_by = 2;

int offset9 = 0;
int offset10 = 0;
int offset11 = 0;
int offset13 = 0;

int minOffset = 0;
int maxOffset = 1245;

void setup()
{ 
  ESC9.attach(9, 600, 2225);
  ESC10.attach(10, 600, 2225);
  ESC11.attach(11, 600, 2225);
  ESC13.attach(13, 600, 2225);
  
  base_throttle = 180;
  ESC9.write(base_throttle);
  ESC10.write(base_throttle);
  ESC11.write(base_throttle);
  ESC13.write(base_throttle);
  delay(6000);
  
  base_throttle = 0;
  ESC9.write(base_throttle);
  ESC10.write(base_throttle);
  ESC11.write(base_throttle);
  ESC13.write(base_throttle);
  delay(6000);
  
  Serial1.begin(9600);
  base_throttle = 1005;
}


void loop()
{
  if(Serial1.available() > 0)
  {
    String string = Serial1.readStringUntil(',');
    int str_len = string.length();
    if(string.charAt(0) == ';' && string.charAt(str_len - 1) == ';' && str_len > 1)
    { 
      int opcode = (string.substring(1, str_len - 1)).toInt();
      switch(opcode){
        case 4000:
          if((offset9 - offset_change_by >= minOffset) && (offset11 - offset_change_by >= minOffset))
          {
            offset9 -= offset_change_by;
            offset11 -= offset_change_by;
          }
          break;
        case 4001:
          if((offset10 - offset_change_by >= minOffset) && (offset13 - offset_change_by >= minOffset))
          {
            offset10 -= offset_change_by;
            offset13 -= offset_change_by;
          }
          break;
        case 5000:
          if(offset9 + offset_change_by <= maxOffset)
          {
            offset9 += offset_change_by;
          }
          break;
        case 5001:
          if(offset9 - offset_change_by >= minOffset)
            {
              offset9 -= offset_change_by;
            }
          break;
        case 5002:
          if(offset10 + offset_change_by <= maxOffset)
            {
              offset10 += offset_change_by;
            }
          break;
        case 5003:
          if(offset10 - offset_change_by >= minOffset)
          {
            offset10 -= offset_change_by;
          }
          break;
        case 5004:
          if(offset11 + offset_change_by <= maxOffset)
          {
            offset11 += offset_change_by;
          }
          break;
        case 5005:
          if(offset11 - offset_change_by >= minOffset)
          {
            offset11 -= offset_change_by;
          }
          break;
        case 5006:
          if(offset13 + offset_change_by <= maxOffset)
          {
            offset13 += offset_change_by;
          }
          break;
        case 5007:
          if(offset13 - offset_change_by >= minOffset)
          {
            offset13 -= offset_change_by;
          }
          break;
        case 5008:
          offset9 = 0;
          break;
        case 5009:
          offset10 = 0;
          break;
        case 5010:
          offset11 = 0;
          break;
        case 5011:
          offset13 = 0;
          break;
        default:
          break;
      }
      
      ESC9.writeMicroseconds(base_throttle + offset9);
      ESC10.writeMicroseconds(base_throttle + offset10);
      ESC11.writeMicroseconds(base_throttle + offset11);
      ESC13.writeMicroseconds(base_throttle + offset13);
    }
    
    // read the input on analog pin 0:
    int pressureSensorValue = analogRead(A0);
    // convert to voltage:
    // float voltage = pressureSensorValue * (5.0 / 1023.0);
    // compute pressure in KPa
    //float pressure = ((pressureSensorValue / 1023.0) * 413.05) + 3.478;
    Serial1.println(String(pressureSensorValue)); 
  }
}
