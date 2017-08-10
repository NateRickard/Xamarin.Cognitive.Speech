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
		public string RecognitionStatus { get; set; }


		/// <summary>
		/// Gets or sets the offset.
		/// </summary>
		public string Offset { get; set; }


		/// <summary>
		/// The duration of speech.
		/// </summary>
		public string Duration { get; set; }


		/// <summary>
		/// A list of <see cref="SpeechResult"/> containing the N-best results (top result + any alternatives), ordered by their confidence.
		/// </summary>
		[JsonProperty ("NBest")]
		public List<SpeechResult> Results { get; set; }
	}
}