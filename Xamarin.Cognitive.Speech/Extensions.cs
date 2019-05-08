using System;
using System.IO;
using System.Net;
using System.Text;

namespace Xamarin.Cognitive.Speech
{
	static class Extensions
	{
		/// <summary>
		/// Writes a WAV file header using the specified audio details.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to write the WAV header to.</param>
		/// <param name="channelCount">The number of channels in the recorded audio.</param>
		/// <param name="sampleRate">The sample rate of the recorded audio.</param>
		/// <param name="bitsPerSample">The bits per sample of the recorded audio.</param>
		/// <param name="audioLength">The length/byte count of the recorded audio, or -1 if recording is still in progress or length is unknown.</param>
		public static void WriteWaveHeader (this Stream stream, int channelCount, int sampleRate, int bitsPerSample, int audioLength = -1)
		{
			var blockAlign = (short) (channelCount * (bitsPerSample / 8));
			var averageBytesPerSecond = sampleRate * blockAlign;

			using (var writer = new BinaryWriter (stream, Encoding.UTF8))
			{
				if (writer.BaseStream.CanSeek)
				{
					writer.Seek (0, SeekOrigin.Begin);
				}

				//chunk ID
				writer.Write (Encoding.UTF8.GetBytes ("RIFF"));

				if (audioLength > -1)
				{
					writer.Write (audioLength + 36); // 36 + subchunk 2 size (data size)
				}
				else
				{
					writer.Write (audioLength); // -1 (Unkown size)
				}

				//format
				writer.Write (Encoding.UTF8.GetBytes ("WAVE"));

				//subchunk 1 ID
				writer.Write (Encoding.UTF8.GetBytes ("fmt "));

				writer.Write (16); //subchunk 1 (fmt) size
				writer.Write ((short) 1); //PCM audio format

				writer.Write ((short) channelCount);
				writer.Write (sampleRate);
				writer.Write (averageBytesPerSecond);
				writer.Write (blockAlign);
				writer.Write ((short) bitsPerSample);

				//subchunk 2 ID
				writer.Write (Encoding.UTF8.GetBytes ("data"));

				//subchunk 2 (data) size
				writer.Write (audioLength);
			}
		}

		/// <summary>
		/// Checks if the Exception is a <see cref="WebException"/> and if so, evaluates if the StatusCode is equal to the given status code.
		/// </summary>
		/// <param name="ex">The <see cref="Exception"/> to evaluate.</param>
		/// <param name="code">The <see cref="HttpStatusCode"/> to check for.</param>
		/// <returns></returns>
		public static bool HasWebResponseStatus (this Exception ex, HttpStatusCode code)
		{
			if (ex is WebException webEx)
			{
				if (webEx.Response is HttpWebResponse response)
				{
					return response.StatusCode == code;
				}
			}

			return false;
		}
	}
}