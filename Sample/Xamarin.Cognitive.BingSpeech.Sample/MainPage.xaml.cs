using System;
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


		public MainPage ()
		{
			InitializeComponent ();

			//setting these in XAML doesn't seem to be working
			RecognitionModePicker.SelectedIndex = 0;
			OutputModePicker.SelectedIndex = 0;

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


		void Recorder_AudioInputReceived (object sender, string audioFile)
		{
			string resultText = null;

			Device.BeginInvokeOnMainThread (async () =>
			{
				try
				{
					RecordButton.Text = "Record";
					spinner.IsVisible = true;
					spinner.IsRunning = true;

					var recognitionMode = (RecognitionMode) Enum.Parse (typeof (RecognitionMode), RecognitionModePicker.SelectedItem.ToString ());
					var outputMode = (OutputMode) Enum.Parse (typeof (OutputMode), OutputModePicker.SelectedItem.ToString ());

					if (audioFile != null)
					{
						//set the selected recognition mode
						bingSpeechClient.RecognitionMode = recognitionMode;

						switch (outputMode)
						{
							case OutputMode.Simple:
								var simpleResult = await bingSpeechClient.SpeechToTextSimple (audioFile);

								if (simpleResult != null)
								{
									resultText = $"Recognition Status: {simpleResult.RecognitionStatus}\r\n" +
										$"DisplayText: {simpleResult.DisplayText}\r\n" +
										$"Offset: {simpleResult.Offset}\r\n" +
										$"Duration: {simpleResult.Duration}";
								}
								break;
							case OutputMode.Detailed:
								var detailedResult = await bingSpeechClient.SpeechToTextDetailed (audioFile);

								if (detailedResult != null && detailedResult.Results.Any ())
								{
									resultText = $"Recognition Status: {detailedResult.RecognitionStatus}\r\n" +
										$"Offset: {detailedResult.Offset}\r\n" +
										$"Duration: {detailedResult.Duration}\r\n";

									var result = detailedResult.Results.First ();

									resultText += $"--::First Result::--\r\n" +
										$"Confidence: {result.Confidence}\r\n" +
										$"Lexical: {result.Lexical}\r\n" +
										$"Display: {result.Display}\r\n" +
										$"ITN: {result.ITN}\r\n" +
										$"Masked ITN: {result.MaskedITN}";
								}
								break;
						}

						System.Diagnostics.Debug.WriteLine (resultText);
						ResultsLabel.Text = resultText ?? "No Results!";
						spinner.IsVisible = false;
						spinner.IsRunning = false;
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine (ex);
				}
			});
		}
	}
}