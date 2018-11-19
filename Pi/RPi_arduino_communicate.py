import socket
import serial
from threading import Thread

class arduino_controls:
    
    def __init__(
        self,
        ser,
        get_command_for_ardui_tcp_port,
        get_data_for_ardui_udp_port,
        ardui_feedback_udp_port): # ser, 5004, 5001, 5003
        
        self.ser = ser
        self.ser.flushInput()
        
        self.get_command_for_ardui_tcp_port = get_command_for_ardui_tcp_port
        self.get_data_for_ardui_udp_port = get_data_for_ardui_udp_port
        self.ardui_feedback_udp_port = ardui_feedback_udp_port
        
        self.get_command_for_ardui_tcp_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.get_command_for_ardui_tcp_sock.bind(('', get_command_for_ardui_tcp_port))
        self.get_command_for_ardui_tcp_sock.listen(1)
        
        self.get_data_for_ardui_udp_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
        #self.get_data_for_ardui_udp_sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
        self.get_data_for_ardui_udp_sock.bind(('', self.get_data_for_ardui_udp_port))
        self.get_data_for_ardui_udp_sock.settimeout(0.01) #seconds
        
        self.adui_feedback_udp_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
        #self.adui_feedback_udp_sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
        self.adui_feedback_udp_sock.bind(('', self.ardui_feedback_udp_port))
        self.adui_feedback_udp_sock.settimeout(0.01) #seconds
        
        self.desired_rpm = 0
        
    def send_important_commands_to_arduino(self):
        print("GETTING COMMAND FOR ARDUI: thread started.")
        while True:
            print("GETTING COMMAND FOR ARDUI: waiting for a TCP connection on port {0}".format(self.get_command_for_ardui_tcp_port))
            connection, client_addr = self.get_command_for_ardui_tcp_sock.accept()
            connection.setblocking(False)
            connection.settimeout(1)
            # connection.sendall("{0}".format(self.tcp_port))
            print("GETTING COMMAND FOR ARDUI: TCP connection established on port {0}".format(self.get_command_for_ardui_tcp_port))
    
            while True:
                try:
                    data = connection.recv(16)
                    if data == "":
                        break
                    split_data = data.split(",")
                    for elem in split_data:
                        if elem.startswith(';') and elem.endswith(';') and len(elem) > 1:
                            # print(elem)
                            self.ser.write(',' + elem + ',' )
                except Exception as e:
                    if e.__class__ != socket.timeout:
                        break;
            
            connection.close()
            print("GETTING COMMAND FOR ARDUI: TCP connection closed on port {0}".format(self.get_command_for_ardui_tcp_port))
       
    def handle_received_udp_command(self):
        print("GETTING UDP DATA FOR ARDUI: thread started.")
        
        while True:
            try:
                data, _ = self.get_data_for_ardui_udp_sock.recvfrom(16)
                split_data = data.split(",")
                for elem in split_data:
                    if elem.startswith(';') and elem.endswith(';') and len(elem) > 1:
                        print(elem)
                        val = int(elem[1:-1])
                        if val >= 0 and val <= 100:
                            self.desired_rpm = val * 80;
                        else:
                            self.ser.write(',' + elem + ',' )
            except Exception as e:
                if e.__class__ != socket.timeout:
                    print(e)
                    break;
            
        self.get_data_for_ardui_udp_sock.close()
        print("GETTING UDP DATA FOR ARDUI: thread ended.") 

        
    def handle_arduino_feedback(self):
        print("SENDING ARDUINO FEEDBACK: thread started.")
    
        while True:
            try:
                #time.sleep(0.001)
                _, pc_address = self.adui_feedback_udp_sock.recvfrom(16)
                adui_feedback = ",;" + self.ser.readline() + ";,"
                if len(adui_feedback) > 4:
                    self.adui_feedback_udp_sock.sendto(adui_feedback.encode(), pc_address)
                    
            except Exception as e:
                if e.__class__ != socket.timeout:
                    break;
        
        connection.close()
        print("SENDING ARDUINO FEEDBACK: thread ended")
        
    def run(self):
        ardui_tcp_command_thread = Thread(target=self.send_important_commands_to_arduino)
        ardui_tcp_command_thread.daemon = True
        ardui_tcp_command_thread.start()
        
        ardui_udp_command_thread = Thread(target=self.handle_received_udp_command)
        ardui_udp_command_thread.daemon = True
        ardui_udp_command_thread.start()
        
        arduino_feedback_thread = Thread(target=self.handle_arduino_feedback)
        arduino_feedback_thread.daemon = True
        arduino_feedback_thread.start()
        
    
