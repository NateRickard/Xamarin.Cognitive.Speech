using System.Collections.Generic;
using Newtonsoft.Json;

namespace Xamarin.Cognitive.BingSpeech.Model
{
	public class RecognitionResult
	{
		public string RecognitionStatus { get; set; }

		public string Offset { get; set; }

		public string Duration { get; set; }

		[JsonProperty ("NBest")]
		public List<SpeechResult> Results { get; set; }
	}
}