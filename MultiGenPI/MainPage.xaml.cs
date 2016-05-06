using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
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
        AD_DAC ADDAC = new AD_DAC();

        string Freq_Hz = "Hz";
        string Freq_KHz = "kHz";
        string Freq_MHz = "MHz";
        string Amp_Vpp = "Vpp";
        string Amp_mVpp = "mVpp";
        string Fase_Gradi = "°";

        public MainPage()
        {
            this.InitializeComponent();
            frequency_comboBox.Items.Insert(0, Freq_Hz);
            frequency_comboBox.Items.Insert(1, Freq_KHz);
            frequency_comboBox.Items.Insert(2, Freq_MHz);
            amplitude_comboBox.Items.Insert(0, Amp_Vpp);
            amplitude_comboBox.Items.Insert(1, Amp_mVpp);
            phase_comboBox.Items.Insert(0, Fase_Gradi);
            frequency_comboBox.SelectedIndex = 0;
            amplitude_comboBox.SelectedIndex = 1;
            phase_comboBox.SelectedIndex = 0;
        }

        private void frequency_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("Cabio ComboBox");
        }
    }

    
}
