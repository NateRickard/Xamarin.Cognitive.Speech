using System;
using System.Threading.Tasks;
using Plugin.AudioRecorder;
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
			try
			{
				Device.BeginInvokeOnMainThread (async () =>
				{
					RecordButton.Text = "Record";

					//do STT

					if (audioFile != null)
					{
						var speechResult = await bingSpeechClient.SpeechToTextSimple (audioFile);

						if (speechResult != null)
						{
							//var text = $"Confidence: {speechResult.Confidence}\r\n" +
							//$"Lexical: {speechResult.Lexical}\r\n" +
							//$"Display: {speechResult.Display}" +
							//$"ITN: {speechResult.ITN}" +
							//$"Masked ITN: {speechResult.MaskedITN}";

							var text = $"DisplayText: {speechResult.DisplayText}\r\n" +
								$"Recognition Status: {speechResult.RecognitionStatus}\r\n" +
								$"Offset: {speechResult.Offset}\r\n" +
								$"Duration: {speechResult.Duration}";

							System.Diagnostics.Debug.WriteLine (text);
						}
					}
				});
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine (ex);
			}
		}
	}
}