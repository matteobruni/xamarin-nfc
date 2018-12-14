using Plugin.Nfc;
using System;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Nfc.Sample
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}

		private bool _isStarted;

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

			CrossNfc.Current.TagDetected += TagDetected;

			//await CrossNfc.Current.StartListeningAsync();
			lblStatus.Text = "Contact a NFC tag to read it.";
		}

		private void TagDetected(Plugin.Nfc.Abstractions.INfcDefTag tag)
		{
			var tagText = string.Join(", ", tag.Records.Select(t => Encoding.UTF8.GetString(t.Payload, 0, t.Payload.Length)));

			Device.BeginInvokeOnMainThread(() =>
			{
				lblStatus.Text = tagText;
			});
		}


		protected override async void OnDisappearing()
		{
			base.OnDisappearing();
			await CrossNfc.Current.StopListeningAsync();
			_isStarted = false;
			StartButton.Text = "Start Scan";
		}

		private async void Button_OnClicked(object sender, EventArgs e)
		{
			if (!_isStarted)
			{
				await CrossNfc.Current.StartListeningAsync();
				_isStarted = true;
				StartButton.Text = "Stop Scan";
			}
			else
			{
				await CrossNfc.Current.StopListeningAsync();
				_isStarted = false;
				StartButton.Text = "Start Scan";
			}
		}
	}
}
