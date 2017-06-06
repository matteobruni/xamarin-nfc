using System;
using System.Threading.Tasks;

namespace Plugin.Nfc.Abstractions
{
    public interface INfc
    {
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


    }


    public enum NDefTypeNameFormat
    {
        AbsoluteUri,
        Empty,
        Media,
        External,
        WellKnown,
        Unchanged,
        Unknown
    }

    public class NDefMessage
    {
        public NDefRecord[] Records { get; set; }
    }

    public class NDefRecord
    {
        public NDefTypeNameFormat TypeNameFormat { get; set; }
    }
}
