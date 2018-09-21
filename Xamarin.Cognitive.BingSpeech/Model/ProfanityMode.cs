namespace Xamarin.Cognitive.BingSpeech
{
	/// <summary>
	/// ProfanityMode defines the possible modes in which the service can handle profanity.
	/// </summary>
	public enum ProfanityMode
	{
		/// <summary>
		/// The service masks profanity.  This is the default.
		/// </summary>
		Masked,
		/// <summary>
		/// The service removes profanity.
		/// </summary>
		Removed,
		/// <summary>
		/// The service does not remove or mask profanity.
		/// </summary>
		Raw
	}
}