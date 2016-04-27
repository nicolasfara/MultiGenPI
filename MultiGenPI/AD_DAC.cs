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
        //=======================================
        //  DICHIARAZIONI VARIABILI
        ulong _freq, _ftw, __refClk, _refIn;
        const ulong AD9954_CLOCK = 400000000;
        const byte AD9954_FTW0 = 0x04;
        const byte AD9954_FTW0_SIZE = 0x04;
        const ulong AD9954_SYNC_CLK = AD9954_CLOCK / 4;
        const byte AD9954_RAM_RAMP_UP = 0x01;
        const byte AD9954_SWEEP_LOG = 0x01;
        const byte AD9954_CFR1 = 0x00;
        const byte AD9954_CFR1_SIZE = 0x04;
        const ulong AD9954_CFR1_RAM_ENABLED = 0x80000000;
        const ulong AD9954_CFR1_RAM_DISABLE = 0x00000000;
        const ulong AD9954_CFR1_RAM_DEST_PHASE = 0x40000000;
        const ulong AD9954_CFR1_RAM_DEST_FREQ = 0x00000000;

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
            var IOSync = GpioController.GetDefault();
            IO_Sync = IOSync.OpenPin(24);
            IO_Sync.Write(GpioPinValue.Low);
            IO_Sync.SetDriveMode(GpioPinDriveMode.Output);

            var IOUpdate = GpioController.GetDefault();
            IO_Update = IOUpdate.OpenPin(25);
            IO_Update.Write(GpioPinValue.Low);
            IO_Update.SetDriveMode(GpioPinDriveMode.Output);

            var Res = GpioController.GetDefault();
            Reset = Res.OpenPin(22);
            Reset.Write(GpioPinValue.Low);
            Reset.SetDriveMode(GpioPinDriveMode.Output);
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
        } //FINE setAmplitude()

        public void AD9954_IoUpdate()
        {
            IO_Update.Write(GpioPinValue.High);
            Task.Delay(1);
            IO_Update.Write(GpioPinValue.Low);
            Task.Delay(1);
        }   //FINE AD9954_IoUpdate()

        public void AD9954_reset()
        {
            Reset.Write(GpioPinValue.High);
            Task.Delay(10);
            Reset.Write(GpioPinValue.Low);
            Task.Delay(10);
        }   //FINE AD9954_Reset()

        private void AD9954_WriteReg(byte RegNum, byte RegSize, ulong Data)
        {
            if (RegSize < 1 || RegSize > 4)
            {
                Debug.WriteLine("Dimensione registro superata");
                return;
            }

            byte[] word_32 = { (byte)(Data >> 24), (byte)(Data >> 16), (byte)(Data >> 8), (byte)(Data) };
            byte[] word_24 = { (byte)(Data >> 16), (byte)(Data >> 8), (byte)(Data) };
            byte[] word_16 = { (byte)(Data >> 8), (byte)(Data) };
            byte[] word_8 = { (byte)(Data) };

            if (RegSize == 4)
                DDS.Write(word_32);
            if (RegSize == 3)
                DDS.Write(word_24);
            if (RegSize == 2)
                DDS.Write(word_16);
            if (RegSize == 1)
                DDS.Write(word_8);
        }   //FINE AD9954_WriteReg()

        private ulong AD9954_ReadReg(byte RegNum, byte RegSize) 
        {
            ulong Val;

            // CONTROLLO RANGE SU REGSIZE
            if (RegSize < 1 || RegSize > 4)
                return 0;

            byte[] _cmd = { (byte)(RegNum | 0x80) };
            DDS.Write(_cmd);

            byte[] Read = { };
            DDS.Read(Read);

            Val = BitConverter.ToUInt32(Read, 0);

            return Val;
        }

        public bool AD9954_SetFreq(double Freq)
        {
            // DETERMINA ORA IL VALORE DELL' FTW
            Freq = Freq / AD9954_CLOCK * 65536.0 * 65536.0;

            // SE FUORI SCALA, RITORNA ERRORE
            if (Freq > 65536.0 * 32768.0)
                return false;

            // TUTTO OK, IMPOSTA LA FREQUENZA
            AD9954_WriteReg(AD9954_FTW0, AD9954_FTW0_SIZE, (ulong)Freq);

            // ESEGUE UN IO/UPDATE PER AGGIORNARE I REGISTRI
            AD9954_IoUpdate();

            return true;   
        }   //FINE AD9954_WriteReg()

        ////////////////////////////////////////////////////////////
        // SCRITTURA REGISTRO DI CONTROLLO RAM
        public void AD9954_WriteRSCW(byte nReg, byte Mode, UInt16 StartAddr, UInt16 EndAddr, UInt16 RampRate, bool NoDwell)
        {
            byte b;
            // DETERMINA IL NUMERO DEL REGISTRO DA SCRIVERE
            // E SCRIVE IL COMANDO
            byte[] _nReg = { (byte)((nReg & 0x03) + 0x07) };
            DDS.Write(_nReg);

            // SCRIVE IL RAMP RATE
            byte[] _RampRate = { (byte)(RampRate), (byte)(RampRate >> 8) };
            DDS.Write(_RampRate);

            // SCRIVE LA PARTE BASSA DELL' INDIRIZZO FINALE
            byte[] _EndAddr = { (byte)(EndAddr) };
            DDS.Write(_EndAddr);

            // SCRIVE LA PARTE ALTA DELL' INDIRIZZO FINALE
            // COMBINATA CON QUELLA BASSA DELL' INDIRIZZO INIZIALE
            b = (byte)((EndAddr >> 8) & 0x03);
            b = (byte)(b | StartAddr << 2);
            byte[] _b = { b };
            DDS.Write(_b);

            // SCRIVE PARTE ALTA DELL' INDIRIZZO INIZIALE,
            // MODE E DWELL BYTE
            b = (byte)((StartAddr >> 6) & 0x0F);
            b = (byte)(b | (NoDwell ? 0x10 : 0x00));
            b = (byte)(b | ((Mode & 0x07) << 5));
            byte[] __b = { b };
            DDS.Write(__b);

            // ESEGUE UN IO/UPDATE PER AGGIORNARE I REGISTRI
            AD9954_IoUpdate();
        }   //FINE AD9954_WriteRSCW()

        public void AD9954_StartRamWrite()
        {
            byte[] _StartWriteRAM = { 0x0B };
            DDS.Write(_StartWriteRAM);
        }   //FINE AD9954_StartRamWrite()

        public void AD9954_StartRamRead()
        {
            byte[] _StartRamRead = { 0x8B };
            DDS.Write(_StartRamRead);
        }   //FINE AD9954_StartRamRead()

        public void AD9954_WriteRamByte(byte b)
        {
            byte[] _b = { b };
            DDS.Write(_b);
        }   //FINE AD9954_WriteRamByte()

        public byte[] AD9954_ReadRam()
        {
            byte[] b = { };

            DDS.Read(b);

            return b;
        }   //FINE AD9954_ReadRam()

        public void AD9954_WriteRamWord (UInt16 Word)
        {
            byte[] _Word = { (byte)(Word >> 8), (byte)(Word) };
            DDS.Write(_Word);
        }   //FINE AD9954_WriteRamWord()

        public void AD9954_WriteRamLong(ulong l)
        {
            byte[] _l = { (byte)(l >> 24), (byte)(l >> 16), (byte)(l >> 8), (byte)(l) };
            DDS.Write(_l);
        }

        public bool AD9954_SetSweep(byte SweepMode, double F1, double F2, double Time)
        {
            double RampRate;
            int i;
            ulong cfr1;

            // FATTORE DI INCREMENTO ESPONENZIALE
            const double ExpBase = 1.2;

            // SE F2 <= F1, RITORNA ERRORE
            if (F2 <= F1)
                return false;

            // CONVERTE LE FREQUENZE IN TERMINI DI FTW
            // ERRORE SE OUT OF RANGE
            // DETERMINA ORA IL VALORE DELL' FTW
            F1 = F1 / AD9954_CLOCK * 65536.0 * 65536.0;
            F2 = F2 / AD9954_CLOCK * 65536.0 * 65536.0;
            if (F1 > 65536.0 * 32768.0 || F2 > 65536.0 * 32768.0)
                return false;

            // DETERMINA IL RAMP RATE PER IL TEMPO PREIMPOSTATO
            // CONSIDERANDO 1024 STEPS
            RampRate = (Time * AD9954_SYNC_CLK / 1024);

            // CONTROLLA SE IL RAMP RATE E' PLAUSIBILE
            if (RampRate < 1.0 || RampRate > 65535.0)
                return false;

            // PROGRAMMA IL CONTROL WORD DEL PROFILO 0
            //  AD9954_WriteRSCW(0, AD9954_RAM_RAMP_UP, 0, 1023, RampRate, FALSE) ;
            AD9954_WriteRSCW(0, AD9954_RAM_RAMP_UP, 0, 1023, (ushort)RampRate, true);

            // PROGRAMMA LA RAM - INIZIO SCRITTURA
            AD9954_StartRamWrite();

            // SCRIVE ORA IN SEQUENZA INVERSA TUTTI I VALORI DI RAMPA
            if (SweepMode == AD9954_SWEEP_LOG)
            {
                for (i = 1023; i >= 1; i--)
                    AD9954_WriteRamLong((ulong)(F1 + (F2 - F1) * Math.Pow(ExpBase, i - 1023)));

                // SCRIVE LA FREQUENZA INIZIALE
                AD9954_WriteRamLong((ulong)F1);
            }
            else
            {
                for (i = 1023; i >= 0; i--)
                    AD9954_WriteRamLong((ulong)(F1 + (F2 - F1) / 1023 * i));
            }

            // ABILITA LA MODALITA' RAM
            cfr1 = AD9954_ReadReg(AD9954_CFR1, AD9954_CFR1_SIZE);
            cfr1 &= ~(AD9954_CFR1_RAM_ENABLED | AD9954_CFR1_RAM_DEST_PHASE);
            cfr1 |= AD9954_CFR1_RAM_ENABLED;
            AD9954_WriteReg(AD9954_CFR1, AD9954_CFR1_SIZE, cfr1);

            return true;
        }

        public void AD9954_SweepCycle()
        {
            AD9954_IoUpdate();
        }

        public void AD9954_SweepStop()
        {
            ulong cfr1;

            // DISABILITA LA MODALITA' RAM
            cfr1 = AD9954_ReadReg(AD9954_CFR1, AD9954_CFR1_SIZE);
            cfr1 &= ~(AD9954_CFR1_RAM_ENABLED | AD9954_CFR1_RAM_DEST_PHASE);
            AD9954_WriteReg(AD9954_CFR1, AD9954_CFR1_SIZE, cfr1);

            // ESEGUE UN IO/UPDATE PER AGGIORNARE I REGISTRI
            AD9954_IoUpdate();
        }

        //definizioni oggetti
        private SpiDevice DDS;
        private SpiDevice DAC;
        private GpioPin IO_Sync;
        private GpioPin IO_Update;
        private GpioPin Reset;
    }
}
