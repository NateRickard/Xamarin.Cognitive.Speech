using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Xamarin.Cognitive.BingSpeech
{
	/// <summary>
	/// Recognition status.
	/// </summary>
	[JsonConverter (typeof (StringEnumConverter))]
	public enum RecognitionStatus
	{
		/// <summary>
		/// The recognition was successful and the DisplayText field will be present
		/// </summary>
		[EnumMember (Value = "Success")]
		Success,
		/// <summary>
		/// Speech was detected in the audio stream, but no words from the target language were matched. See below for more details
		/// </summary>
		[EnumMember (Value = "NoMatch")]
		NoMatch,
		/// <summary>
		/// The start of the audio stream contained only silence, and the service timed out waiting for speech
		/// </summary>
		[EnumMember (Value = "InitialSilenceTimeout")]
		InitialSilenceTimeout,
		/// <summary>
		/// The start of the audio stream contained only noise, and the service timed out waiting for speech
		/// </summary>
		[EnumMember (Value = "BabbleTimeout")]
		BabbleTimeout,
		/// <summary>
		/// The recognition service encountered an internal error and could not continue
		/// </summary>
		[EnumMember (Value = "Error")]
		Error
	}
}