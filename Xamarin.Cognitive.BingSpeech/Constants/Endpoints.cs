namespace Xamarin.Cognitive.BingSpeech
{
	static partial class Constants
	{
		internal static class Endpoints
		{
			internal static class Authentication
			{
				internal const string Host = "api.cognitive.microsoft.com";

				internal const int Port = 443;

				internal const string Path = "/sts/v1.0/issueToken";
			}


			internal static class BingSpeechApi
			{
				internal const string Host = "speech.platform.bing.com";

				internal const int Port = 443;

				internal const string Path = "/speech/recognition/";
			}
		}
	}
}