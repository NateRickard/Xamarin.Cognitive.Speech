using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;

namespace Xamarin.Cognitive.BingSpeech
{
	/// <summary>
	/// Bing speech API client.
	/// </summary>
	public class BingSpeechApiClient
	{
		const int BufferSize = 1024;
		const int DefaultChannelCount = 1;
		const int DefaultBitsPerSample = 16;

		bool requestRetried;

		/// <summary>
		/// Gets the auth client.
		/// </summary>
		/// <value>The auth client.</value>
		AuthenticationClient AuthClient { get; set; }


		/// <summary>
		/// Gets or sets the endpoint used to talk to the Speech API.
		/// </summary>
		/// <value>The endpoint.</value>
		/// <remarks>To use a CRIS/Custom Speech Service endpoint, set this to a new <see cref="Endpoint"/> with the details for your CRIS service.</remarks>
		public Endpoint SpeechEndpoint { get; set; } = Endpoints.BingSpeechApi;


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


		/// <summary>
		/// Initializes a new instance of the <see cref="T:Xamarin.Cognitive.BingSpeech.BingSpeechApiClient"/> class.
		/// </summary>
		/// <param name="subscriptionKey">A valid subscription key for the Bing Speech API.</param>
		public BingSpeechApiClient (string subscriptionKey)
		{
			AuthClient = new AuthenticationClient (subscriptionKey);
		}


		/// <summary>
		/// Calls to the authentication endpoint to get a JWT token for authentication to the Bing Speech API.  Token is cached and valid for 9 minutes.
		/// </summary>
		/// <param name="forceNewToken">If set to <c>true</c>, force new token even if there is already a cached token.</param>
		/// <remarks>This is called automatically when calling any of the SpeechToText* methods.  Call this separately up front to decrease latency on the initial API call.</remarks>
		public async Task Authenticate (bool forceNewToken = false)
		{
			await AuthClient.Authenticate (forceNewToken);
		}


		HttpRequestMessage CreateRequest (OutputMode outputMode)
		{
			try
			{
				var uriBuilder = new UriBuilder (SpeechEndpoint.Protocol,
												 SpeechEndpoint.Host,
												 SpeechEndpoint.Port,
												 SpeechEndpoint.Path);
				uriBuilder.Path += $"/{RecognitionMode.ToString ().ToLower ()}/cognitiveservices/{ApiVersion}";
				uriBuilder.Query = $"language={RecognitionLanguage}&format={outputMode.ToString ().ToLower ()}&profanity={ProfanityMode.ToString ().ToLower ()}";

				Debug.WriteLine ($"{DateTime.Now} :: Request Uri: {uriBuilder.Uri}");

				var request = new HttpRequestMessage (HttpMethod.Post, uriBuilder.Uri);

				request.Headers.TransferEncodingChunked = true;
				request.Headers.ExpectContinue = true;
				request.Headers.Authorization = new AuthenticationHeaderValue (Constants.Keys.Bearer, AuthClient.Token);
				request.Headers.Accept.ParseAdd (Constants.MimeTypes.Json);
				request.Headers.Accept.ParseAdd (Constants.MimeTypes.Xml);
				request.Headers.Host = SpeechEndpoint.Host;

				return request;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}


		async Task<string> SendRequest (Func<HttpRequestMessage> requestFactory, Func<HttpContent> contentFactory)
		{
			try
			{
				using (var client = new HttpClient ())
				{
					var request = requestFactory ();
					request.Content = contentFactory ();

					var response = await client.SendAsync (request).ConfigureAwait (false);

					if (response != null)
					{
						Debug.WriteLine ($"SendRequest returned {response.StatusCode}");

						if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Continue)
						{
							//if we get a valid response (non-null, no exception, and not forbidden), then reset our requestRetried var & return the response
							requestRetried = false;

							return await response.Content.ReadAsStringAsync ();
						}

						//handle expired auth token
						if ((response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized) && !requestRetried)
						{
							//attempt to re-auth
							await AuthClient.Authenticate (true);
							requestRetried = true; //just so we don't retry endlessly...

							//re-create the request and copy the content, then send it again
							return await SendRequest (requestFactory, () => CopyRequestContent (request.Content));
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in sendRequest: {0}", ex);
				throw;
			}

			Debug.WriteLine ("SendRequest: Unable to send successful request, returning null response");

			return null;
		}


		HttpContent CopyRequestContent (HttpContent content)
		{
			return new PushStreamContent (async (outputStream, httpContext, transportContext) =>
			{
				try
				{
					byte [] buffer = null;
					int bytesRead = 0;

					using (outputStream) //must close/dispose output stream to notify that content is done
					{
						// this is not working for PushStreamContent?
						//await content.CopyToAsync (outputStream);

						//this is working - let's copy the content!
						using (var stream = await content.ReadAsStreamAsync ())
						{
							//read 1024 (BufferSize) (max) raw bytes from the input audio file
							buffer = new Byte [checked((uint) Math.Min (BufferSize, (int) stream.Length))];

							while ((bytesRead = await stream.ReadAsync (buffer, 0, buffer.Length)) != 0)
							{
								await outputStream.WriteAsync (buffer, 0, bytesRead);
							}

							await outputStream.FlushAsync ();
						}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine (ex);
					throw;
				}
			}, new MediaTypeHeaderValue (Constants.MimeTypes.WavAudio));
		}


		HttpContent PopulateRequestContent (string audioFilePath)
		{
			return new PushStreamContent (async (outputStream, httpContext, transportContext) =>
			{
				try
				{
					byte [] buffer = null;
					int bytesRead = 0;

					using (outputStream) //must close/dispose output stream to notify that content is done
					using (var audioStream = File.OpenRead (audioFilePath))
					{
						//read 1024 (BufferSize) (max) raw bytes from the input audio file
						buffer = new Byte [checked((uint) Math.Min (BufferSize, (int) audioStream.Length))];

						while ((bytesRead = await audioStream.ReadAsync (buffer, 0, buffer.Length)) != 0)
						{
							await outputStream.WriteAsync (buffer, 0, bytesRead);
						}

						await outputStream.FlushAsync ();
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine (ex);
					throw;
				}
			}, new MediaTypeHeaderValue (Constants.MimeTypes.WavAudio));
		}


		HttpContent PopulateRequestContent (Stream audioStream, int channelCount, int sampleRate, int bitsPerSample, Task recordingTask = null, int streamReadDelay = 30)
		{
			const int audioDataWaitInterval = 100; //ms
			const int maxReadRetries = 10; //times

			return new PushStreamContent (async (outputStream, httpContext, transportContext) =>
			{
				try
				{
					byte [] buffer = null;
					int bytesRead = 0;
					int readRetryCount = 0;

					using (outputStream) //must close/dispose output stream to notify that content is done
					{
						if (audioStream.CanRead)
						{
							var totalWait = 0;

							//wait up to (audioDataWaitInterval * maxReadRetries) for some data to populate
							while (audioStream.Length < BufferSize && totalWait < audioDataWaitInterval * maxReadRetries)
							{
								Debug.WriteLine ("No audio data detected, waiting 100 MS");
								await Task.Delay (audioDataWaitInterval);
								totalWait += audioDataWaitInterval;
							}

							//write a wav/riff header to the stream
							outputStream.WriteWaveHeader (channelCount, sampleRate, bitsPerSample);

							//read 1024 (BufferSize) (max) raw bytes from the input audio stream
							buffer = new Byte [checked((uint) Math.Min (BufferSize, (int) audioStream.Length))];

							//probably a better way to do this... but if the caller has passed a Task in for us to determine the end of recording, we'll use that to see if it's ongoing
							//	Otherwise, we'll always assume that the Stream is being populated and we'll fall back to using delays to attempt to wait for the end of stream
							var tcs = new TaskCompletionSource<bool> ();
							var waitTask = recordingTask ?? tcs.Task;

							// loop while the stream is being populated... attempt to read <buffer.Length> bytes per loop, and see if we should keep checking, either via Task or read retries (when 0 bytes read)
							while (audioStream.CanRead &&
								  ((bytesRead = await audioStream.ReadAsync (buffer, 0, buffer.Length)) != 0 || !waitTask.Wait (streamReadDelay)))
							{
								if (bytesRead > 0)
								{
									readRetryCount = -1;

									//write the bytes to the output stream
									await outputStream.WriteAsync (buffer, 0, bytesRead);
								}

								readRetryCount++;

								//again, only using read retry timeouts if we don't have a Task
								if (recordingTask == null && readRetryCount >= maxReadRetries)
								{
									tcs.SetResult (true);
								}
							}

							await outputStream.FlushAsync ();
						}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine (ex);
					throw;
				}
			}, new MediaTypeHeaderValue (Constants.MimeTypes.WavAudio));
		}


		/// <summary>
		/// Returns Speech to Text results for the given audio input.  Begins sending the audio stream to the server immediately.
		/// </summary>
		/// <returns>Simple Speech to Text results, which is a single result for the given speech input.</returns>
		/// <param name="audioFilePath">Audio file path.</param>
		/// <remarks>More info here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public async Task<RecognitionSpeechResult> SpeechToTextSimple (string audioFilePath)
		{
			try
			{
				if (!string.IsNullOrEmpty (audioFilePath))
				{
					await AuthClient.Authenticate ();

					var response = await SendRequest (
						() => CreateRequest (OutputMode.Simple),
						() => PopulateRequestContent (audioFilePath));

					if (response != null)
					{
						return JsonConvert.DeserializeObject<RecognitionSpeechResult> (response);
					}
				}

				return null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}


		/// <summary>
		/// Returns Speech to Text results for the given audio input.  Assumes single channel (mono) audio at 16 bits per sample.
		/// </summary>
		/// <returns>Simple Speech to Text results, which is a single result for the given speech input.</returns>
		/// <param name="audioStream">Audio stream containing the speech.</param>
		/// <param name="sampleRate">The sample rate of the audio stream.</param>
		/// <param name="recordingTask">A <see cref="Task"/> that will complete when recording is complete.</param>
		/// <remarks>More info here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public Task<RecognitionSpeechResult> SpeechToTextSimple (Stream audioStream, int sampleRate, Task recordingTask = null)
		{
			return SpeechToTextSimple (audioStream, DefaultChannelCount, sampleRate, DefaultBitsPerSample, recordingTask);
		}


		/// <summary>
		/// Returns Speech to Text results for the given audio input.
		/// </summary>
		/// <returns>Simple Speech to Text results, which is a single result for the given speech input.</returns>
		/// <param name="audioStream">Audio stream containing the speech.</param>
		/// <param name="channelCount">The number of channels in the audio stream.</param>
		/// <param name="sampleRate">The sample rate of the audio stream.</param>
		/// <param name="bitsPerSample">The bits per sample of the audio stream.</param>
		/// <param name="recordingTask">A <see cref="Task"/> that will complete when recording is complete.</param>
		/// <remarks>More info here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public async Task<RecognitionSpeechResult> SpeechToTextSimple (Stream audioStream, int channelCount, int sampleRate, int bitsPerSample, Task recordingTask = null)
		{
			try
			{
				await AuthClient.Authenticate ();

				var response = await SendRequest (
					() => CreateRequest (OutputMode.Simple),
					() => PopulateRequestContent (audioStream, channelCount, sampleRate, bitsPerSample, recordingTask));

				if (response != null)
				{
					return JsonConvert.DeserializeObject<RecognitionSpeechResult> (response);
				}

				return null;
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
			try
			{
				await AuthClient.Authenticate ();

				var response = await SendRequest (
					() => CreateRequest (OutputMode.Detailed),
					() => PopulateRequestContent (audioFilePath));

				if (response != null)
				{
					return JsonConvert.DeserializeObject<RecognitionResult> (response);
				}

				return null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}


		/// <summary>
		/// Returns Speech to Text results for the given audio input.  Assumes single channel (mono) audio at 16 bits per sample.
		/// </summary>
		/// <returns>Detailed Speech to Text results, including the N best results for the given speech input.</returns>
		/// <param name="audioStream">Audio stream containing the speech.</param>
		/// <param name="sampleRate">The sample rate of the audio stream.</param>
		/// <param name="recordingTask">A <see cref="Task"/> that will complete when recording is complete.</param>
		/// <remarks>More info here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public Task<RecognitionResult> SpeechToTextDetailed (Stream audioStream, int sampleRate, Task recordingTask = null)
		{
			return SpeechToTextDetailed (audioStream, DefaultChannelCount, sampleRate, DefaultBitsPerSample, recordingTask);
		}


		/// <summary>
		/// Returns Speech to Text results for the given audio input.
		/// </summary>
		/// <returns>Detailed Speech to Text results, including the N best results for the given speech input.</returns>
		/// <param name="audioStream">Audio stream containing the speech.</param>
		/// <param name="channelCount">The number of channels in the audio stream.</param>
		/// <param name="sampleRate">The sample rate of the audio stream.</param>
		/// <param name="bitsPerSample">The bits per sample of the audio stream.</param>
		/// <param name="recordingTask">A <see cref="Task"/> that will complete when recording is complete.</param>
		/// <remarks>More info here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public async Task<RecognitionResult> SpeechToTextDetailed (Stream audioStream, int channelCount, int sampleRate, int bitsPerSample, Task recordingTask = null)
		{
			try
			{
				await AuthClient.Authenticate ();

				var response = await SendRequest (
					() => CreateRequest (OutputMode.Detailed),
					() => PopulateRequestContent (audioStream, channelCount, sampleRate, bitsPerSample, recordingTask));

				if (response != null)
				{
					return JsonConvert.DeserializeObject<RecognitionResult> (response);
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