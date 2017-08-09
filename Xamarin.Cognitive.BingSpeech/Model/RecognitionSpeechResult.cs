namespace Xamarin.Cognitive.BingSpeech.Model
{
	/// <summary>
	/// A single result combining Recogniton result with Speech result.  This is used for Simple result mode.
	/// </summary>
	public class RecognitionSpeechResult
	{
		public string RecognitionStatus { get; set; }

		public string Offset { get; set; }

		public string Duration { get; set; }

		public string DisplayText { get; set; }
	}
}