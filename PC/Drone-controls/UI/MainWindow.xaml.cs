using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CefSharp;


namespace UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // variables relate to engine controls
        private Tuple<TcpClient, NetworkStream> tcpClientAndNetworkStreamForArduiCommand;
        private Thread getArduiFeedbackThread;
        private bool arduinoFeedbackThreadAllowedToRun;
        private Tuple<UdpClient, IPEndPoint> udpClientAndIPEndpointForArduinoFeedback;
        private Thread udpControllingArduiThread;
        private Boolean udpControllingArduiThreadAllowedToRun;
        private Tuple<UdpClient, IPEndPoint> udpClientAndIPEndpointForArduiCommand;
        private double powerSldMax;
        private DateTimeOffset lastUpdatePowerValTime = DateTimeOffset.Now;
        private string currentServoAngleVal = ",;" + ((int)StaticParams.minPower).ToString() + ";,";

        // variable relate to gyro sensor
        private Tuple<UdpClient, IPEndPoint> udpClientandIPEndpointForGyro;
        private Thread getGyroDataThread;
        private bool getGyroDataThreadAllowedTOrun;
        private System.Windows.Forms.Timer visualizeDroneStatusTimer;
        private double xAngle, yAngle, newXAngle, newYAngle;
        private double zAngle, newZAngle;
        private GeometryModel3D droneGeometry;
        private Vector3D oldRotateAxis;
        private double revertYAngle;
        private double revertXAngle;

        public MainWindow()
        {
            InitializeComponent();

            SetUIComponentsStatusForFailConnection();

            powerSld.Maximum = StaticParams.maxPower - StaticParams.minPower;
            powerSldMax = powerSld.Maximum;

            xAngle = yAngle = newXAngle = newYAngle = 0;
            zAngle = newZAngle = 90;
            visualizeDroneStatusTimer = new System.Windows.Forms.Timer
            {
                Interval = 200
            };
            visualizeDroneStatusTimer.Tick += new EventHandler(VisualizeDroneStatus);
            visualizeDroneStatusTimer.Enabled = true;

            // drone's geometry creation
            var droneBodyMesh = BuildDroneMeshGeometry3D();
            droneGeometry = new GeometryModel3D(droneBodyMesh, new DiffuseMaterial(Brushes.YellowGreen))
            {
                Transform = new Transform3DGroup()
            };
            group.Children.Add(droneGeometry);
            revertXAngle = revertYAngle = 0.0;
            oldRotateAxis = new Vector3D(4, 0, 0);
        }

        private bool CommunicateWithRpiToGetArduinoFeedback()
        {
            arduinoFeedbackThreadAllowedToRun = true;
            udpClientAndIPEndpointForArduinoFeedback = 
                Communication.GetUdpClientAndIPEndPointClientSide(
                    StaticParams.readArduiFeedbackPort,
                    StaticParams.serverHostName);
            if (udpClientAndIPEndpointForArduinoFeedback != null)
            {
                getArduiFeedbackThread = new Thread(() =>
                {
                    byte[] buffer;
                    var tempIPEndpoint = udpClientAndIPEndpointForArduinoFeedback.Item2;
                    while (arduinoFeedbackThreadAllowedToRun)
                    {
                        try
                        {
                            udpClientAndIPEndpointForArduinoFeedback.Item1.Send(Encoding.ASCII.GetBytes("1"), 1);
                            buffer = udpClientAndIPEndpointForArduinoFeedback.Item1.Receive(ref tempIPEndpoint);
                            if (buffer.Length > 0)
                            {
                                var multiLineRawArduinoFeedback = (Encoding.Default.GetString(buffer)).Split(',');
                                foreach (var oneLineRawArduinoFeedback in multiLineRawArduinoFeedback)
                                {
                                    if (oneLineRawArduinoFeedback.StartsWith(";") &&
                                    oneLineRawArduinoFeedback.EndsWith(";") &&
                                    oneLineRawArduinoFeedback.Length > 1)
                                    {
                                        var pressure = ((double.Parse(oneLineRawArduinoFeedback.Trim(';')) / 1023) * 413.05) + 3.478;
                                        airPressureTb.Dispatcher.Invoke(() =>
                                        {
                                            airPressureTb.Text = "Air pressure: " + pressure.ToString() + "KPa";
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is SocketException))
                            {
                                SetUIComponentsStatusForFailConnection();
                                break;
                            }
                        }
                    }
                    udpClientAndIPEndpointForArduinoFeedback.Item1.Dispose();
                    udpClientAndIPEndpointForArduinoFeedback.Item1.Close();
                });
                getArduiFeedbackThread.Start();
                return true;
            }
            return false;
        }

        private bool CommunicateWithRPiToGetGyroData()
        {
            getGyroDataThreadAllowedTOrun = true;
            udpClientandIPEndpointForGyro = 
                Communication.GetUdpClientAndIPEndPointClientSide(
                    StaticParams.readGyroDataPort,
                    StaticParams.serverHostName);
            if (udpClientandIPEndpointForGyro != null)
            {
                getGyroDataThread = new Thread(() =>
                {
                    byte[] buffer;
                    var tempIpEndpoint = udpClientandIPEndpointForGyro.Item2;

                    while (getGyroDataThreadAllowedTOrun)
                    {
                        try
                        {
                            udpClientandIPEndpointForGyro.Item1.Send(Encoding.ASCII.GetBytes("1"), 1);
                            buffer = udpClientandIPEndpointForGyro.Item1.Receive(ref tempIpEndpoint);
                            var allData = Encoding.Default.GetString(buffer);
                            var commaSplitTokens = allData.Split(',');
                            foreach (var token in commaSplitTokens)
                            {
                                if (token.StartsWith(";") && token.EndsWith(";") && token.Length > 1)
                                {
                                    var data = token.Trim(';').Split(' ');
                                    if (data.Length == 10)
                                    {
                                        newXAngle = double.Parse(data[3]);
                                        newYAngle = double.Parse(data[4]);
                                        newZAngle = double.Parse(data[5]);
                                        offsetESC9Tb.Dispatcher.Invoke(() =>
                                        {
                                            offsetESC9Tb.Text = "RPM #09 = " + data[6];
                                            offsetESC10Tb.Text = "RPM #10 = " + data[7];
                                            offsetESC11Tb.Text = "RPM #11 = " + data[8];
                                            offsetESC13Tb.Text = "RPM #13 = " + data[9];
                                        });
                                        break;
                                    }
                                }
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is SocketException))
                            {
                                SetUIComponentsStatusForFailConnection();
                                break;
                            }
                        }
                    }
                    udpClientandIPEndpointForGyro.Item1.Dispose();
                    udpClientandIPEndpointForGyro.Item1.Close();
                });

                getGyroDataThread.Start();
                return true;
            }
            return false;
        }

        private bool UdpCommunicateWithRPiToControlArdui()
        {
            udpControllingArduiThreadAllowedToRun = true;
            udpClientAndIPEndpointForArduiCommand = 
                Communication.GetUdpClientAndIPEndPointClientSide(
                    StaticParams.sendingUdpDatatoArduiPort,
                    StaticParams.serverHostName);
            if (udpClientAndIPEndpointForArduiCommand != null)
            {
                udpControllingArduiThread = new Thread(() =>
                {
                    while (udpControllingArduiThreadAllowedToRun)
                    {
                        if ((DateTimeOffset.Now - lastUpdatePowerValTime).TotalSeconds > 2)
                        {
                            try
                            {
                                var data = Encoding.ASCII.GetBytes(currentServoAngleVal);
                                udpClientAndIPEndpointForArduiCommand.Item1.Send(data, data.Length);
                                Thread.Sleep(1000);
                            }
                            catch (Exception)
                            {
                                SetUIComponentsStatusForFailConnection();
                                break;
                            }
                        }
                    }
                    udpClientAndIPEndpointForArduiCommand.Item1.Dispose();
                    udpClientAndIPEndpointForArduiCommand.Item1.Close();
                });

                udpControllingArduiThread.Start();
                return true;
            }
            return false;
        }

        private bool TcpCommunicateWithRPiToControlArdui()
        {
            tcpClientAndNetworkStreamForArduiCommand = Communication.GetTcpClientAndNetworkStream(
                StaticParams.serverHostName,
                StaticParams.sendingCommandToArduiPort);
            if (tcpClientAndNetworkStreamForArduiCommand != null)
            {
                return true;
            }

            SetUIComponentsStatusForFailConnection();
            return false;
        }

        private void VisualizeDroneStatus(object sender, EventArgs e)
        {
            visualizeDroneStatusTimer.Enabled = false;
         
            var latchedNewXAngle = newXAngle;
            var latchedNewYAngle = newYAngle;
            var latchedNewZAngle = newZAngle;

            var transform3DGroup = droneGeometry.Transform as Transform3DGroup;

            QuaternionRotation3D r =
                new QuaternionRotation3D(
                new Quaternion(
                oldRotateAxis, revertYAngle));
            transform3DGroup.Children.Add(new RotateTransform3D(r));

            r =
            new QuaternionRotation3D(
            new Quaternion(
            new Vector3D(0, 4, 0), revertXAngle));
            transform3DGroup.Children.Add(new RotateTransform3D(r));

            Vector3D xRotateAxis;
            double yRotAngle;
            if (latchedNewXAngle >= 0 && latchedNewZAngle >=0 && latchedNewYAngle >= 0)
            {
                r =
                new QuaternionRotation3D(
                new Quaternion(
                new Vector3D(0, 4, 0), -latchedNewXAngle));
                transform3DGroup.Children.Add(new RotateTransform3D(r));
                revertXAngle = latchedNewXAngle;

                xRotateAxis = new Vector3D(4 * Math.Cos(latchedNewXAngle * Math.PI / 180), 0, 4 * Math.Sin(latchedNewXAngle * Math.PI / 180));
                if (latchedNewXAngle == 90)
                {
                    yRotAngle = 0;
                }
                else if (latchedNewXAngle == 0)
                {
                    yRotAngle = -latchedNewYAngle;
                }
                else
                {
                    yRotAngle = latchedNewYAngle * 90 / (90 - latchedNewXAngle);
                }
                r =
                new QuaternionRotation3D(
                new Quaternion(
                xRotateAxis, yRotAngle));
                revertYAngle = -yRotAngle;
                transform3DGroup.Children.Add(new RotateTransform3D(r));
            }
            else if(latchedNewXAngle >= 0 && latchedNewZAngle >= 0 && latchedNewYAngle < 0)
            {
                r =
                new QuaternionRotation3D(
                new Quaternion(
                new Vector3D(0, 4, 0), -latchedNewXAngle));
                transform3DGroup.Children.Add(new RotateTransform3D(r));
                revertXAngle = latchedNewXAngle;

                xRotateAxis = new Vector3D(4 * Math.Cos(latchedNewXAngle * Math.PI / 180), 0, 4 * Math.Sin(latchedNewXAngle * Math.PI / 180));
                if (latchedNewXAngle == 90)
                {
                    yRotAngle = 0;
                }
                else if (latchedNewXAngle == 0)
                {
                    yRotAngle = -latchedNewYAngle;
                }
                else
                {
                    yRotAngle = latchedNewYAngle * 90 / (latchedNewXAngle - 90);
                }
                r =
                new QuaternionRotation3D(
                new Quaternion(
                xRotateAxis, -yRotAngle));
                revertYAngle = yRotAngle;
                transform3DGroup.Children.Add(new RotateTransform3D(r));
            }
            else if(latchedNewXAngle >= 0 && latchedNewZAngle < 0 && latchedNewYAngle >= 0)
            {
                r =
                new QuaternionRotation3D(
                new Quaternion(
                new Vector3D(0, 4, 0), -180 + latchedNewXAngle));
                transform3DGroup.Children.Add(new RotateTransform3D(r));
                revertXAngle = 180 - latchedNewXAngle;

                xRotateAxis = new Vector3D(-4 * Math.Cos(latchedNewXAngle * Math.PI / 180), 0, 4 * Math.Sin(latchedNewXAngle * Math.PI / 180));
                if (latchedNewXAngle == 90)
                {
                    yRotAngle = 0;
                }
                else if (latchedNewXAngle == 0)
                {
                    yRotAngle = latchedNewYAngle;
                }
                else
                {
                    yRotAngle = latchedNewYAngle * 90 / (90 - latchedNewXAngle);
                }
                r =
                new QuaternionRotation3D(
                new Quaternion(
                xRotateAxis, -yRotAngle));
                revertYAngle = yRotAngle;
                transform3DGroup.Children.Add(new RotateTransform3D(r));
            }
            else if(latchedNewXAngle >= 0 && latchedNewZAngle < 0 && latchedNewYAngle < 0)
            {
                r =
                new QuaternionRotation3D(
                new Quaternion(
                new Vector3D(0, 4, 0), -180 + latchedNewXAngle));
                transform3DGroup.Children.Add(new RotateTransform3D(r));
                revertXAngle = 180 - latchedNewXAngle;

                xRotateAxis = new Vector3D(-4 * Math.Cos(latchedNewXAngle * Math.PI / 180), 0, 4 * Math.Sin(latchedNewXAngle * Math.PI / 180));
                if (latchedNewXAngle == 90)
                {
                    yRotAngle = 0;
                }
                else if (latchedNewXAngle == 0)
                {
                    yRotAngle = latchedNewYAngle;
                }
                else
                {
                    yRotAngle = latchedNewYAngle * 90 / (latchedNewXAngle - 90);
                }
                r =
                new QuaternionRotation3D(
                new Quaternion(
                xRotateAxis, yRotAngle));
                revertYAngle = -yRotAngle;
                transform3DGroup.Children.Add(new RotateTransform3D(r));
            }
            else if (latchedNewXAngle < 0 && latchedNewZAngle >= 0 && latchedNewYAngle >= 0)
            {
                r =
                new QuaternionRotation3D(
                new Quaternion(
                new Vector3D(0, 4, 0), -latchedNewXAngle));
                transform3DGroup.Children.Add(new RotateTransform3D(r));
                revertXAngle = latchedNewXAngle;
                
                xRotateAxis = new Vector3D(4 * Math.Cos(latchedNewXAngle * Math.PI / 180), 0, 4 * Math.Sin(latchedNewXAngle * Math.PI / 180));
                if (latchedNewXAngle == -90)
                {
                    yRotAngle = 0;
                }
                else
                {
                    yRotAngle = latchedNewYAngle * 90 / (90 + latchedNewXAngle);
                }
                r =
                new QuaternionRotation3D(
                new Quaternion(
                xRotateAxis, yRotAngle));
                revertYAngle = -yRotAngle;
                transform3DGroup.Children.Add(new RotateTransform3D(r));
            }
            else if (latchedNewXAngle < 0 && latchedNewZAngle >= 0 && latchedNewYAngle < 0)
            {
                r =
                new QuaternionRotation3D(
                new Quaternion(
                new Vector3D(0, 4, 0), -latchedNewXAngle));
                transform3DGroup.Children.Add(new RotateTransform3D(r));
                revertXAngle = latchedNewXAngle;

                xRotateAxis = new Vector3D(4 * Math.Cos(latchedNewXAngle * Math.PI / 180), 0, 4 * Math.Sin(latchedNewXAngle * Math.PI / 180));
                if (latchedNewXAngle == -90)
                {
                    yRotAngle = 0;
                }
                else
                {
                    yRotAngle = latchedNewYAngle * 90 / (latchedNewXAngle - 90);
                }
                r =
                new QuaternionRotation3D(
                new Quaternion(
                xRotateAxis, -yRotAngle));
                revertYAngle = yRotAngle;
                transform3DGroup.Children.Add(new RotateTransform3D(r));
            }
            else if (latchedNewXAngle < 0 && latchedNewZAngle < 0 && latchedNewYAngle >= 0)
            {
                r =
                new QuaternionRotation3D(
                new Quaternion(
                new Vector3D(0, 4, 0), -latchedNewXAngle));
                transform3DGroup.Children.Add(new RotateTransform3D(r));
                revertXAngle = latchedNewXAngle;

                xRotateAxis = new Vector3D(4 * Math.Cos(latchedNewXAngle * Math.PI / 180), 0, 4 * Math.Sin(latchedNewXAngle * Math.PI / 180));
                if (latchedNewXAngle == -90)
                {
                    yRotAngle = 0;
                }
                else
                {
                    yRotAngle = 180 + latchedNewYAngle * 90 / (latchedNewXAngle + 90);
                }
                r =
                new QuaternionRotation3D(
                new Quaternion(
                xRotateAxis, yRotAngle));
                revertYAngle = -yRotAngle;
                transform3DGroup.Children.Add(new RotateTransform3D(r));
            }
            else
            {
                r =
                new QuaternionRotation3D(
                new Quaternion(
                new Vector3D(0, 4, 0), -latchedNewXAngle));
                transform3DGroup.Children.Add(new RotateTransform3D(r));
                revertXAngle = latchedNewXAngle;

                xRotateAxis = new Vector3D(4 * Math.Cos(latchedNewXAngle * Math.PI / 180), 0, 4 * Math.Sin(latchedNewXAngle * Math.PI / 180));
                if (latchedNewXAngle == -90)
                {
                    yRotAngle = 0;
                }
                else
                {
                    yRotAngle = 180 + latchedNewYAngle * 90 / (latchedNewXAngle + 90);
                }
                r =
                new QuaternionRotation3D(
                new Quaternion(
                xRotateAxis, -yRotAngle));
                revertYAngle = yRotAngle;
                transform3DGroup.Children.Add(new RotateTransform3D(r));
            }
            oldRotateAxis = xRotateAxis;

            xAngle = latchedNewXAngle;
            yAngle = latchedNewYAngle;
            zAngle = latchedNewZAngle;
            xAngleTb.Text = "x = " + Math.Round(xAngle).ToString() + " degrees";
            yAngleTb.Text = "y = " + Math.Round(yAngle).ToString() + " degrees";
            zAngleTb.Text = "z = " + Math.Round(zAngle).ToString() + " degrees";

            
            visualizeDroneStatusTimer.Enabled = true;
        }

        private void EngineControlConnectBttClick(object sender, RoutedEventArgs e)
        {
            
            if (TcpCommunicateWithRPiToControlArdui() &&
                UdpCommunicateWithRPiToControlArdui() &&
                CommunicateWithRpiToGetArduinoFeedback() &&
                CommunicateWithRPiToGetGyroData())
            { 
                SetUIComponentsStatusForSuccessfulConnection();
            }
            else
            {
                //TODO: Report when connections fail to establish
            }
        }

        private void EngineControlDisconnectBttClick(object sender, RoutedEventArgs e)
        {
            SetUIComponentsStatusForFailConnection();
        }

        private void SetUIComponentsStatusForSuccessfulConnection()
        {
            engineControlConnectBtt.IsEnabled = false;
            engineControlDisconnectBtt.IsEnabled = true;
            powerSld.IsEnabled = true;
            add1ToOffsetESC9Btt.IsEnabled = true;
            add1ToOffsetESC10Btt.IsEnabled = true;
            add1ToOffsetESC11Btt.IsEnabled = true;
            add1ToOffsetESC13Btt.IsEnabled = true;
            decrease1ToOffsetESC9Btt.IsEnabled = true;
            decrease1ToOffsetESC10Btt.IsEnabled = true;
            decrease1ToOffsetESC11Btt.IsEnabled = true;
            decrease1ToOffsetESC13Btt.IsEnabled = true;

            resetPowerBarBtt.IsEnabled = true;
            resetOffsetESC9Btt.IsEnabled = true;
            resetOffsetESC10Btt.IsEnabled = true;
            resetOffsetESC11Btt.IsEnabled = true;
            resetOffsetESC13Btt.IsEnabled = true;
        }

        private void SetUIComponentsStatusForFailConnection()
        {
            if(tcpClientAndNetworkStreamForArduiCommand != null)
            {
                tcpClientAndNetworkStreamForArduiCommand.Item2.Dispose();
                tcpClientAndNetworkStreamForArduiCommand.Item1.Dispose();
                tcpClientAndNetworkStreamForArduiCommand.Item1.Close();
            }

            udpControllingArduiThreadAllowedToRun = false;
            arduinoFeedbackThreadAllowedToRun = false;
            getGyroDataThreadAllowedTOrun = false;
            engineControlConnectBtt.Dispatcher.Invoke(() =>
            {
                engineControlConnectBtt.IsEnabled = true;
                engineControlDisconnectBtt.IsEnabled = false;
                powerSld.IsEnabled = false;
                add1ToOffsetESC9Btt.IsEnabled = false;
                add1ToOffsetESC10Btt.IsEnabled = false;
                add1ToOffsetESC11Btt.IsEnabled = false;
                add1ToOffsetESC13Btt.IsEnabled = false;
                decrease1ToOffsetESC9Btt.IsEnabled = false;
                decrease1ToOffsetESC10Btt.IsEnabled = false;
                decrease1ToOffsetESC11Btt.IsEnabled = false;
                decrease1ToOffsetESC13Btt.IsEnabled = false;

                resetPowerBarBtt.IsEnabled = false;
                resetOffsetESC9Btt.IsEnabled = false;
                resetOffsetESC10Btt.IsEnabled = false;
                resetOffsetESC11Btt.IsEnabled = false;
                resetOffsetESC13Btt.IsEnabled = false;
            });
        }

        private void Add1ToOffsetESC9BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToIncreaseESC9OffsetBy1,
                    StaticParams.signalToIncreaseESC9OffsetBy1.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void Add1ToOffsetESC10BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToIncreaseESC10OffsetBy1,
                    StaticParams.signalToIncreaseESC10OffsetBy1.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void Add1ToOffsetESC11BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToIncreaseESC11OffsetBy1,
                    StaticParams.signalToIncreaseESC11OffsetBy1.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void Add1ToOffsetESC13BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToIncreaseESC13OffsetBy1,
                    StaticParams.signalToIncreaseESC13OffsetBy1.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void Decrease1ToOffsetESC9BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToDecreaseESC9OffsetBy1,
                    StaticParams.signalToDecreaseESC9OffsetBy1.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void Decrease1ToOffsetESC10BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToDecreaseESC10OffsetBy1,
                    StaticParams.signalToDecreaseESC10OffsetBy1.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void Decrease1ToOffsetESC11BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToDecreaseESC11OffsetBy1,
                    StaticParams.signalToDecreaseESC11OffsetBy1.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void Decrease1ToOffsetESC13BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToDecreaseESC13OffsetBy1,
                    StaticParams.signalToDecreaseESC13OffsetBy1.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void PowerSldValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var powerPercentage = Math.Round(powerSld.Value * 100 / powerSld.Maximum, 2);
                powerTb.Text = powerPercentage.ToString() + "%";
                currentServoAngleVal = ",;" + ((int)powerSld.Value + StaticParams.minPower).ToString() + ";,";
                var data = Encoding.ASCII.GetBytes(currentServoAngleVal);
                udpClientAndIPEndpointForArduiCommand.Item1.Send(data, data.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void ResetOffsetESC9BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToResetESC9Offset,
                    StaticParams.signalToResetESC9Offset.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void ResetOffsetESC10BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToResetESC10Offset,
                    StaticParams.signalToResetESC10Offset.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void ResetOffsetESC11BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToResetESC11Offset,
                    StaticParams.signalToResetESC11Offset.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void ResetOffsetESC13BttClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientAndIPEndpointForArduiCommand.Item1.Send(
                    StaticParams.signalToResetESC13Offset,
                    StaticParams.signalToResetESC13Offset.Length);

                lastUpdatePowerValTime = DateTimeOffset.Now;
            }
            catch (Exception)
            {
                SetUIComponentsStatusForFailConnection();
            }
        }

        private void ResetPowerBarBttClick(object sender, RoutedEventArgs e)
        {
            powerSld.Value = 0;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            SetUIComponentsStatusForFailConnection();
        }

        private void StartCamBttClick(object sender, RoutedEventArgs e)
        {
            chromeBr.Load("http://" + StaticParams.serverHostName + ":" + StaticParams.videoStreamingPort.ToString());
        }

        private void StopCamBttClick(object sender, RoutedEventArgs e)
        {
            chromeBr.Stop();
        }

        private MeshGeometry3D BuildDroneMeshGeometry3D()
        {
            // Define 3D mesh drone
            MeshGeometry3D droneBodymesh = new MeshGeometry3D();
            // Front face
            droneBodymesh.Positions.Add(new Point3D(-6, -2, -0.5));
            droneBodymesh.Positions.Add(new Point3D(6, -2, -0.5));
            droneBodymesh.Positions.Add(new Point3D(6, -2, 0.5));
            droneBodymesh.Positions.Add(new Point3D(-6, -2, 0.5));
            // Back face
            droneBodymesh.Positions.Add(new Point3D(-6, 2, -0.5));
            droneBodymesh.Positions.Add(new Point3D(6, 2, -0.5));
            droneBodymesh.Positions.Add(new Point3D(6, 2, 0.5));
            droneBodymesh.Positions.Add(new Point3D(-6, 2, 0.5));
            // front head wing
            droneBodymesh.Positions.Add(new Point3D(3, -2, -0.125));
            droneBodymesh.Positions.Add(new Point3D(6, -2, -0.125));
            droneBodymesh.Positions.Add(new Point3D(6, -2, 0.125));
            droneBodymesh.Positions.Add(new Point3D(3, -2, 0.125));
            droneBodymesh.Positions.Add(new Point3D(9, -6, -0.125));
            droneBodymesh.Positions.Add(new Point3D(9, -6, 0.125));
            // front tail wing
            droneBodymesh.Positions.Add(new Point3D(-6, -2, -0.125));
            droneBodymesh.Positions.Add(new Point3D(-3, -2, -0.125));
            droneBodymesh.Positions.Add(new Point3D(-3, -2, 0.125));
            droneBodymesh.Positions.Add(new Point3D(-6, -2, 0.125));
            droneBodymesh.Positions.Add(new Point3D(-9, -6, -0.125));
            droneBodymesh.Positions.Add(new Point3D(-9, -6, 0.125));
            // back head wing
            droneBodymesh.Positions.Add(new Point3D(3, 2, -0.125));
            droneBodymesh.Positions.Add(new Point3D(6, 2, -0.125));
            droneBodymesh.Positions.Add(new Point3D(6, 2, 0.125));
            droneBodymesh.Positions.Add(new Point3D(3, 2, 0.125));
            droneBodymesh.Positions.Add(new Point3D(9, 6, -0.125));
            droneBodymesh.Positions.Add(new Point3D(9, 6, 0.125));
            // back tail wing
            droneBodymesh.Positions.Add(new Point3D(-6, 2, -0.125));
            droneBodymesh.Positions.Add(new Point3D(-3, 2, -0.125));
            droneBodymesh.Positions.Add(new Point3D(-3, 2, 0.125));
            droneBodymesh.Positions.Add(new Point3D(-6, 2, 0.125));
            droneBodymesh.Positions.Add(new Point3D(-9, 6, -0.125));
            droneBodymesh.Positions.Add(new Point3D(-9, 6, 0.125));
            // drone's head
            droneBodymesh.Positions.Add(new Point3D(3, -0.25, 0.5));
            droneBodymesh.Positions.Add(new Point3D(6, -0.25, 0.5));
            droneBodymesh.Positions.Add(new Point3D(6, 0.25, 0.5));
            droneBodymesh.Positions.Add(new Point3D(3, 0.25, 0.5));
            droneBodymesh.Positions.Add(new Point3D(4.5, -0.25, 1.5));
            droneBodymesh.Positions.Add(new Point3D(4.5, 0.25, 1.5));


            // triangles for font face
            droneBodymesh.TriangleIndices.Add(0);
            droneBodymesh.TriangleIndices.Add(1);
            droneBodymesh.TriangleIndices.Add(2);
            droneBodymesh.TriangleIndices.Add(2);
            droneBodymesh.TriangleIndices.Add(3);
            droneBodymesh.TriangleIndices.Add(0);
            // Back face
            droneBodymesh.TriangleIndices.Add(6);
            droneBodymesh.TriangleIndices.Add(5);
            droneBodymesh.TriangleIndices.Add(4);
            droneBodymesh.TriangleIndices.Add(4);
            droneBodymesh.TriangleIndices.Add(7);
            droneBodymesh.TriangleIndices.Add(6);
            // Right face
            droneBodymesh.TriangleIndices.Add(1);
            droneBodymesh.TriangleIndices.Add(5);
            droneBodymesh.TriangleIndices.Add(2);
            droneBodymesh.TriangleIndices.Add(5);
            droneBodymesh.TriangleIndices.Add(6);
            droneBodymesh.TriangleIndices.Add(2);
            // Up face
            droneBodymesh.TriangleIndices.Add(3);
            droneBodymesh.TriangleIndices.Add(2);
            droneBodymesh.TriangleIndices.Add(6);
            droneBodymesh.TriangleIndices.Add(6);
            droneBodymesh.TriangleIndices.Add(7);
            droneBodymesh.TriangleIndices.Add(3);
            // back left face
            droneBodymesh.TriangleIndices.Add(0);
            droneBodymesh.TriangleIndices.Add(3);
            droneBodymesh.TriangleIndices.Add(4);
            droneBodymesh.TriangleIndices.Add(4);
            droneBodymesh.TriangleIndices.Add(3);
            droneBodymesh.TriangleIndices.Add(7);
            // bottom face
            droneBodymesh.TriangleIndices.Add(0);
            droneBodymesh.TriangleIndices.Add(5);
            droneBodymesh.TriangleIndices.Add(1);
            droneBodymesh.TriangleIndices.Add(0);
            droneBodymesh.TriangleIndices.Add(4);
            droneBodymesh.TriangleIndices.Add(5);
            // front head wing
            droneBodymesh.TriangleIndices.Add(8);
            droneBodymesh.TriangleIndices.Add(12);
            droneBodymesh.TriangleIndices.Add(13);
            droneBodymesh.TriangleIndices.Add(8);
            droneBodymesh.TriangleIndices.Add(13);
            droneBodymesh.TriangleIndices.Add(11);
            droneBodymesh.TriangleIndices.Add(9);
            droneBodymesh.TriangleIndices.Add(10);
            droneBodymesh.TriangleIndices.Add(12);
            droneBodymesh.TriangleIndices.Add(12);
            droneBodymesh.TriangleIndices.Add(10);
            droneBodymesh.TriangleIndices.Add(13);
            droneBodymesh.TriangleIndices.Add(11);
            droneBodymesh.TriangleIndices.Add(13);
            droneBodymesh.TriangleIndices.Add(10);
            droneBodymesh.TriangleIndices.Add(8);
            droneBodymesh.TriangleIndices.Add(9);
            droneBodymesh.TriangleIndices.Add(12);
            // front tail wing
            droneBodymesh.TriangleIndices.Add(14);
            droneBodymesh.TriangleIndices.Add(18);
            droneBodymesh.TriangleIndices.Add(17);
            droneBodymesh.TriangleIndices.Add(17);
            droneBodymesh.TriangleIndices.Add(18);
            droneBodymesh.TriangleIndices.Add(19);
            droneBodymesh.TriangleIndices.Add(15);
            droneBodymesh.TriangleIndices.Add(19);
            droneBodymesh.TriangleIndices.Add(18);
            droneBodymesh.TriangleIndices.Add(15);
            droneBodymesh.TriangleIndices.Add(16);
            droneBodymesh.TriangleIndices.Add(19);
            droneBodymesh.TriangleIndices.Add(17);
            droneBodymesh.TriangleIndices.Add(19);
            droneBodymesh.TriangleIndices.Add(16);
            droneBodymesh.TriangleIndices.Add(14);
            droneBodymesh.TriangleIndices.Add(15);
            droneBodymesh.TriangleIndices.Add(18);
            // back head wing
            droneBodymesh.TriangleIndices.Add(20);
            droneBodymesh.TriangleIndices.Add(25);
            droneBodymesh.TriangleIndices.Add(24);
            droneBodymesh.TriangleIndices.Add(20);
            droneBodymesh.TriangleIndices.Add(23);
            droneBodymesh.TriangleIndices.Add(25);
            droneBodymesh.TriangleIndices.Add(21);
            droneBodymesh.TriangleIndices.Add(24);
            droneBodymesh.TriangleIndices.Add(22);
            droneBodymesh.TriangleIndices.Add(22);
            droneBodymesh.TriangleIndices.Add(24);
            droneBodymesh.TriangleIndices.Add(25);
            droneBodymesh.TriangleIndices.Add(23);
            droneBodymesh.TriangleIndices.Add(22);
            droneBodymesh.TriangleIndices.Add(25);
            droneBodymesh.TriangleIndices.Add(20);
            droneBodymesh.TriangleIndices.Add(24);
            droneBodymesh.TriangleIndices.Add(21);
            // back tail wing
            droneBodymesh.TriangleIndices.Add(26);
            droneBodymesh.TriangleIndices.Add(31);
            droneBodymesh.TriangleIndices.Add(30);
            droneBodymesh.TriangleIndices.Add(26);
            droneBodymesh.TriangleIndices.Add(29);
            droneBodymesh.TriangleIndices.Add(31);
            droneBodymesh.TriangleIndices.Add(27);
            droneBodymesh.TriangleIndices.Add(30);
            droneBodymesh.TriangleIndices.Add(31);
            droneBodymesh.TriangleIndices.Add(31);
            droneBodymesh.TriangleIndices.Add(28);
            droneBodymesh.TriangleIndices.Add(27);
            droneBodymesh.TriangleIndices.Add(28);
            droneBodymesh.TriangleIndices.Add(31);
            droneBodymesh.TriangleIndices.Add(29);
            droneBodymesh.TriangleIndices.Add(26);
            droneBodymesh.TriangleIndices.Add(30);
            droneBodymesh.TriangleIndices.Add(27);
            // drone's head
            droneBodymesh.TriangleIndices.Add(32);
            droneBodymesh.TriangleIndices.Add(33);
            droneBodymesh.TriangleIndices.Add(36);
            //
            droneBodymesh.TriangleIndices.Add(34);
            droneBodymesh.TriangleIndices.Add(35);
            droneBodymesh.TriangleIndices.Add(37);
            //
            droneBodymesh.TriangleIndices.Add(35);
            droneBodymesh.TriangleIndices.Add(32);
            droneBodymesh.TriangleIndices.Add(36);
            droneBodymesh.TriangleIndices.Add(35);
            droneBodymesh.TriangleIndices.Add(36);
            droneBodymesh.TriangleIndices.Add(37);
            //
            droneBodymesh.TriangleIndices.Add(33);
            droneBodymesh.TriangleIndices.Add(37);
            droneBodymesh.TriangleIndices.Add(36);
            droneBodymesh.TriangleIndices.Add(33);
            droneBodymesh.TriangleIndices.Add(34);
            droneBodymesh.TriangleIndices.Add(37);

            return droneBodymesh;
        }
    }
}
