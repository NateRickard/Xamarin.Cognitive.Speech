using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Plugin.AudioRecorder;
using Xamarin.Forms;

namespace Xamarin.Cognitive.BingSpeech.Sample
{
	public partial class MainPage : ContentPage
	{
		AudioRecorderService recorder;
		BingSpeechApiClient bingSpeechClient;
		OutputMode outputMode;

		public MainPage ()
		{
			InitializeComponent ();

			//setting these in XAML doesn't seem to be working
			RecognitionModePicker.SelectedIndex = 0;
			OutputModePicker.SelectedIndex = 0;
			ProfanityModePicker.SelectedIndex = 0;

			recorder = new AudioRecorderService
			{
				StopRecordingOnSilence = true,
				StopRecordingAfterTimeout = true,
				TotalAudioTimeout = TimeSpan.FromSeconds (15) //Bing speech REST API has 15 sec max
			};

			if (Keys.BingSpeech.SubscriptionKey == Keys.BingSpeech.BadSubscriptionKey)
			{
				throw new Exception ("Get a Bing Speech API key here: https://azure.microsoft.com/en-us/pricing/details/cognitive-services/speech-api/");
			}

			bingSpeechClient = new BingSpeechApiClient (Keys.BingSpeech.SubscriptionKey);

			//go fetch an auth token up front - this should decrease latecy on the first call. 
			//	Otherwise, this would be called automatically the first time I use the speech client
			Task.Run (() => bingSpeechClient.Authenticate ());
		}


		async void Record_Clicked (object sender, EventArgs e)
		{
			await RecordAudio ();
			//await RecordAudioAlternate ();
		}


		void updateUI (bool buttonEnabled, bool spinnerEnabled = false)
		{
			updateUI (buttonEnabled, null, spinnerEnabled);
		}


		void updateUI (bool buttonEnabled, string buttonText, bool spinnerEnabled = false)
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
					updateUI (false);

					//start recording audio
					var audoRecordTask = await recorder.StartRecording ();

					updateUI (true, "Stop");

					//configure the Bing Speech client
					var recognitionMode = (RecognitionMode) Enum.Parse (typeof (RecognitionMode), RecognitionModePicker.SelectedItem.ToString ());
					var profanityMode = (ProfanityMode) Enum.Parse (typeof (ProfanityMode), ProfanityModePicker.SelectedItem.ToString ());
					outputMode = (OutputMode) Enum.Parse (typeof (OutputMode), OutputModePicker.SelectedItem.ToString ());

					//set the selected recognition mode & profanity mode
					bingSpeechClient.RecognitionMode = recognitionMode;
					bingSpeechClient.ProfanityMode = profanityMode;

					//if we want to stream the audio as it's recording, we'll do that below
					if (SteamSwitch.IsToggled)
					{
						//does nothing more than turn the spinner on once recording is complete
						_ = audoRecordTask.ContinueWith ((audioFile) => updateUI (false, "Record", true), TaskScheduler.FromCurrentSynchronizationContext ());

						//do streaming speech to text
						var resultText = await SpeechToText (audoRecordTask);
						ResultsLabel.Text = resultText ?? "No Results!";

						updateUI (true, "Record", false);
					}
					else //waits for the audio file to finish recording before starting to send audio data to the server
					{
						var audioFile = await audoRecordTask;

						updateUI (true, "Record", true);

						//if we're not streaming the audio as we're recording, we'll use the file-based STT API here
						if (audioFile != null)
						{
							var resultText = await SpeechToText (audioFile);
							ResultsLabel.Text = resultText ?? "No Results!";
						}

						updateUI (true, false);
					}
				}
				else //Stop button clicked
				{
					updateUI (false, true);

					//stop the recording...
					await recorder.StopRecording ();
				}
			}
			catch (Exception ex)
			{
				//blow up the app!
				throw ex;
			}
		}


		//Hook up this altrernate handler to try out the event-based API

		async Task RecordAudioAlternate ()
		{
			if (!recorder.IsRecording)
			{
				recorder.AudioInputReceived -= Recorder_AudioInputReceived;
				recorder.AudioInputReceived += Recorder_AudioInputReceived;

				updateUI (false);

				await recorder.StartRecording ();

				updateUI (true, "Stop");

				var recognitionMode = (RecognitionMode) Enum.Parse (typeof (RecognitionMode), RecognitionModePicker.SelectedItem.ToString ());
				var profanityMode = (ProfanityMode) Enum.Parse (typeof (ProfanityMode), ProfanityModePicker.SelectedItem.ToString ());
				outputMode = (OutputMode) Enum.Parse (typeof (OutputMode), OutputModePicker.SelectedItem.ToString ());

				//set the selected recognition mode & profanity mode
				bingSpeechClient.RecognitionMode = recognitionMode;
				bingSpeechClient.ProfanityMode = profanityMode;

				if (SteamSwitch.IsToggled)
				{
					throw new Exception ("Use RecordAudio() with the Stream API - this is older code");
				}
			}
			else //Stop button clicked
			{
				updateUI (false, true);

				//stop the recording... recorded audio will be used in the Recorder_AudioInputReceived handler below
				await recorder.StopRecording ();
			}
		}


		async void Recorder_AudioInputReceived (object sender, string audioFile)
		{
			//This handler is called once audio recording is complete. 
			//	It is run on a background thread so we'll need to run UI code on the main thread

			Device.BeginInvokeOnMainThread (() => updateUI (false, "Record", true));

			//if we're not streaming the audio as we're recording, we'll use the file-based STT API here
			if (!SteamSwitch.IsToggled)
			{
				string resultText = null;

				if (audioFile != null)
				{
					resultText = await SpeechToText (audioFile);
				}

				Device.BeginInvokeOnMainThread (() =>
				{
					ResultsLabel.Text = resultText ?? "No Results!";
					updateUI (true, false);
				});
			}
		}


		async Task<string> SpeechToText (string audioFile)
		{
			try
			{
				switch (outputMode)
				{
					case OutputMode.Simple:
						var simpleResult = await bingSpeechClient.SpeechToTextSimple (audioFile);

						return ProcessResult (simpleResult);
					case OutputMode.Detailed:
						var detailedResult = await bingSpeechClient.SpeechToTextDetailed (audioFile);

						return ProcessResult (detailedResult);
				}

				return null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex);
				throw;
			}
		}


		async Task<string> SpeechToText (Task audoRecordTask)
		{
			try
			{
				using (var stream = recorder.GetAudioFileStream ())
				{
					switch (outputMode)
					{
						case OutputMode.Simple:
							var simpleResult = await bingSpeechClient.SpeechToTextSimple (stream, recorder.AudioStreamDetails.SampleRate, audoRecordTask);

							return ProcessResult (simpleResult);
						case OutputMode.Detailed:
							var detailedResult = await bingSpeechClient.SpeechToTextDetailed (stream, recorder.AudioStreamDetails.SampleRate, audoRecordTask);

							return ProcessResult (detailedResult);
					}
				}

				return null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex);
				throw;
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

			if (recognitionResult != null && recognitionResult.Results.Any ())
			{
				resultText = $"Recognition Status: {recognitionResult.RecognitionStatus}\r\n" +
					$"Offset: {recognitionResult.Offset}\r\n" +
					$"Duration: {recognitionResult.Duration}\r\n";

				var speechResult = recognitionResult.Results.First ();

				resultText += $"--::First Result::--\r\n" +
					$"Confidence: {speechResult.Confidence}\r\n" +
					$"Lexical: {speechResult.Lexical}\r\n" +
					$"Display: {speechResult.Display}\r\n" +
					$"ITN: {speechResult.ITN}\r\n" +
					$"Masked ITN: {speechResult.MaskedITN}";
			}

			Debug.WriteLine (resultText);

			return resultText;
		}
	}
}