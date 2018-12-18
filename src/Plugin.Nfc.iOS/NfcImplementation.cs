using CoreFoundation;
using CoreNFC;
using Foundation;
using Plugin.Nfc.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Nfc
{
	public class NfcImplementation : NSObject, INfc
	{
		private NfcReader _reader;
		public event TagDetectedDelegate TagDetected;

		public Task<bool> IsAvailableAsync() => Task.FromResult(NFCNdefReaderSession.ReadingAvailable);

		public Task<bool> IsEnabledAsync() => Task.FromResult(true);

		public async Task StartListeningAsync()
		{
			_reader = new NfcReader();
			var messages = await _reader.ScanAsync();

			foreach (var message in messages)
			{
				TagDetected?.Invoke(new NfcDefTag(message));
			}

			await StopListeningAsync();
		}

		public async Task StopListeningAsync()
		{
			if (_reader != null)
			{
				await _reader.StopAsync();
				_reader = null;
			}
		}
	}

	public class NfcDefTag : INfcDefTag
	{
		public bool IsWriteable => false;
		public NfcDefRecord[] Records { get; }

		public NfcDefTag(NFCNdefMessage tag)
		{
			Records = tag.Records.Select(r => new AppleNdefRecord(r)).ToArray();
		}
	}

	public class AppleNdefRecord : NfcDefRecord
	{
		public AppleNdefRecord(NFCNdefPayload nativeRecord)
		{
			TypeNameFormat = GetTypeNameFormat(nativeRecord.TypeNameFormat);
			Payload = nativeRecord.Payload.ToArray();
		}

		private NDefTypeNameFormat GetTypeNameFormat(NFCTypeNameFormat nativeRecordTypeNameFormat)
		{
			switch (nativeRecordTypeNameFormat)
			{
				case NFCTypeNameFormat.AbsoluteUri:
					return NDefTypeNameFormat.AbsoluteUri;
				case NFCTypeNameFormat.Empty:
					return NDefTypeNameFormat.Empty;
				case NFCTypeNameFormat.NFCExternal:
					return NDefTypeNameFormat.External;
				case NFCTypeNameFormat.Media:
					return NDefTypeNameFormat.Media;
				case NFCTypeNameFormat.Unchanged:
					return NDefTypeNameFormat.Unchanged;
				case NFCTypeNameFormat.Unknown:
					return NDefTypeNameFormat.Unknown;
				case NFCTypeNameFormat.NFCWellKnown:
					return NDefTypeNameFormat.WellKnown;
			}

			return NDefTypeNameFormat.Unknown;
		}
	}

	public class NfcReader : NSObject, INFCNdefReaderSessionDelegate
	{
		private NFCNdefReaderSession _session;
		private TaskCompletionSource<NFCNdefMessage[]> _tcs;

		public Task<NFCNdefMessage[]> ScanAsync()
		{
			if (!NFCNdefReaderSession.ReadingAvailable)
			{
				throw new InvalidOperationException("Reading NDEF is not available");
			}

			_tcs = new TaskCompletionSource<NFCNdefMessage[]>();
			_session = new NFCNdefReaderSession(this, DispatchQueue.CurrentQueue, true);
			_session.BeginSession();

			return _tcs.Task;
		}

		public Task StopAsync() => Task.Run(() => _session.InvalidateSession());

		public void DidInvalidate(NFCNdefReaderSession session, NSError error) => _tcs.TrySetException(new Exception(error?.LocalizedFailureReason));

		public void DidDetect(NFCNdefReaderSession session, NFCNdefMessage[] messages) => _tcs.TrySetResult(messages);
	}
}