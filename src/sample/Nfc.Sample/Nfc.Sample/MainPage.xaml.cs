using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Nfc;
using Xamarin.Forms;

namespace Nfc.Sample
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!await CrossNfc.Current.IsAvailableAsync())
            {
                lblStatus.Text = "NFC Reading not supported";
                return;
            }

            if (!await CrossNfc.Current.IsEnabledAsync())
            {
                lblStatus.Text = "NFC Reader not enabled. Please turn it on in the settings.";
                return;
            }

            //await CrossNfc.Current.StartListeningAsync();
            lblStatus.Text = "Contact a NFC tag to read it.";
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await CrossNfc.Current.StopListeningAsync();
        }
    }
}
