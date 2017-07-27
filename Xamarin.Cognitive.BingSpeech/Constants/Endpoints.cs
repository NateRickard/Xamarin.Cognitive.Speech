namespace Xamarin.Cognitive.BingSpeech
{
	static partial class Constants
	{
		internal static class Endpoints
		{
			internal const string AuthApi = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";

			internal static class BingSpeechApi
			{
				internal const string Host = "speech.platform.bing.com";

				internal const int Port = 443;

				internal const string Path = "/speech/recognition/";
			}
		}
	}
}