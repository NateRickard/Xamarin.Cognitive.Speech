using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Plugin.AudioRecorder;
using Xamarin.Cognitive.BingSpeech.Model;
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

			recorder.AudioInputReceived += Recorder_AudioInputReceived;

			if (Keys.BingSpeech.SubscriptionKey == Keys.BingSpeech.BadSubscriptionKey)
			{
				throw new Exception ("Get a Bind Speech API key here: https://azure.microsoft.com/en-us/pricing/details/cognitive-services/speech-api/");
			}

			bingSpeechClient = new BingSpeechApiClient (Keys.BingSpeech.SubscriptionKey);

			//go fetch an auth token up frot - this should decrease latecy on the first call.
			//	Otherwise, this would be called automatically the first time I use the speech client
			Task.Run (() => bingSpeechClient.Authenticate ());
		}


		async void Record_Clicked (object sender, EventArgs e)
		{
			await RecordAudio ();
		}


		async Task RecordAudio ()
		{
			try
			{
				if (!recorder.IsRecording)
				{
					RecordButton.IsEnabled = false;

					await recorder.StartRecording ();

					RecordButton.Text = "Stop";
					RecordButton.IsEnabled = true;

					var recognitionMode = (RecognitionMode) Enum.Parse (typeof (RecognitionMode), RecognitionModePicker.SelectedItem.ToString ());
					var profanityMode = (ProfanityMode) Enum.Parse (typeof (ProfanityMode), ProfanityModePicker.SelectedItem.ToString ());
					outputMode = (OutputMode) Enum.Parse (typeof (OutputMode), OutputModePicker.SelectedItem.ToString ());

					//set the selected recognition mode
					bingSpeechClient.RecognitionMode = recognitionMode;
					bingSpeechClient.ProfanityMode = profanityMode;

					if (SteamSwitch.IsToggled)
					{
						spinner.IsVisible = true;
						spinner.IsRunning = true;

						var resultText = await SpeechToText ();
						ResultsLabel.Text = resultText ?? "No Results!";

						spinner.IsVisible = false;
						spinner.IsRunning = false;
					}
				}
				else
				{
					RecordButton.IsEnabled = false;

					await recorder.StopRecording ();

					RecordButton.Text = "Record";
					RecordButton.IsEnabled = true;
				}
			}
			catch (Exception ex)
			{
				//blow up the app!
				throw ex;
			}
		}


		async void Recorder_AudioInputReceived (object sender, string audioFile)
		{
			Device.BeginInvokeOnMainThread (() =>
			{
				RecordButton.Text = "Record";
				spinner.IsVisible = true;
				spinner.IsRunning = true;
			});

			if (audioFile != null && !SteamSwitch.IsToggled)
			{
				var resultText = await SpeechToText (audioFile);

				Device.BeginInvokeOnMainThread (() =>
				{
					ResultsLabel.Text = resultText ?? "No Results!";
					spinner.IsVisible = false;
					spinner.IsRunning = false;
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
				System.Diagnostics.Debug.WriteLine (ex);
				throw;
			}
		}


		async Task<string> SpeechToText ()
		{
			try
			{
				using (var stream = recorder.GetAudioFileStream ())
				{
					switch (outputMode)
					{
						case OutputMode.Simple:
							var simpleResult = await bingSpeechClient.SpeechToTextSimple (stream, recorder.AudioStreamDetails.ChannelCount, recorder.AudioStreamDetails.SampleRate, recorder.AudioStreamDetails.BitsPerSample);

							return ProcessResult (simpleResult);
						case OutputMode.Detailed:
							var detailedResult = await bingSpeechClient.SpeechToTextDetailed (stream, recorder.AudioStreamDetails.ChannelCount, recorder.AudioStreamDetails.SampleRate, recorder.AudioStreamDetails.BitsPerSample);

							return ProcessResult (detailedResult);
					}
				}

				return null;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine (ex);
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

			System.Diagnostics.Debug.WriteLine (resultText);

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

			System.Diagnostics.Debug.WriteLine (resultText);

			return resultText;
		}
	}
}