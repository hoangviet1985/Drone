from RPi_arduino_communicate import arduino_controls
from RPi_sensor_processor import RPi_sensor_processor
import serial

ser = serial.Serial("/dev/ttyUSB0", 9600, timeout=0.1)
ser.flushInput()

PC_RPi_Arduino_com = arduino_controls(ser, 5004, 5001, 5003)
PC_RPi_Arduino_com.run()

rpi_sensor_processor = RPi_sensor_processor(ser, -0.06, 0.04, 0.12, 5002, PC_RPi_Arduino_com)
rpi_sensor_processor.run()

while True:
    pass