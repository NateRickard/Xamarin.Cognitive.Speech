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
		public RecognitionStatus RecognitionStatus { get; set; }

		/// <summary>
		/// Gets or sets the offset.  
		/// The Offset element specifies the offset (in 100-nanosecond units) at which the phrase was recognized, relative to the start of the audio stream
		/// </summary>
		public long Offset { get; set; }


		/// <summary>
		/// The duration of speech.  
		/// The Duration element specifies the duration (in 100-nanosecond units) of this speech phrase.
		/// </summary>
		public long Duration { get; set; }


		/// <summary>
		/// The top result (by confidence), returned in Display Form.
		/// </summary>
		/// <remarks>The display form adds punctuation and capitalization to recognition results, making it the most appropriate form for applications that display the spoken text.</remarks>
		public string DisplayText { get; set; }
	}
}