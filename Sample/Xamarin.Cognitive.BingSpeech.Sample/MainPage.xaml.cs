using Plugin.AudioRecorder;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xamarin.Cognitive.BingSpeech.Sample
{
	public partial class MainPage : ContentPage
	{
		AudioRecorderService recorder;
		BingSpeechApiClient speechClient;

		public Array AuthenticationModes => Enum.GetValues (typeof (AuthenticationMode));

		public AuthenticationMode AuthenticationMode { get; set; } = AuthenticationMode.AuthorizationToken;

		public Array RecognitionModes => Enum.GetValues (typeof (RecognitionMode));

		public RecognitionMode RecognitionMode { get; set; } = RecognitionMode.Interactive;

		public Array ProfanityModes => Enum.GetValues (typeof (ProfanityMode));

		public ProfanityMode ProfanityMode { get; set; } = ProfanityMode.Masked;

		public Array OutputModes => Enum.GetValues (typeof (OutputMode));

		public OutputMode OutputMode { get; set; } = OutputMode.Simple;

		public MainPage ()
		{
			InitializeComponent ();

			recorder = new AudioRecorderService
			{
				StopRecordingOnSilence = true,
				StopRecordingAfterTimeout = true,
				TotalAudioTimeout = TimeSpan.FromSeconds (15) // Speech REST API has 15 sec max
			};

			if (Keys.BingSpeech.SubscriptionKey == Keys.BingSpeech.BadSubscriptionKey)
			{
				throw new Exception ("Get a Bing Speech API key here: https://azure.microsoft.com/en-us/pricing/details/cognitive-services/speech-api/");
			}

			speechClient = new BingSpeechApiClient (Keys.BingSpeech.SubscriptionKey);

			//	if you need custom endpoint(s) you can do this:
			//speechClient.SpeechEndpoint = new Endpoint ("westus.stt.speech.microsoft.com", "/speech/recognition");
			//speechClient.AuthEndpoint = new Endpoint ("westus.api.cognitive.microsoft.com", "/sts/v1.0/issueToken");

			BindingContext = this;
		}


		private void AuthenticationModePicker_SelectedIndexChanged (object sender, EventArgs e)
		{
			speechClient.AuthenticationMode = AuthenticationMode;

			if (AuthenticationMode == AuthenticationMode.AuthorizationToken)
			{
				InitialAuthSection.IsVisible = true;
			}
			else
			{
				InitialAuthSection.IsVisible = false;
				speechClient.ClearAuthToken ();
			}
		}


		private void AuthSwitch_Toggled (object sender, ToggledEventArgs e)
		{
			if (AuthSwitch.IsToggled)
			{
				//go fetch an auth token up front - this can/should decrease latecy on the first call. 
				//	Otherwise, authentication would be performed transparently the first time the speech client is used.
				Task.Run (() => speechClient.AuthenticateWithToken ());
			}
			else
			{
				speechClient.ClearAuthToken ();
			}
		}


		async void Record_Clicked (object sender, EventArgs e)
		{
			await RecordAudio ();
			//await RecordAudioAlternate ();
		}


		void UpdateUI (bool buttonEnabled, bool spinnerEnabled = false)
		{
			UpdateUI (buttonEnabled, null, spinnerEnabled);
		}


		void UpdateUI (bool buttonEnabled, string buttonText, bool spinnerEnabled = false)
		{
			RecordButton.IsEnabled = buttonEnabled;

			if (buttonText != null)
			{
				RecordButton.Text = buttonText;
			}

			spinnerContent.IsVisible = spinnerEnabled;
			spinner.IsRunning = spinnerEnabled;
		}


		async Task RecordAudio ()
		{
			try
			{
				if (!recorder.IsRecording) //Record button clicked
				{
					UpdateUI (false);

					//start recording audio
					var audioRecordTask = await recorder.StartRecording ();

					UpdateUI (true, "Stop");

					//set the selected recognition mode & profanity mode
					speechClient.RecognitionMode = RecognitionMode;
					speechClient.ProfanityMode = ProfanityMode;

					//if we want to stream the audio as it's recording, we'll do that below
					if (StreamSwitch.IsToggled)
					{
						//does nothing more than turn the spinner on once recording is complete
						_ = audioRecordTask.ContinueWith ((audioFile) => UpdateUI (false, "Record", true), TaskScheduler.FromCurrentSynchronizationContext ());

						//do streaming speech to text
						var resultText = await SpeechToText (audioRecordTask);
						ResultsLabel.Text = resultText ?? "No Results!";

						UpdateUI (true, "Record", false);
					}
					else //waits for the audio file to finish recording before starting to send audio data to the server
					{
						var audioFile = await audioRecordTask;

						UpdateUI (true, "Record", true);

						//if we're not streaming the audio as we're recording, we'll use the file-based STT API here
						if (audioFile != null)
						{
							var resultText = await SpeechToText (audioFile);
							ResultsLabel.Text = resultText ?? "No Results!";
						}

						UpdateUI (true, false);
					}
				}
				else //Stop button clicked
				{
					UpdateUI (false, true);

					//stop the recording...
					await recorder.StopRecording ();
				}
			}
			catch (Exception ex)
			{
				ResultsLabel.Text = ProcessResult (ex);

				UpdateUI (true, "Record", false);
			}
		}


		//Hook up this alternate handler to try out the event-based API

		async Task RecordAudioAlternate ()
		{
			if (!recorder.IsRecording)
			{
				recorder.AudioInputReceived -= Recorder_AudioInputReceived;
				recorder.AudioInputReceived += Recorder_AudioInputReceived;

				UpdateUI (false);

				await recorder.StartRecording ();

				UpdateUI (true, "Stop");

				//set the selected recognition mode & profanity mode
				speechClient.RecognitionMode = RecognitionMode;
				speechClient.ProfanityMode = ProfanityMode;

				if (StreamSwitch.IsToggled)
				{
					throw new Exception ("Use RecordAudio() with the Stream API - this is older code");
				}
			}
			else //Stop button clicked
			{
				UpdateUI (false, true);

				//stop the recording... recorded audio will be used in the Recorder_AudioInputReceived handler below
				await recorder.StopRecording ();
			}
		}


		async void Recorder_AudioInputReceived (object sender, string audioFile)
		{
			//This handler is called once audio recording is complete. 
			//	It is run on a background thread so we'll need to run UI code on the main thread

			Device.BeginInvokeOnMainThread (() => UpdateUI (false, "Record", true));

			//if we're not streaming the audio as we're recording, we'll use the file-based STT API here
			if (!StreamSwitch.IsToggled)
			{
				string resultText = null;

				if (audioFile != null)
				{
					resultText = await SpeechToText (audioFile);
				}

				Device.BeginInvokeOnMainThread (() =>
				{
					ResultsLabel.Text = resultText ?? "No Results!";
					UpdateUI (true, false);
				});
			}
		}


		async Task<string> SpeechToText (string audioFile)
		{
			try
			{
				switch (OutputMode)
				{
					case OutputMode.Simple:
						var simpleResult = await speechClient.SpeechToTextSimple (audioFile);

						return ProcessResult (simpleResult);
					case OutputMode.Detailed:
						var detailedResult = await speechClient.SpeechToTextDetailed (audioFile);

						return ProcessResult (detailedResult);
				}

				return null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex);

				return ProcessResult (ex);
			}
		}


		async Task<string> SpeechToText (Task audioRecordTask)
		{
			try
			{
				using (var stream = recorder.GetAudioFileStream ())
				{
					switch (OutputMode)
					{
						case OutputMode.Simple:
							var simpleResult = await speechClient.SpeechToTextSimple (stream, recorder.AudioStreamDetails.SampleRate, audioRecordTask);

							return ProcessResult (simpleResult);
						case OutputMode.Detailed:
							var detailedResult = await speechClient.SpeechToTextDetailed (stream, recorder.AudioStreamDetails.SampleRate, audioRecordTask);

							return ProcessResult (detailedResult);
					}
				}

				return null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex);

				return ProcessResult (ex);
			}
		}


		string ProcessResult (RecognitionSpeechResult speechResult)
		{
			string resultText = null;

			if (speechResult != null)
			{
				resultText = $"Recognition Status: {speechResult.RecognitionStatus}\r\n" +
					$"DisplayText: {speechResult.DisplayText}\r\n" +
					$"Offset: {speechResult.Offset}\r\n" +
					$"Duration: {speechResult.Duration}";
			}

			Debug.WriteLine (resultText);

			return resultText;
		}


		string ProcessResult (RecognitionResult recognitionResult)
		{
			string resultText = null;

			if (recognitionResult != null)
			{
				resultText = $"Recognition Status: {recognitionResult.RecognitionStatus}\r\n" +
					$"Offset: {recognitionResult.Offset}\r\n" +
					$"Duration: {recognitionResult.Duration}\r\n";

				if (recognitionResult.RecognitionStatus == RecognitionStatus.Success &&
					recognitionResult.Results.Any ())
				{
					var speechResult = recognitionResult.Results.First ();

					resultText += $"--::First Result::--\r\n" +
						$"Confidence: {speechResult.Confidence}\r\n" +
						$"Lexical: {speechResult.Lexical}\r\n" +
						$"Display: {speechResult.Display}\r\n" +
						$"ITN: {speechResult.ITN}\r\n" +
						$"Masked ITN: {speechResult.MaskedITN}";
				}
			}

			Debug.WriteLine (resultText);

			return resultText;
		}


		string ProcessResult (Exception ex)
		{
			string resultText = null;

			if (ex != null)
			{
				resultText = $"Error occurred: {ex.Message}\r\n" +
					$"Stack trace: {ex.StackTrace}";
			}

			Debug.WriteLine (resultText);

			return resultText;
		}
	}
}