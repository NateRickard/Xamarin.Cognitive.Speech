using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Diagnostics;
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
		/// Gets or sets the profanity mode.
		/// </summary>
		/// <value>The profanity mode.</value>
		/// <remarks>You can control how the service handles profanity by setting the profanity mode.  
		/// https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#profanity-handling-in-speech-recognition</remarks>
		public ProfanityMode ProfanityMode { get; set; }


		/// <summary>
		/// Gets or sets the API version.
		/// </summary>
		/// <value>The API version. Defaults to "v1"</value>
		public string ApiVersion { get; set; } = "v1";


		public BingSpeechApiClient (string subscriptionKey)
		{
			AuthClient = new AuthenticationClient (subscriptionKey);
		}


		HttpRequestMessage CreateRequest (string audioFilePath, OutputMode outputMode)
		{
			var uriBuilder = new UriBuilder ("https",
											 Constants.Endpoints.BingSpeechApi.Host,
											 Constants.Endpoints.BingSpeechApi.Port,
											 Constants.Endpoints.BingSpeechApi.Path);
			uriBuilder.Path += $"/{RecognitionMode.ToString ().ToLower ()}/cognitiveservices/{ApiVersion}";
			uriBuilder.Query = $"language={RecognitionLanguage}&format={outputMode.ToString ().ToLower ()}&profanity={ProfanityMode.ToString ().ToLower ()}";

			Debug.WriteLine ($"Request Uri: {uriBuilder.Uri}");

			try
			{
				var request = new HttpRequestMessage (HttpMethod.Post, uriBuilder.Uri);

				request.Headers.TransferEncodingChunked = true;
				request.Headers.ExpectContinue = true;
				request.Headers.Authorization = new AuthenticationHeaderValue (Constants.Keys.Bearer, AuthClient.Token);
				request.Headers.Accept.ParseAdd (Constants.MimeTypes.Json);
				request.Headers.Accept.ParseAdd (Constants.MimeTypes.Xml);
				request.Headers.Host = Constants.Endpoints.BingSpeechApi.Host;

				request.Content = new PushStreamContent (async (outputStream, httpContext, transportContext) =>
				{
					try
					{
						byte [] buffer = null;
						int bytesRead = 0;

						var root = FileSystem.Current.LocalStorage;
						var file = await root.GetFileAsync (audioFilePath);

						using (outputStream)
						using (var audioStream = await file.OpenAsync (FileAccess.Read))
						{
							//read 1024 raw bytes from the input audio file.
							buffer = new Byte [checked((uint) Math.Min (1024, (int) audioStream.Length))];

							while ((bytesRead = audioStream.Read (buffer, 0, buffer.Length)) != 0)
							{
								await outputStream.WriteAsync (buffer, 0, bytesRead);
							}

							await outputStream.FlushAsync ();
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine (ex);
					}
				}, new MediaTypeHeaderValue (Constants.MimeTypes.WavAudio));

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
						Debug.WriteLine ($"sendRequest returned {response.StatusCode}");

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


		/// <summary>
		/// Returns Speech to Text results for the given audio input.  Begins sending the audio stream to the server immediately.
		/// </summary>
		/// <returns>Simple Speech to Text results, which is a single result for the given speech input.</returns>
		/// <param name="audioFilePath">Audio file path.</param>
		/// <remarks>More info here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public async Task<RecognitionSpeechResult> SpeechToTextSimple (string audioFilePath)
		{
			await AuthClient.Authenticate ();

			try
			{
				var request = CreateRequest (audioFilePath, OutputMode.Simple);
				var response = await SendRequest (request);

				var result = JsonConvert.DeserializeObject<RecognitionSpeechResult> (response);

				return result;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}


		/// <summary>
		/// Returns Speech to Text results for the given audio input.
		/// </summary>
		/// <returns>Detailed Speech to Text results, including the N best results for the given speech input.</returns>
		/// <param name="audioFilePath">Audio file path.</param>
		/// <remarks>More info here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public async Task<RecognitionResult> SpeechToTextDetailed (string audioFilePath)
		{
			await AuthClient.Authenticate ();

			try
			{
				var request = CreateRequest (audioFilePath, OutputMode.Detailed);
				var response = await SendRequest (request);

				var result = JsonConvert.DeserializeObject<RecognitionResult> (response);

				return result;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}
	}
}