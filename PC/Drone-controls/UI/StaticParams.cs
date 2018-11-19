using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI
{
    public class StaticParams
    {
        public const string serverHostName = "192.168.1.243";//"vietraspi.ddns.net";
        public const Int32 sendingCommandToArduiPort = 5004;
        public const Int32 readArduiFeedbackPort = 5003;
        public const Int32 readGyroDataPort = 5002;
        public const Int32 sendingUdpDatatoArduiPort = 5001;
        public const Int32 videoStreamingPort = 5000;
        public const int minPower = 0;//1005;//43;//84
        public const int maxPower = 100;//2250;//180;//135
        public static byte[] signalToIncreaseESC9OffsetBy1 = Encoding.ASCII.GetBytes(",;5000;,");
        public static byte[] signalToDecreaseESC9OffsetBy1 = Encoding.ASCII.GetBytes(",;5001;,");
        public static byte[] signalToIncreaseESC10OffsetBy1 = Encoding.ASCII.GetBytes(",;5002;,");
        public static byte[] signalToDecreaseESC10OffsetBy1 = Encoding.ASCII.GetBytes(",;5003;,");
        public static byte[] signalToIncreaseESC11OffsetBy1 = Encoding.ASCII.GetBytes(",;5004;,");
        public static byte[] signalToDecreaseESC11OffsetBy1 = Encoding.ASCII.GetBytes(",;5005;,");
        public static byte[] signalToIncreaseESC13OffsetBy1 = Encoding.ASCII.GetBytes(",;5006;,");
        public static byte[] signalToDecreaseESC13OffsetBy1 = Encoding.ASCII.GetBytes(",;5007;,");
        public static byte[] signalToResetESC9Offset = Encoding.ASCII.GetBytes(",;5008;,");
        public static byte[] signalToResetESC10Offset = Encoding.ASCII.GetBytes(",;5009;,");
        public static byte[] signalToResetESC11Offset = Encoding.ASCII.GetBytes(",;5010;,");
        public static byte[] signalToResetESC13Offset = Encoding.ASCII.GetBytes(",;5011;,");
    }
}
