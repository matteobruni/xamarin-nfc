using System;
using System.Threading.Tasks;
using Android.App;
using Android.Nfc;
using Plugin.Nfc.Abstractions;


namespace Plugin.Nfc
{
    internal class NfcImplementation : INfc
    {
        private readonly NfcAdapter _nfcAdapter;

        public NfcImplementation()
        {
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(Application.Context);
        }

        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(_nfcAdapter != null);
            
        }
    }
}