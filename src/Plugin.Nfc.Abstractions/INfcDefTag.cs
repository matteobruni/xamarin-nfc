namespace Plugin.Nfc.Abstractions
{
	public interface INfcDefTag
	{
		bool IsWriteable { get; }
		NfcDefRecord[] Records { get; }
	}
}
