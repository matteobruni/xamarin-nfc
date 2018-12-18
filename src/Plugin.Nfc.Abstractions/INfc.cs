using System.Threading.Tasks;

namespace Plugin.Nfc.Abstractions
{
	public delegate void TagDetectedDelegate(INfcDefTag tag);

	public interface INfc
	{
		event TagDetectedDelegate TagDetected;

		/// <summary>
		/// Checks if <see cref="GetAvailabilityAsync"/> returns <see cref="FingerprintAvailability.Available"/>.
		/// </summary>
		/// <param name="allowAlternativeAuthentication">
		/// En-/Disables the use of the PIN / Passwort as fallback.
		/// Supported Platforms: iOS, Mac
		/// Default: false
		/// </param>
		/// <returns><c>true</c> if Available, else <c>false</c></returns>
		Task<bool> IsAvailableAsync();
		Task<bool> IsEnabledAsync();


		Task StartListeningAsync();
		Task StopListeningAsync();
	}
}
