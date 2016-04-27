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
        AD_DAC ADDAC = new AD_DAC();

        public MainPage()
        {
            this.InitializeComponent();     
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //byte[] dds = new byte[3]{ 0xFF, 0x0F, 0xF0 };
            ADDAC.writeDDS(0x0FF0);
        }

        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            //byte[] dac = new byte[3] { 0xFF, 0x0F, 0xF0 };
            ADDAC.writeDAC(0x0F0F);
        }
    }

    
}
