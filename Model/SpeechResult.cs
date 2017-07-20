using Newtonsoft.Json;

namespace Xamarin.Cognitive.BingSpeech.Model
{
	[JsonObject ("result")]
	public class SpeechResult
	{
		public string Scenario { get; set; }

		public string Name { get; set; }

		public string Lexical { get; set; }

		public float Confidence { get; set; }
	}
}