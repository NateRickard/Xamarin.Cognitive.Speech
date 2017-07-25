using System;
using System.Net;

namespace Xamarin.Cognitive.BingSpeech
{
	public static class ExceptionExtensions
	{
		public static bool HasWebResponseStatus (this Exception ex, HttpStatusCode code)
		{
			if (ex is WebException webEx)
			{
				if (webEx.Response is HttpWebResponse response)
				{
					return response.StatusCode == code;
				}
			}

			return false;
		}
	}
}