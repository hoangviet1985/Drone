using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UI
{
    public class Communication
    {
        /// <summary>
        /// attemps to establish communication with the server
        /// </summary>
        /// <param name="server">the server's host name</param>
        /// <param name="port">the port to connect to the server</param>
        /// <returns>a tuple of <see cref="TcpClient"/> instance and <see cref="NetworkStream"/> instance or null</returns>
        public static Tuple<TcpClient, NetworkStream> GetTcpClientAndNetworkStream(string server, Int32 port)
        {
            try
            {
                TcpClient client = new TcpClient(server, port);
                NetworkStream stream = client.GetStream();
                stream.WriteTimeout = 1000;
                stream.ReadTimeout = 5000;
                return new Tuple<TcpClient, NetworkStream>(client, stream);
            }
            catch (Exception)
            {
                return null;
            }

        }

        /// <summary>
        /// setup a udp server
        /// </summary>
        /// <param name="port">local port on which udp server listen</param>
        /// <param name="localIPInterface">local interface IP address on which server listen</param>
        /// <returns>pair of <see cref="UdpClient"> and <see cref="IPEndPoint"/></returns>
        public static Tuple<UdpClient, IPEndPoint> GetUdpClientAndIPEndPointServerSide(Int32 port, string localIPInterface = "")
        {
            try
            {
                var udpClient = new UdpClient();
                IPEndPoint endPoint;
                // if local interface IP is not identified, the server listen on all interface
                if (localIPInterface == "")
                {
                    endPoint = new IPEndPoint(IPAddress.Any, port);
                }
                else
                {
                    endPoint = new IPEndPoint(IPAddress.Parse(localIPInterface), port);
                }
                udpClient.Client.Bind(endPoint);
 
                return new Tuple<UdpClient, IPEndPoint>(udpClient, endPoint);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// set up a udp client
        /// </summary>
        /// <param name="port"></param>
        /// <param name="remoteHostNameOrIPAddress"></param>
        /// <returns></returns>
        public static Tuple<UdpClient, IPEndPoint> GetUdpClientAndIPEndPointClientSide(Int32 port, string remoteHostNameOrIPAddress)
        {
            try
            {
                if (!IPAddress.TryParse(remoteHostNameOrIPAddress, out IPAddress remoteHostIPAddr))
                {
                    remoteHostIPAddr =  (Dns.GetHostAddresses(remoteHostNameOrIPAddress))[0];
                }

                var udpClient = new UdpClient();
                var remoteIPEndpoint = new IPEndPoint(remoteHostIPAddr, port);
                udpClient.Connect(remoteIPEndpoint);
                udpClient.Client.ReceiveTimeout = 10; //miliseconds
                return new Tuple<UdpClient, IPEndPoint>(udpClient, remoteIPEndpoint);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
