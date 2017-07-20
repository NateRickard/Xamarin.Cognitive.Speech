using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using PCLStorage;
using System.Net.Http;
using System.Net.Http.Headers;
using Xamarin.Cognitive.BingSpeech.Model;

namespace Xamarin.Cognitive.BingSpeech
{
	public class BingSpeechApiClient
	{
		int retryCount;

		readonly AuthenticationClient authClient;


		public string Locale { get; set; } = "en-US";


		public BingSpeechApiClient (string subscriptionKey)
		{
			authClient = new AuthenticationClient (subscriptionKey);
		}


		async Task<HttpRequestMessage> CreateRequest (string audioFilePath)
		{
			StringBuilder requestUriBuilder = new StringBuilder (Constants.Endpoints.BingSpeechApi);

			requestUriBuilder.Append (@"?scenarios=smd");                               // websearch is the other main option.
			requestUriBuilder.Append (@"&appid=D4D52672-91D7-4C74-8AD8-42B1D98141A5");  // You must use this ID.
			requestUriBuilder.Append ($@"&locale={Locale}");                            // We support several other languages.  Refer to README file.
			requestUriBuilder.Append (@"&device.os=wp7");
			requestUriBuilder.Append (@"&version=3.0");
			requestUriBuilder.Append (@"&format=json");
			requestUriBuilder.Append (@"&instanceid=565D69FF-E928-4B7E-87DA-9A750B96D9E3");
			requestUriBuilder.AppendFormat (@"&requestid={0}", Guid.NewGuid ());

			var requestUri = requestUriBuilder.ToString ();

			Debug.WriteLine ("Request Uri: " + requestUri + Environment.NewLine);

			try
			{
				var request = new HttpRequestMessage (HttpMethod.Post, requestUri);

				request.Headers.TransferEncodingChunked = true;
				request.Headers.Authorization = new AuthenticationHeaderValue ("Bearer", authClient.Token);
				request.Headers.Accept.ParseAdd ("application/json");
				request.Headers.Accept.ParseAdd ("text/xml");

				var root = FileSystem.Current.LocalStorage;
				var file = await root.GetFileAsync (audioFilePath);
				var stream = await file.OpenAsync (FileAccess.Read);
				// we'll dispose the StreamContent later after it's sent

				request.Content = new StreamContent (stream);
				request.Content.Headers.ContentType = new MediaTypeHeaderValue ("audio/wav");

				return request;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}


		async Task<string> SendRequest (HttpRequestMessage request)
		{
			try
			{
				using (var client = new HttpClient ())
				{
					var response = await client.SendAsync (request);

					//if we get a valid response (non-null & no exception), then reset our retry count & return the response
					if (response != null)
					{
						retryCount = 0;
						Debug.WriteLine ($"sendRequest returned ${response.StatusCode}");

						return await response.Content.ReadAsStringAsync ();
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in sendRequest: {0}", ex.Message);

				//handle expired auth token
				if (ex.HasWebResponseStatus (HttpStatusCode.Forbidden) && retryCount < 1)
				{
					await authClient.Authenticate (true);
					retryCount++;

					return await SendRequest (request);
				}
			}
			finally
			{
				//release the underlying file stream
				request.Content?.Dispose ();
			}

			return null;
		}


		public async Task<SpeechResult> SpeechToText (string audioFilePath)
		{
			await authClient.Authenticate ();

			try
			{
				var request = await CreateRequest (audioFilePath);
				var response = await SendRequest (request);

				try
				{
					var root = JsonConvert.DeserializeObject<SpeechResults> (response);
					var result = root.Results? [0];

					return result;
				}
				catch (Exception e)
				{
					//looking for a specific e here...
					Debug.WriteLine (e.Message);
				}

				return null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}
	}
}