using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Plugin.Nfc.Abstractions;

namespace Plugin.Nfc
{
    internal class NfcImplementation : Java.Lang.Object, INfc, NfcAdapter.IReaderCallback
    {
        private readonly List<NdefMessageWrite> _writeQueue = new List<NdefMessageWrite>();
        private readonly NfcAdapter _nfcAdapter;

        public event TagDetectedDelegate TagDetected;
        public event TagDetectedDelegate TagWritten;

        public NfcImplementation()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Kitkat)
                return;

            //if (Build.VERSION.SdkInt < BuildVersionCodes.Gingerbread)
            //    return;

            _nfcAdapter = NfcAdapter.GetDefaultAdapter(CrossNfc.CurrentActivity);
        }

        public async Task<bool> IsAvailableAsync()
        {
            var context = Application.Context;
            if (context.CheckCallingOrSelfPermission(Manifest.Permission.Nfc) != Permission.Granted)
                return false;

            return _nfcAdapter != null;
        }

        public Task<bool> IsEnabledAsync()
        {
            return Task.FromResult(_nfcAdapter?.IsEnabled ?? false);
        }

        public async Task StartListeningAsync()
        {
            if (!await IsAvailableAsync())
                throw new InvalidOperationException("NFC not available");

            if (!await IsEnabledAsync()) // todo: offer possibility to open dialog
                throw new InvalidOperationException("NFC is not enabled");

            var activity = CrossNfc.CurrentActivity;
            var tagDetected = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
            tagDetected.AddDataType("*/*");
            var filters = new[] { tagDetected };
            var intent = new Intent(activity, activity.GetType()).AddFlags(ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(activity, 0, intent, 0);
            _nfcAdapter.EnableForegroundDispatch(activity, pendingIntent, filters, new[] { new[] { Java.Lang.Class.FromType(typeof(Ndef)).Name } });
            //_nfcAdapter.EnableReaderMode(activity, this, NfcReaderFlags.NfcA | NfcReaderFlags.NoPlatformSounds, null);
        }

        public async Task StopListeningAsync()
        {
            //_nfcAdapter?.DisableReaderMode(CrossNfc.CurrentActivity);
            _nfcAdapter?.DisableForegroundDispatch(CrossNfc.CurrentActivity);
        }

        public Task WriteTagAsync(byte[] tagId, NfcDefRecord[] recordcs)
        {
            var oldItem = _writeQueue.FirstOrDefault(a => a.TagID.SequenceEqual(tagId));
            if (oldItem != null)
            {
                _writeQueue.Remove(oldItem);
            }

            var writeItem = new NdefMessageWrite() {TagID = tagId, Records = recordcs};
            _writeQueue.Add(writeItem);
            return writeItem.CompleteTask;
        }

        public Task WriteTagAsync(NfcDefRecord[] recordcs)
        {
            var oldItem = _writeQueue.FirstOrDefault(a => a.TagID == null);
            if (oldItem != null)
            {
                _writeQueue.Remove(oldItem);
            }

            var writeItem = new NdefMessageWrite() { TagID = null, Records = recordcs };
            _writeQueue.Add(writeItem);
            return writeItem.CompleteTask;
        }

        internal void CheckForNfcMessage(Intent intent)
        {
            //if (intent.Action != NfcAdapter.ActionTagDiscovered)
            //    return;

            //var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            //if (tag == null)
            //    return;

            //var nativeMessages = intent.GetParcelableArrayExtra(NfcAdapter.ExtraNdefMessages);
            //if (nativeMessages == null)
            //    return;

            //var messages = nativeMessages
            //    .Cast<NdefMessage>()
            //    .Select(m => new AndroidNdefMessage(m));
        }

        public void OnTagDiscovered(Tag tag)
        {
            try
            {
                var techs = tag.GetTechList();
                if (!techs.Contains(Java.Lang.Class.FromType(typeof(Ndef)).Name))
                    return;

                var ndef = Ndef.Get(tag);

                var writeItem = _writeQueue.FirstOrDefault(a => a.TagID.SequenceEqual(ndef.Tag.GetId())) ??
                                _writeQueue.FirstOrDefault(a => a.TagID == null);

                if (writeItem != null)
                {
                    ndef.Connect();
                    //TODO: there are some aguments for the ndefrecord missing
                    ndef.WriteNdefMessage(new NdefMessage(writeItem.Records.Select(a => new NdefRecord(a.Payload)).ToArray()));

                    var ndefMessage = ndef.NdefMessage;
                    var records = ndefMessage.GetRecords();
                    ndef.Close();
                    _writeQueue.Remove(writeItem);

                    var nfcTag = new NfcDefTag(ndef, records) {TagID = tag.GetId()};
                    TagWritten?.Invoke(nfcTag);
                }

                else
                {
                    ndef.Connect();
                    var ndefMessage = ndef.NdefMessage;
                    var records = ndefMessage.GetRecords();
                    ndef.Close();
                    var nfcTag = new NfcDefTag(ndef, records);
                    nfcTag.TagID = tag.GetId();
                    TagDetected?.Invoke(nfcTag);
                }
            }
            catch
            {
                // handle errors
            }
        }
    }

    public class NfcDefTag : INfcDefTag
    {
        public Ndef Tag { get; }
        public bool IsWriteable { get; }
        public NfcDefRecord[] Records { get; }
        public byte[] TagID { get; set; }

        public NfcDefTag(Ndef tag, IEnumerable<NdefRecord> records)
        {
            Tag = tag;
            IsWriteable = tag.IsWritable;
            Records = records
                .Select(r => new AndroidNdefRecord(r))
                .ToArray();
        }
    }

    class NdefMessageWrite
    {
        private TaskCompletionSource<bool> _tsc;

        public NdefMessageWrite()
        {
            _tsc = new TaskCompletionSource<bool>();
        }
        public byte[] TagID { get; set; }
        public NfcDefRecord[] Records { get; set; }
        public Task CompleteTask {get { return _tsc.Task; } }
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
                    return NDefTypeNameFormat.Unchanged;
                case NdefRecord.TnfWellKnown:
                    return NDefTypeNameFormat.WellKnown;
            }

            return NDefTypeNameFormat.Unknown;
        }
    }
}