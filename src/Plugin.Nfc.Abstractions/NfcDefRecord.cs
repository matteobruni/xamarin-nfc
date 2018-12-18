namespace Plugin.Nfc.Abstractions
{
	public class NfcDefRecord
	{
		public NDefTypeNameFormat TypeNameFormat { get; set; }
		public byte[] Payload { get; set; }
	}
}
