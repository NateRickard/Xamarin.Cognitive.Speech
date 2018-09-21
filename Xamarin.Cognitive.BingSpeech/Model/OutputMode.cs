namespace Xamarin.Cognitive.BingSpeech
{
	/// <summary>
	/// Output mode.
	/// </summary>
	public enum OutputMode
	{
		/// <summary>
		/// A simplified phrase result containing the recognition status and the recognized text in display form.
		/// </summary>
		Simple,
		/// <summary>
		/// A recognition status and N-best list of phrase results where each phrase result contains all four recognition forms and a confidence score.
		/// </summary>
		Detailed
	}
}