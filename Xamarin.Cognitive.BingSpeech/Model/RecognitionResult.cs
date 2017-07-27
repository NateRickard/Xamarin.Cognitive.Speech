using System.Collections.Generic;

namespace Xamarin.Cognitive.BingSpeech.Model
{
	public class RecognitionResult
	{
		public string RecognitionStatus { get; set; }

		public string Offset { get; set; }

		public string Duration { get; set; }

		public List<SpeechResult> Results { get; set; }
	}
}