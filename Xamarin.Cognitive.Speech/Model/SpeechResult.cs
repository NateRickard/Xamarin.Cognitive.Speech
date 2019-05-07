namespace Xamarin.Cognitive.Speech
{
	/// <summary>
	/// A Speech result that is part of a detailed resultset.
	/// </summary>
	public class SpeechResult
	{
		/// <summary>
		/// Confidence scores range from 0 to 1. A score of 1 represents the highest level of confidence. A score of 0 represents the lowest level of confidence.
		/// </summary>
		public float Confidence { get; set; }

		/// <summary>
		/// The lexical form is the recognized text, exactly how it occurred in the utterance and without punctuation or capitalization.
		/// </summary>
		public string Lexical { get; set; }

		/// <summary>
		/// The ITN form of a recognition result does not include capitalization or punctuation. 
		/// The ITN form is most appropriate for applications that act on the recognized text.
		/// </summary>
		public string ITN { get; set; }

		/// <summary>
		/// The masked ITN form applies profanity masking to the inverse text normalization form.
		/// </summary>
		public string MaskedITN { get; set; }

		/// <summary>
		/// The display form adds punctuation and capitalization to recognition results, making it the most appropriate form for applications that display the spoken text.
		/// </summary>
		public string Display { get; set; }
	}
}