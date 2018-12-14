using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreFoundation;
using CoreNFC;
using Foundation;
using Plugin.Nfc.Abstractions;

namespace Plugin.Nfc
{
    public class NfcImplementation : NSObject, INfc, INFCNdefReaderSessionDelegate
    {
        private NFCNdefReaderSession _session;
        public event TagDetectedDelegate TagDetected;
        public async Task<bool> IsAvailableAsync()
        {
            return NFCNdefReaderSession.ReadingAvailable;
        }

        public Task<bool> IsEnabledAsync()
        {
            return Task.FromResult(true);
        }

        public async Task StartListeningAsync()
        {
            var reader = new NfcReader();
            var message = await reader.ScanAsync();

        }

        public async Task StopListeningAsync()
        {
            _session.InvalidateSession();
        }

        public void DidInvalidate(NFCNdefReaderSession session, NSError error)
        {
        }

        public void DidDetect(NFCNdefReaderSession session, NFCNdefMessage[] messages)
        {
            var bytes = messages[0].Records[0].Payload.Skip(3).ToArray();
            var message = Encoding.UTF8.GetString(bytes);
        }
    }

    public class NfcReader : NSObject, INFCNdefReaderSessionDelegate
    {
        private NFCNdefReaderSession _session;
        private TaskCompletionSource<string> _tcs;

        public Task<string> ScanAsync()
        {
            if (!NFCNdefReaderSession.ReadingAvailable)
            {
                throw new InvalidOperationException("Reading NDEF is not available");
            }

            _tcs = new TaskCompletionSource<string>();
            _session = new NFCNdefReaderSession(this, DispatchQueue.CurrentQueue, true);
            _session.BeginSession();
            
            return _tcs.Task;
        }

        public void DidInvalidate(NFCNdefReaderSession session, NSError error)
        {
            _tcs.TrySetException(new Exception(error?.LocalizedFailureReason));
        }

        public void DidDetect(NFCNdefReaderSession session, NFCNdefMessage[] messages)
        {
            var bytes = messages[0].Records[0].Payload.Skip(3).ToArray();
            var message = Encoding.UTF8.GetString(bytes);
            _tcs.SetResult(message);
        }
    }
}