namespace Xamarin.Cognitive.BingSpeech.Model
{
	/// <summary>
	/// A single speech result combining Recogniton result with Speech result.  This is used for Simple result mode.
	/// </summary>
	public class RecognitionSpeechResult
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
		/// The top result (by confidence), returned in Display Form.
		/// </summary>
		/// <remarks>The display form adds punctuation and capitalization to recognition results, making it the most appropriate form for applications that display the spoken text.</remarks>
		public string DisplayText { get; set; }
	}
}