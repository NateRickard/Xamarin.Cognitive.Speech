using System;
using System.Threading.Tasks;
using Plugin.AudioRecorder;
using Xamarin.Forms;

namespace Xamarin.Cognitive.BingSpeech.Sample
{
	public partial class MainPage : ContentPage
	{
		const string BingSpeechSubscriptionKey = "GET ONE";

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

			bingSpeechClient = new BingSpeechApiClient (BingSpeechSubscriptionKey);
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


		async void Recorder_AudioInputReceived (object sender, string audioFile)
		{
			Device.BeginInvokeOnMainThread (() => RecordButton.Text = "Record");

			//do STT

			if (audioFile != null)
			{
				var speechResult = await bingSpeechClient.SpeechToText (audioFile);

				if (speechResult != null)
				{
					var text = $"Confidence: {speechResult.Confidence}\r\n" +
						$"Lexical: {speechResult.Lexical}\r\n" +
						$"Name: {speechResult.Name}" +
						$"Scenario: {speechResult.Scenario}";

					System.Diagnostics.Debug.WriteLine (text);
				}
			}
		}
	}
}