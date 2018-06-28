using System;
using System.IO;
using System.Net;
using System.Text;

namespace Xamarin.Cognitive.BingSpeech
{
	static class Extensions
	{
		public static void WriteWaveHeader (this Stream stream, int channelCount, int sampleRate, int bitsPerSample)
		{
			using (var writer = new BinaryWriter (stream, Encoding.UTF8))
			{
				if (writer.BaseStream.CanSeek)
				{
					writer.Seek (0, SeekOrigin.Begin);
				}

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