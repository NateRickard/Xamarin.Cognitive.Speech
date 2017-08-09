using Newtonsoft.Json;

namespace Xamarin.Cognitive.BingSpeech.Model
{
	//[JsonObject ("result")]
	public class SpeechResult
	{
		public float Confidence { get; set; }

		public string Lexical { get; set; }

		public string ITN { get; set; }

		public string MaskedITN { get; set; }

		public string Display { get; set; }
	}
}

//"RecognitionStatus": "Success",
//"Offset": 22500000,
//"Duration": 21000000,
//"NBest": [{
//    "Confidence": 0.941552162,
//    "Lexical": "find a funny movie to watch",
//    "ITN": "find a funny movie to watch",
//    "MaskedITN": "find a funny movie to watch",
//    "Display": "Find a funny movie to watch."
//}]

//{"RecognitionStatus":"Success","DisplayText":"This is a test of the push functionality.","Offset":8500000,"Duration":27800000}