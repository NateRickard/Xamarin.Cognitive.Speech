using System.Collections.Generic;
using Newtonsoft.Json;

namespace Xamarin.Cognitive.BingSpeech.Model
{
	/// <summary>
	/// Recognition result.
	/// </summary>
	public class RecognitionResult
	{
		/// <summary>
		/// A string indicating the result status.  Successful requests will return "Success"
		/// </summary>
		public RecognitionStatus RecognitionStatus { get; set; }


		/// <summary>
		/// Gets or sets the offset.  
		/// The Offset element specifies the offset (in 100-nanosecond units) at which the phrase was recognized, relative to the start of the audio stream.
		/// </summary>
		public long Offset { get; set; }


		/// <summary>
		/// The duration of speech.  
		/// The Duration element specifies the duration (in 100-nanosecond units) of this speech phrase.
		/// </summary>
		public long Duration { get; set; }


		/// <summary>
		/// A list of <see cref="SpeechResult"/> containing the N-best results (top result + any alternatives), ordered by their confidence.
		/// </summary>
		[JsonProperty ("NBest")]
		public List<SpeechResult> Results { get; set; }
	}
}