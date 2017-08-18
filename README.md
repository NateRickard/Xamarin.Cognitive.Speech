# Xamarin.Cognitive.BingSpeech ![NuGet](https://img.shields.io/nuget/v/Xamarin.Cognitive.BingSpeech.svg?label=NuGet)

`Xamarin.Cognitive.BingSpeech` is a managed client library that makes it easy to work with the [Microsoft Cognitive Services Bing Speech API](https://azure.microsoft.com/en-us/services/cognitive-services/speech/) on Xamarin.iOS, Xamarin.Android, Xamarin.Forms, and other Portable Class Library (PCL) projects.

Includes a Xamarin.Forms sample with iOS and Android apps.

Resources about the Bing Speech API and what it is:

- [Learn about the Bing Speech API](https://azure.microsoft.com/en-us/services/cognitive-services/speech/)
- [Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/home)


# Why?

Why use this client library to talk to the Bing Speech API and not [insert other lib or sample code here]?

- The official C# SDK is made with Windows in mind, not Xamarin.
- Many/most other Xamarin-based examples or sample apps I've found:
	- Use an older version of the Bing Speech API.
	- Use the older `HttpWebRequest` API.
	- Contain very little in the way of feature support.
	- Arent mobile specific or don't contain mobile examples

I wanted to create something specific to mobile, that was updated and easy to use.


# Features

- 100% `HttpClient`-based, managed code
- Works with the latest version of the Bing Speech API
- Advanced Bing Speech API feature support, such as:
	- Chunked transfer encoding for efficient audio streaming to server
	- Three recognition modes: Interactive, Conversation, Dictation
	- Simple or Detailed Output Mode
	- 3 profanity modes: Masked, Removed, or Raw
	- Recognition language support
- Ability to stream audio to the server as it's being recorded
- Easy to use API
- Works seamlessly with [Plugin.AudioRecorder](https://www.nuget.org/packages/Plugin.AudioRecorder/), a cross platform way to record device microphone input


# Setup

`Xamarin.Cognitive.BingSpeech` is available as a [NuGet package](https://www.nuget.org/packages/Xamarin.Cognitive.BingSpeech/) to be added to your Xamarin.iOS, Xamarin.Android, Xamarin.Forms, or UWP project(s).

The included PCL assembly is compiled against Profile 111, which is currently the same profile used for PCL-based Xamarin.Forms solutions.


## Platform Configuration

**Note** if you're using the [Audio Recorder Plugin](https://www.nuget.org/packages/Plugin.AudioRecorder/) to record audio to send to the Bing Speech API, you'll want to review the [platform-specific permissions required for each platform](https://github.com/NateRickard/Plugin.AudioRecorder#permissions) there for accessing the microphone and recording audio.

### HttpClient configuration

When using the Bing Speech API, you must use one of the native `HttpClient` implementations that support the TLS 1.2 stack.

Please review the [documentation here](https://developer.xamarin.com/guides/cross-platform/transport-layer-security/) for more information about what this means and how to implement these on each platform.  The included sample apps use the `NSUrlSession` (iOS) and `AndroidClientHandler` (Android) implementations and these are recommended.


# Usage

The [sample app](https://github.com/NateRickard/Xamarin.Cognitive.BingSpeech/tree/master/Sample) demonstrates everything shown below and more.

The Bing Speech API has two distinct output modes that yeild different results.  More information on each output mode [can be found in the documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format).


## Simple Output Mode

Simple output mode will return a single result with less detail.

```C#
var audioFile = "/a/path/to/my/audio/file/in/WAV/format.wav";

var simpleResult = await bingSpeechClient.SpeechToTextSimple (audioFile);
```

Simple output mode will return a `RecognitionSpeechResult` with the following structure:

```C#
/// <summary>
/// A single speech result combining Recogniton result with Speech result.  This is used for Simple result mode.
/// </summary>
public class RecognitionSpeechResult
{
	/// <summary>
	/// A string indicating the result status.  Successful requests will return "Success"
	/// </summary>
	public RecognitionStatus RecognitionStatus { get; set; }

	/// <summary>
	/// Gets or sets the offset.  
	/// The Offset element specifies the offset (in 100-nanosecond units) at which the phrase was recognized, relative to the start of the audio stream
	/// </summary>
	public long Offset { get; set; }


	/// <summary>
	/// The duration of speech.  
	/// The Duration element specifies the duration (in 100-nanosecond units) of this speech phrase.
	/// </summary>
	public long Duration { get; set; }


	/// <summary>
	/// The top result (by confidence), returned in Display Form.
	/// </summary>
	/// <remarks>The display form adds punctuation and capitalization to recognition results, making it the most appropriate form for applications that display the spoken text.</remarks>
	public string DisplayText { get; set; }
}
```


## Detailed Output Mode

Detailed output mode will return more detail, and possibly more than one result.

```C#
var audioFile = "/a/path/to/my/audio/file/in/WAV/format.wav";

var detailedResult = await bingSpeechClient.SpeechToTextDetailed (audioFile);
```

Detailed output mode will return a `RecognitionResult` with the following structure:

```C#
/// <summary>
/// Recognition result.
/// </summary>
public class RecognitionResult
{
	/// <summary>
	/// A string indicating the result status.  Successful requests will return "Success"
	/// </summary>
	public RecognitionStatus RecognitionStatus { get; set; }


	/// <summary>
	/// Gets or sets the offset.  
	/// The Offset element specifies the offset (in 100-nanosecond units) at which the phrase was recognized, relative to the start of the audio stream.
	/// </summary>
	public long Offset { get; set; }


	/// <summary>
	/// The duration of speech.  
	/// The Duration element specifies the duration (in 100-nanosecond units) of this speech phrase.
	/// </summary>
	public long Duration { get; set; }


	/// <summary>
	/// A list of <see cref="SpeechResult"/> containing the N-best results (top result + any alternatives), ordered by their confidence.
	/// </summary>
	[JsonProperty ("NBest")]
	public List<SpeechResult> Results { get; set; }
}
```

The `Results` list can contain one _or more_ speech results, which will contain an associated confidence score.  These results will be ordered by their confidence.

Each `SpeechResult` will have the following structure:

```C#
/// <summary>
/// A Speech result that is part of a detailed resultset.
/// </summary>
public class SpeechResult
{
	/// <summary>
	/// Confidence scores range from 0 to 1. A score of 1 represents the highest level of confidence. A score of 0 represents the lowest level of confidence.
	/// </summary>
	public float Confidence { get; set; }

	/// <summary>
	/// The lexical form is the recognized text, exactly how it occurred in the utterance and without punctuation or capitalization.
	/// </summary>
	public string Lexical { get; set; }


	/// <summary>
	/// The ITN form of a recognition result does not include capitalization or punctuation. 
	/// The ITN form is most appropriate for applications that act on the recognized text.
	/// </summary>
	public string ITN { get; set; }


	/// <summary>
	/// The masked ITN form applies profanity masking to the inverse text normalization form.
	/// </summary>
	public string MaskedITN { get; set; }


	/// <summary>
	/// The display form adds punctuation and capitalization to recognition results, making it the most appropriate form for applications that display the spoken text.
	/// </summary>
	public string Display { get; set; }
}
```

## Streaming Audio

It's also possible to send audio to the server from a `Stream`.  The most common use of this would be to start sending audio data to the Bing Speech API as it's still being recorded.

In addition to the audio `Stream`, you also need to provide the sample rate, and, optionally, a `Task` object that will indicate when the audio stream has finished recording.  If the `Task` is omitted, the client library will attempt to make additonal reads from the stream until no further audio data is detected; hwoever, this is not recommended.

```C#
//simple output mode

var simpleResult = await bingSpeechClient.SpeechToTextSimple (stream, <sample rate>, <audio record Task>);

//... or detailed output mode

var detailedResult = await bingSpeechClient.SpeechToTextDetailed (stream, <sample rate>, <audio record Task>);
```

Since this library was developed with my [audio recorder plugin](https://github.com/NateRickard/Plugin.AudioRecorder) in mind, this has been made quite simple:

```C#
//start recording audio
var audioRecordTask = await recorder.StartRecording ();

using (var stream = recorder.GetAudioFileStream ())
{
	//this will begin sending the recording audio data as it continues to record
	var simpleResult = await bingSpeechClient.SpeechToTextSimple (stream, recorder.AudioStreamDetails.SampleRate, audioRecordTask);
}
```

	
# Contributing

Contributions are welcome and encouraged.  Feel free to file issues and pull requests on the repo and I'll address them as time permits.


# About

- Created by [Nate Rickard](https://github.com/naterickard)

## License

Licensed under the MIT License (MIT). See [LICENSE](LICENSE) for details.

`PushStreamContent` class from the excellent [Refit](https://github.com/paulcbetts/refit) library by Paul Betts and licensed under the Apache License, Version 2.0 (original license from Microsoft).