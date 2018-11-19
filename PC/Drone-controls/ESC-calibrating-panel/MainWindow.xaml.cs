using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ESC_calibrating_panel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort serial;

        public MainWindow()
        {
            InitializeComponent();
            DisableAllUIComponents();
            ESC1Sld.Minimum = StaticParams.defaultMinESCPW;
            ESC2Sld.Minimum = StaticParams.defaultMinESCPW;
            ESC3Sld.Minimum = StaticParams.defaultMinESCPW;
            ESC4Sld.Minimum = StaticParams.defaultMinESCPW;
            ESC1Sld.Maximum = StaticParams.defaultMaxESCPW;
            ESC2Sld.Maximum = StaticParams.defaultMaxESCPW;
            ESC3Sld.Maximum = StaticParams.defaultMaxESCPW;
            ESC4Sld.Maximum = StaticParams.defaultMaxESCPW;

            serial = StaticParams.GetConnectedSerialPort();
            if(serial != null)
            {
                EnableAllUIComponents();
                
            }
        }

        private void MinMaxPWESC1Tb_LostFocus(object sender, RoutedEventArgs e)
        {
            if(!int.TryParse(minPWESC1Tb.Text, out int minPWESC1))
            {
                minPWESC1 = StaticParams.defaultMinESCPW;
            }

            if(!int.TryParse(maxPWESC1Tb.Text, out int maxPWESC1))
            {
                maxPWESC1 = StaticParams.defaultMaxESCPW;
            }

            if(minPWESC1 >= maxPWESC1)
            {
                minPWESC1 = StaticParams.defaultMinESCPW;
                maxPWESC1 = StaticParams.defaultMaxESCPW;
            }
            minPWESC1Tb.Text = minPWESC1.ToString();
            maxPWESC1Tb.Text = maxPWESC1.ToString();
            ESC1Sld.Minimum = minPWESC1;
            ESC1Sld.Maximum = maxPWESC1;
            currentPWESC1Tb.Text = minPWESC1.ToString();
            ESC1Sld.Minimum = minPWESC1;
            ESC1Sld.Maximum = maxPWESC1;
            ESC1Sld.Value = minPWESC1;
        }

        private void MinMaxPWESC2Tb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(minPWESC2Tb.Text, out int minPWESC2))
            {
                minPWESC2 = StaticParams.defaultMinESCPW;
            }

            if (!int.TryParse(maxPWESC2Tb.Text, out int maxPWESC2))
            {
                maxPWESC2 = StaticParams.defaultMaxESCPW;
            }

            if (minPWESC2 >= maxPWESC2)
            {
                minPWESC2 = StaticParams.defaultMinESCPW;
                maxPWESC2 = StaticParams.defaultMaxESCPW;
            }
            minPWESC2Tb.Text = minPWESC2.ToString();
            maxPWESC2Tb.Text = maxPWESC2.ToString();
            ESC2Sld.Minimum = minPWESC2;
            ESC2Sld.Maximum = maxPWESC2;
            currentPWESC2Tb.Text = minPWESC2.ToString();
            ESC2Sld.Minimum = minPWESC2;
            ESC2Sld.Maximum = maxPWESC2;
            ESC2Sld.Value = minPWESC2;
        }

        private void MinMaxPWESC3Tb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(minPWESC3Tb.Text, out int minPWESC3))
            {
                minPWESC3 = StaticParams.defaultMinESCPW;
            }

            if (!int.TryParse(maxPWESC3Tb.Text, out int maxPWESC3))
            {
                maxPWESC3 = StaticParams.defaultMaxESCPW;
            }

            if (minPWESC3 >= maxPWESC3)
            {
                minPWESC3 = StaticParams.defaultMinESCPW;
                maxPWESC3 = StaticParams.defaultMaxESCPW;
            }
            minPWESC3Tb.Text = minPWESC3.ToString();
            maxPWESC3Tb.Text = maxPWESC3.ToString();
            ESC3Sld.Minimum = minPWESC3;
            ESC3Sld.Maximum = maxPWESC3;
            currentPWESC3Tb.Text = minPWESC3.ToString();
            ESC3Sld.Minimum = minPWESC3;
            ESC3Sld.Maximum = maxPWESC3;
            ESC3Sld.Value = minPWESC3;
        }

        private void MinMaxPWESC4Tb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(minPWESC4Tb.Text, out int minPWESC4))
            {
                minPWESC4 = StaticParams.defaultMinESCPW;
            }

            if (!int.TryParse(maxPWESC4Tb.Text, out int maxPWESC4))
            {
                maxPWESC4 = StaticParams.defaultMaxESCPW;
            }

            if (minPWESC4 >= maxPWESC4)
            {
                minPWESC4 = StaticParams.defaultMinESCPW;
                maxPWESC4 = StaticParams.defaultMaxESCPW;
            }
            minPWESC4Tb.Text = minPWESC4.ToString();
            maxPWESC4Tb.Text = maxPWESC4.ToString();
            ESC4Sld.Minimum = minPWESC4;
            ESC4Sld.Maximum = maxPWESC4;
            currentPWESC4Tb.Text = minPWESC4.ToString();
            ESC4Sld.Minimum = minPWESC4;
            ESC4Sld.Maximum = maxPWESC4;
            ESC4Sld.Value = minPWESC4;
        }

        private void ESC1Sld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                serial.Write("1" + ((int)ESC1Sld.Value).ToString() + "\n");
                currentPWESC1Tb.Text = ESC1Sld.Value.ToString();
            }
            catch (Exception) { }
        }

        private void ESC2Sld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                serial.Write("2" + ((int)ESC2Sld.Value).ToString() + "\n");
                currentPWESC2Tb.Text = ESC2Sld.Value.ToString();
            }
            catch (Exception) { }
        }

        private void ESC3Sld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                serial.Write("3" + ((int)ESC3Sld.Value).ToString() + "\n");
                currentPWESC3Tb.Text = ESC3Sld.Value.ToString();
            }
            catch (Exception) { }
        }

        private void ESC4Sld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                serial.Write("4" + ((int)ESC4Sld.Value).ToString() + "\n");
                currentPWESC4Tb.Text = ESC4Sld.Value.ToString();
            }
            catch (Exception) { }
        }

        private void EnableAllUIComponents()
        {
            minPWESC1Tb.IsEnabled = true;
            minPWESC2Tb.IsEnabled = true;
            minPWESC3Tb.IsEnabled = true;
            minPWESC4Tb.IsEnabled = true;
            maxPWESC1Tb.IsEnabled = true;
            maxPWESC2Tb.IsEnabled = true;
            maxPWESC3Tb.IsEnabled = true;
            maxPWESC4Tb.IsEnabled = true;
            ESC1Sld.IsEnabled = true;
            ESC2Sld.IsEnabled = true;
            ESC3Sld.IsEnabled = true;
            ESC4Sld.IsEnabled = true;
        }

        private void DisableAllUIComponents()
        {
            minPWESC1Tb.IsEnabled = false;
            minPWESC2Tb.IsEnabled = false;
            minPWESC3Tb.IsEnabled = false;
            minPWESC4Tb.IsEnabled = false;
            maxPWESC1Tb.IsEnabled = false;
            maxPWESC2Tb.IsEnabled = false;
            maxPWESC3Tb.IsEnabled = false;
            maxPWESC4Tb.IsEnabled = false;
            ESC1Sld.IsEnabled = false;
            ESC2Sld.IsEnabled = false;
            ESC3Sld.IsEnabled = false;
            ESC4Sld.IsEnabled = false;
        }
    }
}
