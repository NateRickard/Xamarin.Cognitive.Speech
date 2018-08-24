namespace Xamarin.Cognitive.BingSpeech
{
	/// <summary>
	/// The authentication types supported by the Speech API.
	/// </summary>
	/// <remarks>
	/// See https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication.
	/// </remarks>
	public enum AuthenticationMode
	{
		/// <summary>
		/// Authentication will be performed by calling to the authentication endpoint and grabbing a JWT token that is valid for 10 minutes.
		/// </summary>
		/// <remarks>
		/// https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication?tabs=CSharp#use-an-authorization-token
		/// </remarks>
		AuthorizationToken,
		/// <summary>
		/// Authentication will be performed by sending the subscription key in the Ocp-Apim-Subscription-Key header each time a request is made.
		/// </summary>
		/// <remarks>
		/// https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication?tabs=CSharp#use-a-subscription-key
		/// </remarks>
		SubscriptionKey
	}
}