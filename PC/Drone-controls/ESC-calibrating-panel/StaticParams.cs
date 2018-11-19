using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace ESC_calibrating_panel
{
    public class StaticParams
    {
        public const int defaultMinESCPW = 600;
        public const int defaultMaxESCPW = 2250;
        public const string arduinoDetectString = "arduino";

        public static SerialPort GetConnectedSerialPort()
        {
            var portList =  SerialPort.GetPortNames();
            foreach(var portName in portList)
            {
                var mPort = new SerialPort();
                mPort.PortName = portName;
                mPort.BaudRate = 9600;
                mPort.Parity = Parity.None;
                mPort.DataBits = 8;
                mPort.StopBits = StopBits.One;
                mPort.Handshake = Handshake.None;

                mPort.ReadTimeout = 500; //ms
                try
                {
                    mPort.Open();
                    while (true)
                    {
                        var str = mPort.ReadTo(" ");
                        if (str.Contains(arduinoDetectString))
                        {
                            return mPort;
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    mPort.Dispose();
                    mPort.Close();
                }
            }
            return null;
        }
    }
}
