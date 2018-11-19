'''
        Read Gyro and Accelerometer by Interfacing Raspberry Pi with MPU6050 using Python
	http://www.electronicwings.com
'''
import smbus			#import SMBus module of I2C
import math
import socket
from threading import Thread
import time
import pigpio

class RPi_sensor_processor:
    
    #some MPU6050 Registers and their Address
    PWR_MGMT_1   = 0x6B
    SMPLRT_DIV   = 0x19
    CONFIG       = 0x1A
    GYRO_CONFIG  = 0x1B
    INT_ENABLE   = 0x38
    ACCEL_XOUT_H = 0x3B
    ACCEL_YOUT_H = 0x3D
    ACCEL_ZOUT_H = 0x3F
    GYRO_XOUT_H  = 0x43
    GYRO_YOUT_H  = 0x45
    GYRO_ZOUT_H  = 0x47
    BUS = smbus.SMBus(1)    # or bus = smbus.SMBus(0) for older version boards
    DEV_ADDR = 0x68         # MPU6050 device address
    
##    acc_offset_x = -0.06
##    acc_offset_y = 0.04
##    acc_offset_z = 0.1
    
    COUNT_PERIOD = 0.5 #seconds
    WEIGHT_OF_PRE_RPM = 0
    NOISE_RPM = 100
    MIN_RPM = 5000
    CURR_LOC_IN_RPM_LIST_9 = bytearray(b'\x00')
    CURR_LOC_IN_RPM_LIST_10 = bytearray(b'\x00')
    CURR_LOC_IN_RPM_LIST_11 = bytearray(b'\x00')
    CURR_LOC_IN_RPM_LIST_13 = bytearray(b'\x00')
    RPM_LIST_9 = []
    RPM_LIST_10 = []
    RPM_LIST_11 = []
    RPM_LIST_13 = []
    CURR_DESIRED_RPM_9 = 0
    CURR_DESIRED_RPM_10 = 0
    CURR_DESIRED_RPM_11 = 0
    CURR_DESIRED_RPM_13 = 0
    RPM_LST_LEN = 100
    RPM_CLOSE_TO_DESIRED = 200

    def __init__(self, ser, acc_offset_x, acc_offset_y, acc_offset_z, udp_port, ard_controls):
        self.HALL_INPUT_PIN_9 = 17
        self.passed_time_9 = time.time()
        self.rpm_9 = 0

        self.HALL_INPUT_PIN_10 = 18
        self.passed_time_10 = time.time()
        self.rpm_10 = 0

        self.HALL_INPUT_PIN_11 = 27
        self.passed_time_11 = time.time()
        self.rpm_11 = 0

        self.HALL_INPUT_PIN_13 = 22
        self.passed_time_13 = time.time()
        self.rpm_13 = 0
        
        self.pi = pigpio.pi()
        
        self.pi.set_mode(self.HALL_INPUT_PIN_9, pigpio.INPUT)
        self.pi.set_mode(self.HALL_INPUT_PIN_10, pigpio.INPUT)
        self.pi.set_mode(self.HALL_INPUT_PIN_11, pigpio.INPUT)
        self.pi.set_mode(self.HALL_INPUT_PIN_13, pigpio.INPUT)
        
        self.pi.set_pull_up_down(self.HALL_INPUT_PIN_9, pigpio.PUD_UP)
        self.pi.set_pull_up_down(self.HALL_INPUT_PIN_10, pigpio.PUD_UP)
        self.pi.set_pull_up_down(self.HALL_INPUT_PIN_11, pigpio.PUD_UP)
        self.pi.set_pull_up_down(self.HALL_INPUT_PIN_13, pigpio.PUD_UP)
        
        self.callback_9 = self.pi.callback(self.HALL_INPUT_PIN_9)
        self.callback_10 = self.pi.callback(self.HALL_INPUT_PIN_10)
        self.callback_11 = self.pi.callback(self.HALL_INPUT_PIN_11)
        self.callback_13 = self.pi.callback(self.HALL_INPUT_PIN_13)
        
        self.pi.set_watchdog(self.HALL_INPUT_PIN_9, 200) #200 msecs
        self.pi.set_watchdog(self.HALL_INPUT_PIN_10, 200) #
        self.pi.set_watchdog(self.HALL_INPUT_PIN_11, 200) #
        self.pi.set_watchdog(self.HALL_INPUT_PIN_13, 200) #
        
        self.ser = ser
        self.udp_port = udp_port
        self.acc_offset_x = acc_offset_x
        self.acc_offset_y = acc_offset_y
        self.acc_offset_z = acc_offset_z
        self.__MPU_Init()
        
        self.udp_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
        self.udp_sock.bind(('', self.udp_port))
        self.udp_sock.settimeout(0.01) #seconds
        
        self.ard_controls = ard_controls


    def MPU_Init(self):
        #write to sample rate register
        self.BUS.write_byte_data(self.DEV_ADDR, self.SMPLRT_DIV, 7)
            
        #Write to power management register
        self.BUS.write_byte_data(self.DEV_ADDR, self.PWR_MGMT_1, 1)
            
        #Write to Configuration register
        self.BUS.write_byte_data(self.DEV_ADDR, self.CONFIG, 0)
            
        #Write to Gyro configuration register
        self.BUS.write_byte_data(self.DEV_ADDR, self.GYRO_CONFIG, 24)
            
        #Write to interrupt enable register
        self.BUS.write_byte_data(self.DEV_ADDR, self.INT_ENABLE, 1)

    __MPU_Init = MPU_Init


    def read_raw_data(self, addr):
        #Accelero and Gyro value are 16-bit
        high = self.BUS.read_byte_data(self.DEV_ADDR, addr)
        low = self.BUS.read_byte_data(self.DEV_ADDR, addr+1)
        
        #concatenate higher and lower value
        value = ((high << 8) | low)
            
        #to get signed value from mpu6050
        if(value > 32768):
            value = value - 65536
            
        return value

    

    def compute_data(self):
        print("HANDLING GYRO DATA: thread started.")
        
        while True:
            try:
                _, pc_address = self.udp_sock.recvfrom(16)
                #Read Accelerometer raw value
                acc_x = self.read_raw_data(self.ACCEL_XOUT_H)
                acc_y = self.read_raw_data(self.ACCEL_YOUT_H)
                acc_z = self.read_raw_data(self.ACCEL_ZOUT_H)
                    
                #Read Gyroscope raw value
                gyro_x = self.read_raw_data(self.GYRO_XOUT_H)
                gyro_y = self.read_raw_data(self.GYRO_YOUT_H)
                gyro_z = self.read_raw_data(self.GYRO_ZOUT_H)
                    
                #Full scale range +/- 250 degree/C as per sensitivity scale factor
                Ax = math.asin(max(min(acc_x/16384.0 + self.acc_offset_x, 1), -1)) * 180 / math.pi
                Ay = math.asin(max(min(acc_y/16384.0 + self.acc_offset_y, 1), -1)) * 180 / math.pi
                Az = math.asin(max(min(acc_z/16384.0 + self.acc_offset_z, 1), -1)) * 180 / math.pi
                    
                Gx = gyro_x/131.0 
                Gy = gyro_y/131.0 
                Gz = gyro_z/131.0
                
                
                if time.time() - self.passed_time_9 >= self.COUNT_PERIOD:
                    sample_rpm_9 = int(round((8.571429 * self.callback_9.tally() / (time.time() - self.passed_time_9)) * (1.0 - self.WEIGHT_OF_PRE_RPM) + (self.rpm_9 * self.WEIGHT_OF_PRE_RPM)))
                    if self.CURR_DESIRED_RPM_9 != self.ard_controls.desired_rpm:
                        self.rpm_9 = sample_rpm_9
                        self.CURR_DESIRED_RPM_9 = self.ard_controls.desired_rpm
                        self.RPM_LIST_9 = []
                        self.CURR_LOC_IN_RPM_LIST_9[0] = 0
                    else:
                        if abs(self.ard_controls.desired_rpm - sample_rpm_9) <= self.RPM_CLOSE_TO_DESIRED:
                            if len(self.RPM_LIST_9) == self.RPM_LST_LEN:
                                self.RPM_LIST_9[self.CURR_LOC_IN_RPM_LIST_9[0]] = sample_rpm_9
                                self.rpm_9 = reduce(lambda x, y: x + y, self.RPM_LIST_9) / self.RPM_LST_LEN
                            else:
                                self.rpm_9 = sample_rpm_9
                                self.RPM_LIST_9.append(sample_rpm_9)
                        else:
                            self.rpm_9 = sample_rpm_9
                        if self.CURR_LOC_IN_RPM_LIST_9[0] < self.RPM_LST_LEN - 1:
                            self.CURR_LOC_IN_RPM_LIST_9[0] += 1
                        else:
                            self.CURR_LOC_IN_RPM_LIST_9[0] = 0
                        
                    if self.rpm_9 < self.NOISE_RPM:
                        self.rpm_9 = 0
                    if (self.rpm_9 < self.ard_controls.desired_rpm) and (self.ard_controls.desired_rpm >= self.MIN_RPM):
                        self.ser.write(",;5000;,")
                    elif self.rpm_9 > self.ard_controls.desired_rpm:
                        self.ser.write(",;5001;,")
                    self.passed_time_9 = time.time()
                    self.callback_9.reset_tally()
                    
                if time.time() - self.passed_time_10 >= self.COUNT_PERIOD:
                    sample_rpm_10 = int(round((8.571429 * self.callback_10.tally() / (time.time() - self.passed_time_10)) * (1.0 - self.WEIGHT_OF_PRE_RPM) + (self.rpm_10 * self.WEIGHT_OF_PRE_RPM)))
                    if self.CURR_DESIRED_RPM_10 != self.ard_controls.desired_rpm:
                        self.rpm_10 = sample_rpm_10
                        self.CURR_DESIRED_RPM_10 = self.ard_controls.desired_rpm
                        self.RPM_LIST_10 = []
                        self.CURR_LOC_IN_RPM_LIST_10[0] = 0
                    else:
                        if abs(self.ard_controls.desired_rpm - sample_rpm_10) <= self.RPM_CLOSE_TO_DESIRED:
                            if len(self.RPM_LIST_10) == self.RPM_LST_LEN:
                                self.RPM_LIST_10[self.CURR_LOC_IN_RPM_LIST_10[0]] = sample_rpm_10
                                self.rpm_10 = reduce(lambda x, y: x + y, self.RPM_LIST_10) / self.RPM_LST_LEN
                            else:
                                self.rpm_10 = sample_rpm_10
                                self.RPM_LIST_10.append(sample_rpm_10)
                        else:
                            self.rpm_10 = sample_rpm_10
                        if self.CURR_LOC_IN_RPM_LIST_10[0] < self.RPM_LST_LEN - 1:
                            self.CURR_LOC_IN_RPM_LIST_10[0] += 1
                        else:
                            self.CURR_LOC_IN_RPM_LIST_10[0] = 0
                    
                    if self.rpm_10 < self.NOISE_RPM:
                        self.rpm_10 = 0
                    if (self.rpm_10 < self.ard_controls.desired_rpm) and (self.ard_controls.desired_rpm >= self.MIN_RPM):
                        self.ser.write(",;5002;,")
                    elif self.rpm_10 > self.ard_controls.desired_rpm:
                        self.ser.write(",;5003;,")
                    self.passed_time_10 = time.time()
                    self.callback_10.reset_tally()
                    
                if time.time() - self.passed_time_11 >= self.COUNT_PERIOD:
                    sample_rpm_11 = int(round((8.571429 * self.callback_11.tally() / (time.time() - self.passed_time_11)) * (1.0 - self.WEIGHT_OF_PRE_RPM) + (self.rpm_11 * self.WEIGHT_OF_PRE_RPM)))
                    if self.CURR_DESIRED_RPM_11 != self.ard_controls.desired_rpm:
                        self.rpm_11 = sample_rpm_11
                        self.CURR_DESIRED_RPM_11 = self.ard_controls.desired_rpm
                        self.RPM_LIST_11 = []
                        self.CURR_LOC_IN_RPM_LIST_11[0] = 0
                    else:
                        if abs(self.ard_controls.desired_rpm - sample_rpm_11) <= self.RPM_CLOSE_TO_DESIRED:
                            if len(self.RPM_LIST_11) == self.RPM_LST_LEN:
                                self.RPM_LIST_11[self.CURR_LOC_IN_RPM_LIST_11[0]] = sample_rpm_11
                                self.rpm_11 = reduce(lambda x, y: x + y, self.RPM_LIST_11) / self.RPM_LST_LEN
                            else:
                                self.rpm_11 = sample_rpm_11
                                self.RPM_LIST_11.append(sample_rpm_11)
                        else:
                            self.rpm_11 = sample_rpm_11
                        if self.CURR_LOC_IN_RPM_LIST_11[0] < self.RPM_LST_LEN - 1:
                            self.CURR_LOC_IN_RPM_LIST_11[0] += 1
                        else:
                            self.CURR_LOC_IN_RPM_LIST_11[0] = 0

                    if self.rpm_11 < self.NOISE_RPM:
                        self.rpm_11 = 0
                    if (self.rpm_11 < self.ard_controls.desired_rpm) and (self.ard_controls.desired_rpm >= self.MIN_RPM):
                        self.ser.write(",;5004;,")
                    elif self.rpm_11 > self.ard_controls.desired_rpm:
                        self.ser.write(",;5005;,")
                    self.passed_time_11 = time.time()
                    self.callback_11.reset_tally()
                    
                if time.time() - self.passed_time_13 >= self.COUNT_PERIOD:
                    sample_rpm_13 = int(round((8.571429 * self.callback_13.tally() / (time.time() - self.passed_time_13)) * (1.0 - self.WEIGHT_OF_PRE_RPM) + (self.rpm_13 * self.WEIGHT_OF_PRE_RPM)))
                    if self.CURR_DESIRED_RPM_13 != self.ard_controls.desired_rpm:
                        self.rpm_13 = sample_rpm_13
                        self.CURR_DESIRED_RPM_13 = self.ard_controls.desired_rpm
                        self.RPM_LIST_13 = []
                        self.CURR_LOC_IN_RPM_LIST_13[0] = 0
                    else:
                        if abs(self.ard_controls.desired_rpm - sample_rpm_13) <= self.RPM_CLOSE_TO_DESIRED:
                            if len(self.RPM_LIST_13) == self.RPM_LST_LEN:
                                self.RPM_LIST_13[self.CURR_LOC_IN_RPM_LIST_13[0]] = sample_rpm_13
                                self.rpm_13 = reduce(lambda x, y: x + y, self.RPM_LIST_13) / self.RPM_LST_LEN
                            else:
                                self.rpm_13 = sample_rpm_13
                                self.RPM_LIST_13.append(sample_rpm_13)
                        else:
                            self.rpm_13 = sample_rpm_13
                        if self.CURR_LOC_IN_RPM_LIST_13[0] < self.RPM_LST_LEN - 1:
                            self.CURR_LOC_IN_RPM_LIST_13[0] += 1
                        else:
                            self.CURR_LOC_IN_RPM_LIST_13[0] = 0
                    
                    if self.rpm_13 < self.NOISE_RPM:
                        self.rpm_13 = 0
                    if (self.rpm_13 < self.ard_controls.desired_rpm) and (self.ard_controls.desired_rpm >= self.MIN_RPM):
                        self.ser.write(",;5006;,")
                    elif self.rpm_13 > self.ard_controls.desired_rpm:
                        self.ser.write(",;5007;,")
                    self.passed_time_13 = time.time()
                    self.callback_13.reset_tally()
                
                data = ",;{0} {1} {2} {3} {4} {5} {6} {7} {8} {9};,".format(Gx,Gy,Gz,Ax,Ay,Az,self.rpm_9,self.rpm_10,self.rpm_11,self.rpm_13)
                self.udp_sock.sendto(data.encode(),pc_address)
                #time.sleep(0.01)
            except socket.timeout:
                pass
            
    def run(self):
        read_gyro_thread = Thread(target=self.compute_data)
        read_gyro_thread.daemon = True
        read_gyro_thread.start()
