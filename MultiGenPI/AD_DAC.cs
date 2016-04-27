using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;

namespace MultiGenPI
{
    public class AD_DAC
    {
        public AD_DAC()
        {
            InitAll();
            Debug.WriteLine("Inizializzato Correttamente");
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
            catch (Exception ex)
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
        async void InitAll()
        {
            try
            {
                InitGpio();
                await InitSpi();
            }
            catch (Exception ex)
            {
                throw new Exception("Init fail", ex);
            }
        }

        private void writeDAC(UInt16 word)
        {
            Byte MSB = (Byte)(word >> 8);
            Byte LSB = (Byte)(word & 0x00FF);
            Byte Operation = 0x00;
            byte[] write = { LSB, MSB, Operation };
            DAC.Write(write);           
        }

        public void setAmplitude(double amplitude)
        {
            UInt16 word;

            //Ricavo i valori dall'ampiezza da fornire al DAC
            //  Ampiezza in ingresso al VGA = 0.1V
            //  Tensione riferimento DAC = 950mV
            //  VGA 20mV/dB
            //  risoluzione DAC 16 bit
            word = (UInt16)((65536/950)*(400 * Math.Log10(10 * amplitude) + 100));

            //Invio valore al DAC
            writeDAC(word);
        } //fine setAmplitude()



        //definizioni oggetti
        private SpiDevice DDS;
        private SpiDevice DAC;
        private GpioPin IO_Sync;
    }
}
