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

		/// <summary>
		/// Gets the auth client.
		/// </summary>
		/// <value>The auth client.</value>
		public AuthenticationClient AuthClient { get; private set; }


		/// <summary>
		/// Gets or sets the Recognition language.
		/// </summary>
		/// <remarks>Supported locales can be found here: 
		/// https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#recognition-language</remarks>
		public string RecognitionLanguage { get; set; } = "en-US";


		/// <summary>
		/// Gets or sets the recognition mode.
		/// </summary>
		/// <value>The recognition mode.</value>
		/// <remarks>There are three modes of recognition: interactive, conversation, and dictation. 
		/// The recognition mode adjusts speech recognition based on how the users are likely to speak. 
		/// Choose the appropriate recognition mode for your application.
		/// https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#recognition-modes</remarks>
		public RecognitionMode RecognitionMode { get; set; }


		/// <summary>
		/// Gets or sets the output mode.
		/// </summary>
		/// <value>The output mode.</value>
		/// <remarks>https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public OutputMode OutputMode { get; set; }


		/// <summary>
		/// Gets or sets the API version.
		/// </summary>
		/// <value>The API version. Defaults to "v1"</value>
		public string ApiVersion { get; set; } = "v1";


		public BingSpeechApiClient (string subscriptionKey)
		{
			AuthClient = new AuthenticationClient (subscriptionKey);
		}


		async Task<HttpRequestMessage> CreateRequest (string audioFilePath)
		{
			//var requestUriBuilder = new StringBuilder (Constants.Endpoints.BingSpeechApi);

			var uriBuilder = new UriBuilder ("https",
											 Constants.Endpoints.BingSpeechApi.Host,
											 Constants.Endpoints.BingSpeechApi.Port,
											 Constants.Endpoints.BingSpeechApi.Path);
			uriBuilder.Path += $"/{RecognitionMode.ToString ().ToLower ()}/cognitiveservices/{ApiVersion}";
			uriBuilder.Query = $"language={RecognitionLanguage}&format={OutputMode.ToString ().ToLower ()}";


			//requestUriBuilder.Append (@"?scenarios=smd");                               // websearch is the other main option.
			//requestUriBuilder.Append (@"&appid=D4D52672-91D7-4C74-8AD8-42B1D98141A5");  // You must use this ID.
			//requestUriBuilder.Append ($@"&locale={Locale}");                            // We support several other languages.  Refer to README file.
			//requestUriBuilder.Append (@"&device.os=wp7");
			//requestUriBuilder.Append (@"&version=3.0");
			//requestUriBuilder.Append (@"&format=json");
			//requestUriBuilder.Append (@"&instanceid=565D69FF-E928-4B7E-87DA-9A750B96D9E3");
			//requestUriBuilder.AppendFormat (@"&requestid={0}", Guid.NewGuid ());

			//var requestUri = requestUriBuilder.ToString ();

			Debug.WriteLine ($"Request Uri: {uriBuilder.Uri}");

			try
			{
				var request = new HttpRequestMessage (HttpMethod.Post, uriBuilder.Uri);

				request.Headers.TransferEncodingChunked = true;
				request.Headers.Authorization = new AuthenticationHeaderValue (Constants.Keys.Bearer, AuthClient.Token);
				request.Headers.Accept.ParseAdd (Constants.MimeTypes.Json);
				request.Headers.Accept.ParseAdd (Constants.MimeTypes.Xml);
				request.Headers.Host = Constants.Endpoints.BingSpeechApi.Host;

				var root = FileSystem.Current.LocalStorage;
				var file = await root.GetFileAsync (audioFilePath);
				var stream = await file.OpenAsync (FileAccess.Read);
				// we'll dispose the StreamContent later after it's sent

				request.Content = new StreamContent (stream);
				request.Content.Headers.ContentType = new MediaTypeHeaderValue (Constants.MimeTypes.WavAudio);

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
					await AuthClient.Authenticate (true);
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
			await AuthClient.Authenticate ();

			try
			{
				var request = await CreateRequest (audioFilePath);
				var response = await SendRequest (request);

				try
				{
					var root = JsonConvert.DeserializeObject<RecognitionResult> (response);
					var result = root.Results? [0];

					return result;
				}
				catch (Exception e)
				{
					//looking for a specific e here... ??
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