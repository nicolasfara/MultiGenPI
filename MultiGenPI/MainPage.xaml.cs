using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;

// Il modello di elemento per la pagina vuota è documentato all'indirizzo http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x410

namespace MultiGenPI
{
    /// <summary>
    /// Pagina vuota che può essere utilizzata autonomamente oppure esplorata all'interno di un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            InitAll();        
        }

        private async Task InitSpi()
        {
            try
            {
                var settingsDDS = new SpiConnectionSettings(0);
                var settingsDAC = new SpiConnectionSettings(1);
                settingsDDS.ClockFrequency = 1000000;
                settingsDAC.ClockFrequency = 1000000;
                settingsDDS.Mode = SpiMode.Mode0;
                settingsDAC.Mode = SpiMode.Mode0;

                string spiDDS = SpiDevice.GetDeviceSelector("SPI0");
                string spiDAC = SpiDevice.GetDeviceSelector("SPI0");
                var deviceInfoDDS = await DeviceInformation.FindAllAsync(spiDDS);
                var deviceInfoDAC = await DeviceInformation.FindAllAsync(spiDAC);
                DDS = await SpiDevice.FromIdAsync(deviceInfoDDS[0].Id, settingsDDS);
                DAC = await SpiDevice.FromIdAsync(deviceInfoDAC[0].Id, settingsDAC);
            }
            catch(Exception ex)
            {
                throw new Exception("Spi initialization fail", ex);
            }

        }

        private void InitGpio()
        {
            var gpio = GpioController.GetDefault();
            IO_Sync = gpio.OpenPin(22);
            IO_Sync.Write(GpioPinValue.Low);
            IO_Sync.SetDriveMode(GpioPinDriveMode.Output);
        }

        private async void InitAll()
        {
            try
            {
                InitGpio();
                await InitSpi();
            }
            catch(Exception ex)
            {
                throw new Exception("Init fail", ex);
            }
        }

        private SpiDevice DDS;
        private SpiDevice DAC;
        private GpioPin IO_Sync;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            byte[] dds = new byte[3]{ 0xFF, 0x0F, 0xF0 };
            DDS.Write(dds);
        }

        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            byte[] dac = new byte[3] { 0xFF, 0x0F, 0xF0 };
            DAC.Write(dac);
        }
    }

    
}
