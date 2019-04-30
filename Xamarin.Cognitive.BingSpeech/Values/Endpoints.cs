namespace Xamarin.Cognitive.BingSpeech
{
	static class Endpoints
	{
		public static Endpoint Authentication = new Endpoint("api.cognitive.microsoft.com", "/sts/v1.0/issueToken");

		public static Endpoint SpeechServiceApi = new Endpoint("stt.speech.microsoft.com", "/speech/recognition");
	}
}