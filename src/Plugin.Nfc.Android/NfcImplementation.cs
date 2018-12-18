using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Plugin.Nfc.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Nfc
{
	internal class NfcImplementation : Java.Lang.Object, INfc, NfcAdapter.IReaderCallback
	{
		private readonly NfcAdapter _nfcAdapter;

		public event TagDetectedDelegate TagDetected;

		public NfcImplementation()
		{
			if (Build.VERSION.SdkInt < BuildVersionCodes.Kitkat)
			{
				return;
			}

			_nfcAdapter = NfcAdapter.GetDefaultAdapter(CrossNfc.CurrentActivity);
		}

		public Task<bool> IsAvailableAsync()
		{
			var context = Application.Context;
			var result = false;

			if (context.CheckCallingOrSelfPermission(Manifest.Permission.Nfc) != Permission.Granted)
			{
				result = false;
			}
			else
			{
				result = _nfcAdapter != null;
			}

			return Task.FromResult(result);
		}

		public Task<bool> IsEnabledAsync() => Task.FromResult(_nfcAdapter?.IsEnabled ?? false);

		public async Task StartListeningAsync()
		{
			if (!await IsAvailableAsync())
			{
				throw new InvalidOperationException("NFC not available");
			}

			if (!await IsEnabledAsync()) // todo: offer possibility to open dialog
			{
				throw new InvalidOperationException("NFC is not enabled");
			}

			var activity = CrossNfc.CurrentActivity;
			var tagDetected = new IntentFilter(NfcAdapter.ActionNdefDiscovered);

			tagDetected.AddDataType("*/*");

			var filters = new[] { tagDetected };
			var intent = new Intent(activity, activity.GetType()).AddFlags(ActivityFlags.SingleTop);
			var pendingIntent = PendingIntent.GetActivity(activity, 0, intent, 0);

			_nfcAdapter.EnableForegroundDispatch(activity, pendingIntent, filters, new[] { new[] { Java.Lang.Class.FromType(typeof(Ndef)).Name } });
			_nfcAdapter.EnableReaderMode(activity, this, NfcReaderFlags.NfcA | NfcReaderFlags.NoPlatformSounds, null);
		}

		public Task StopListeningAsync() => Task.Run(() =>
		{
			_nfcAdapter?.DisableReaderMode(CrossNfc.CurrentActivity);
			_nfcAdapter?.DisableForegroundDispatch(CrossNfc.CurrentActivity);
		});

		internal void CheckForNfcMessage(Intent intent)
		{
			if (intent.Action != NfcAdapter.ActionTagDiscovered)
			{
				return;
			}

			if (!(intent.GetParcelableExtra(NfcAdapter.ExtraTag) is Tag tag))
			{
				return;
			}

			var nativeMessages = intent.GetParcelableArrayExtra(NfcAdapter.ExtraNdefMessages);
			if (nativeMessages == null)
			{
				return;
			}

			OnTagDiscovered(tag);
		}

		public void OnTagDiscovered(Tag tag)
		{
			try
			{
				var techs = tag.GetTechList();

				if (!techs.Contains(Java.Lang.Class.FromType(typeof(Ndef)).Name))
				{
					return;
				}

				var ndef = Ndef.Get(tag);

				ndef.Connect();

				var ndefMessage = ndef.NdefMessage;
				var records = ndefMessage.GetRecords();

				ndef.Close();

				var nfcTag = new NfcDefTag(ndef, records);

				TagDetected?.Invoke(nfcTag);
			}
			catch (Exception ex)
			{
				// handle errors
			}
		}
	}

	public class NfcDefTag : INfcDefTag
	{
		public bool IsWriteable { get; }
		public NfcDefRecord[] Records { get; }

		public NfcDefTag(Ndef tag, IEnumerable<NdefRecord> records)
		{
			IsWriteable = tag.IsWritable;
			Records = records.Select(r => new AndroidNdefRecord(r)).ToArray();
		}
	}

	public class AndroidNdefRecord : NfcDefRecord
	{
		public AndroidNdefRecord(NdefRecord nativeRecord)
		{
			TypeNameFormat = GetTypeNameFormat(nativeRecord.Tnf);
			Payload = nativeRecord.GetPayload();
		}

		private NDefTypeNameFormat GetTypeNameFormat(short nativeRecordTnf)
		{
			switch (nativeRecordTnf)
			{
				case NdefRecord.TnfAbsoluteUri:
					return NDefTypeNameFormat.AbsoluteUri;
				case NdefRecord.TnfEmpty:
					return NDefTypeNameFormat.Empty;
				case NdefRecord.TnfExternalType:
					return NDefTypeNameFormat.External;
				case NdefRecord.TnfMimeMedia:
					return NDefTypeNameFormat.Media;
				case NdefRecord.TnfUnchanged:
					return NDefTypeNameFormat.Unchanged;
				case NdefRecord.TnfUnknown:
					return NDefTypeNameFormat.Unknown;
				case NdefRecord.TnfWellKnown:
					return NDefTypeNameFormat.WellKnown;
			}

			return NDefTypeNameFormat.Unknown;
		}
	}
}