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


		async void Record_Clicked (object sender, System.EventArgs e)
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
			RecordButton.Text = "Record";

			//do STT


		}
	}
}