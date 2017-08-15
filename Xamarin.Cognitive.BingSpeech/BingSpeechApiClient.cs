using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Diagnostics;
using PCLStorage;
using System.Net.Http;
using System.Net.Http.Headers;
using Xamarin.Cognitive.BingSpeech.Model;
using System.IO;
using System.Text;

namespace Xamarin.Cognitive.BingSpeech
{
	public class BingSpeechApiClient
	{
		const int BufferSize = 1024;
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


		HttpRequestMessage CreateRequest (OutputMode outputMode)
		{
			try
			{
				var uriBuilder = new UriBuilder ("https",
												 Constants.Endpoints.BingSpeechApi.Host,
												 Constants.Endpoints.BingSpeechApi.Port,
												 Constants.Endpoints.BingSpeechApi.Path);
				uriBuilder.Path += $"/{RecognitionMode.ToString ().ToLower ()}/cognitiveservices/{ApiVersion}";
				uriBuilder.Query = $"language={RecognitionLanguage}&format={outputMode.ToString ().ToLower ()}&profanity={ProfanityMode.ToString ().ToLower ()}";

				Debug.WriteLine ($"Request Uri: {uriBuilder.Uri}");

				var request = new HttpRequestMessage (HttpMethod.Post, uriBuilder.Uri);

				request.Headers.TransferEncodingChunked = true;
				request.Headers.ExpectContinue = true;
				request.Headers.Authorization = new AuthenticationHeaderValue (Constants.Keys.Bearer, AuthClient.Token);
				request.Headers.Accept.ParseAdd (Constants.MimeTypes.Json);
				request.Headers.Accept.ParseAdd (Constants.MimeTypes.Xml);
				request.Headers.Host = Constants.Endpoints.BingSpeechApi.Host;

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


		void PopulateRequestContent (HttpRequestMessage request, string audioFilePath)
		{
			request.Content = new PushStreamContent (async (outputStream, httpContext, transportContext) =>
			{
				try
				{
					byte [] buffer = null;
					int bytesRead = 0;

					var root = FileSystem.Current.LocalStorage;
					var file = await root.GetFileAsync (audioFilePath);

					using (outputStream) //must close/dispose output stream to notify that content is done
					using (var audioStream = await file.OpenAsync (FileAccess.Read))
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


		void WriteWaveHeader (Stream stream, int channelCount, int sampleRate, int bitsPerSample)
		{
			using (var writer = new BinaryWriter (stream, Encoding.UTF8))
			{
				writer.Seek (0, SeekOrigin.Begin);

				//chunk ID
				writer.Write ('R');
				writer.Write ('I');
				writer.Write ('F');
				writer.Write ('F');

				writer.Write (-1); // -1 - Unknown size

				//format
				writer.Write ('W');
				writer.Write ('A');
				writer.Write ('V');
				writer.Write ('E');

				//subchunk 1 ID
				writer.Write ('f');
				writer.Write ('m');
				writer.Write ('t');
				writer.Write (' ');

				writer.Write (16); //subchunk 1 (fmt) size
				writer.Write ((short) 1); //PCM audio format

				writer.Write ((short) channelCount);
				writer.Write (sampleRate);
				writer.Write (sampleRate * 2);
				writer.Write ((short) 2); //block align
				writer.Write ((short) bitsPerSample);

				//subchunk 2 ID
				writer.Write ('d');
				writer.Write ('a');
				writer.Write ('t');
				writer.Write ('a');

				//subchunk 2 (data) size
				writer.Write (-1); // -1 - Unknown size
			}
		}


		void PopulateRequestContent (HttpRequestMessage request, Stream audioStream, int channelCount, int sampleRate, int bitsPerSample)
		{
			request.Content = new PushStreamContent (async (outputStream, httpContext, transportContext) =>
			{
				try
				{
					byte [] buffer = null;
					int bytesRead = 0;

					using (outputStream) //must close/dispose output stream to notify that content is done
					{
						if (audioStream.CanRead)
						{
							WriteWaveHeader (outputStream, channelCount, sampleRate, bitsPerSample);

							Debug.WriteLine ("Creating buffer, audio stream length is {0}", audioStream.Length);

							if (audioStream.Length < BufferSize)
							{
								Debug.WriteLine ("No audio data detected, waiting 100 MS");
								await Task.Delay (100);
							}

							//read 1024 (BufferSize) (max) raw bytes from the input audio stream
							buffer = new Byte [checked((uint) Math.Min (BufferSize, (int) audioStream.Length))];

							while (audioStream.CanRead && (bytesRead = await audioStream.ReadAsync (buffer, 0, buffer.Length)) != 0)
							{
								//Debug.WriteLine ("Read {0} bytes from input stream, writing to output stream", bytesRead);

								await outputStream.WriteAsync (buffer, 0, bytesRead);

								//Debug.WriteLine ("Waiting 10 MS before next read");
								await Task.Delay (10);
							}

							await outputStream.FlushAsync ();

							Debug.WriteLine ("Wrote {0} bytes to outout stream", outputStream.Length);
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
			await AuthClient.Authenticate ();

			try
			{
				var request = CreateRequest (OutputMode.Simple);

				PopulateRequestContent (request, audioFilePath);

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
		/// <returns>Simple Speech to Text results, which is a single result for the given speech input.</returns>
		/// <param name="audioStream">Audio stream containing the speech.</param>
		/// <param name="channelCount">The number of channels in the audio stream.</param>
		/// <param name="sampleRate">The sample rate of the audio stream.</param>
		/// <param name="bitsPerSample">The bits per sample of the audio stream.</param>
		/// <remarks>More info here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public async Task<RecognitionSpeechResult> SpeechToTextSimple (Stream audioStream, int channelCount, int sampleRate, int bitsPerSample)
		{
			await AuthClient.Authenticate ();

			try
			{
				var request = CreateRequest (OutputMode.Simple);

				PopulateRequestContent (request, audioStream, channelCount, sampleRate, bitsPerSample);

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
				var request = CreateRequest (OutputMode.Detailed);

				PopulateRequestContent (request, audioFilePath);

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


		/// <summary>
		/// Returns Speech to Text results for the given audio input.
		/// </summary>
		/// <returns>Detailed Speech to Text results, including the N best results for the given speech input.</returns>
		/// <param name="audioStream">Audio stream containing the speech.</param>
		/// <param name="channelCount">The number of channels in the audio stream.</param>
		/// <param name="sampleRate">The sample rate of the audio stream.</param>
		/// <param name="bitsPerSample">The bits per sample of the audio stream.</param>
		/// <remarks>More info here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format</remarks>
		public async Task<RecognitionResult> SpeechToTextDetailed (Stream audioStream, int channelCount, int sampleRate, int bitsPerSample)
		{
			await AuthClient.Authenticate ();

			try
			{
				var request = CreateRequest (OutputMode.Detailed);

				PopulateRequestContent (request, audioStream, channelCount, sampleRate, bitsPerSample);

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