# Xamarin.Cognitive.BingSpeech ![NuGet](https://img.shields.io/nuget/v/Xamarin.Cognitive.BingSpeech.svg?label=NuGet)

`Xamarin.Cognitive.BingSpeech` is a managed client library that makes it easy to work with the [Microsoft Cognitive Services Bing Speech API](https://azure.microsoft.com/en-us/services/cognitive-services/speech/) on Xamarin.iOS, Xamarin.Android, Xamarin.Forms, UWP, and other .NET Standard 2.0+ projects.

Includes a Xamarin.Forms sample with iOS, Android, and UWP apps.

Resources about the Bing Speech API and what it is:

- [Learn about the Bing Speech API](https://azure.microsoft.com/en-us/services/cognitive-services/speech/)
- [Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/home)


# Why?

Why use this client library to talk to the Bing Speech API and not [insert other lib or sample code here]?

- The official C# SDK is made with Windows in mind, not Xamarin/mobile.
- Many/most other Xamarin-based examples or sample apps I've found:
	- Use an older version of the Bing Speech API.
	- Use the older `HttpWebRequest` network APIs.
	- Contain very little in the way of feature support.
	- Are not mobile-specific or don't contain mobile examples/value-adds

I wanted to create something specific to mobile, that was updated, easy to use, and included some niceties specific to the mobile world.


# Features

- 100% `HttpClient`-based, managed code (.NET Standard 2.0)
- Works with the latest version of the Bing Speech API
- Advanced Bing Speech API feature support, such as:
	- Chunked transfer encoding for efficient audio streaming to server
	- Three recognition modes:
    	- Interactive
    	- Conversation
    	- Dictation
	- Simple vs. Detailed Output Mode
	- 3 profanity modes:
    	- Masked
    	- Removed
    	- Raw
	- Recognition language support
	- Multiple authentication modes
- Ability to stream audio to the server as it's being recorded
- Easy to use API
- Works seamlessly with [Plugin.AudioRecorder](https://www.nuget.org/packages/Plugin.AudioRecorder/), a cross platform way to record device microphone input


# Setup

`Xamarin.Cognitive.BingSpeech` is available as a [NuGet package](https://www.nuget.org/packages/Xamarin.Cognitive.BingSpeech/) to be added to your Xamarin.iOS, Xamarin.Android, Xamarin.Forms, UWP, or other .NET Standard 2.0+ project(s).

You must have a valid Bing Speech API subscription key.  You can get a free trial key or create a permanent key in the [Bing Speech API portal](https://azure.microsoft.com/en-us/services/cognitive-services/speech/).

Once you have an API key, you can construct an instance of the client:

```C#
var bingSpeechClient = new BingSpeechApiClient ("<YOUR KEY>");
```

Read more details on [how to use the client below](https://github.com/NateRickard/Xamarin.Cognitive.BingSpeech#usage).

To run the sample(s), update the `Keys.cs` file so the `SubscriptionKey` property is set to your API key:

```C#
public const string SubscriptionKey = "My Key Goes Here";
```


## Platform-Specific Configuration

All app platforms should have 'Internet' or similar permissions in order to communicate with the Bing Speech API.  Outside of this, no platform specific config is required for this library to function.

**Note:** if you're using the [Audio Recorder Plugin](https://www.nuget.org/packages/Plugin.AudioRecorder/) (or likely any other method of recording audio) to send audio to the Bing Speech API, you'll want to review the [platform-specific permissions required for each platform](https://github.com/NateRickard/Plugin.AudioRecorder#required-permissions--capabilities) there for accessing the microphone and recording audio.

### iOS

With the latest speech api we've seen the need to change your iOS build config(s) to [use the NSUrlSession HttpClient implementation](https://docs.microsoft.com/en-us/xamarin/ios/app-fundamentals/ats#setting-the-httpclient-implementation).  If you're experiencing "Service Unavailable" errors on iOS, this is likely what you need to do to resolve the issue.  This must be done for all build configs being used (i.e. iPhoneSimulator **and** iPhone).

### Android

With the latest speech api we've seen the need to change your Android project to [use the Android HttpClient implementation](https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/http-stack?tabs=windows).  If you're experiencing "Service Unavailable" errors on Android, this is likely what you need to do to resolve the issue.  This must be done for all build configs being used.


# Usage

The [sample app](https://github.com/NateRickard/Xamarin.Cognitive.BingSpeech/tree/master/Sample) demonstrates everything shown below and more.

The first thing you'll need to do is construct a new instance of the Bing Speech API client that will be used for your API calls:

```C#
BingSpeechApiClient bingSpeechClient = new BingSpeechApiClient ("<YOUR API KEY>");
```

## Authentication

[As noted above](#setup), you'll need to procure a subscription key in order to authenticate to the Speech service.  There are 2  ways to authenticate, [as described in the service documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication?tabs=CSharp):

1. Subscription key  
  
    This method passes the subscription key as a header in the service request.  Enable this method by setting the authentication mode as follows:
    
    ```c#  
    speechClient.AuthenticationMode = AuthenticationMode.SubscriptionKey;
    ```

2. Authorization token  
  
    This method makes a call to the authentication endpoint to get a reusable JWT authorization token that lasts for 10 minutes.  If the token expires after 10 minutes, the library will attempt to re-authenticate with the authentication endpoint.
    
    This is the default, and recommended, authentication method.
    
    This library will authenticate transparently to the endpoint  if it doesn't already have an authentication token cached.  If you'd like to proactively authenticate before making your first speech to text call (to reduce latency of the first call), you may do the following:
    
    ```c#
    speechClient.AuthenticateWithToken (); // pass true to overwrite any existing token
    ```
    
    To clear the current auth token, you can do the following:
    
    ```c#
    speechClient.ClearAuthToken ();
    ```
    
## Output Modes

The Bing Speech API has two distinct output modes that yield different results.  More information on each output mode [can be found in the documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoicerecognition#output-format).


### Simple Output Mode

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


### Detailed Output Mode

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

In addition to the audio `Stream`, you can also provide an optional `Task` object that will indicate when the audio stream has finished recording.  If the `Task` is omitted, the client library will attempt to make additional reads from the stream until no further audio data is detected; however, this is not recommended.

```C#
// simple output mode

var simpleResult = await bingSpeechClient.SpeechToTextSimple (stream, <audio record Task>);

// ... or detailed output mode

var detailedResult = await bingSpeechClient.SpeechToTextDetailed (stream, <audio record Task>);
```

**NOTE** If you're streaming raw PCM audio data, a WAV/RIFF header needs to be written to the beginning of your audio data (this is a requirement of the Speech API).  This library is able to write the header to the outgoing network stream prior to sending the audio data; however, you MUST use one of the `SpeechToTextSimple` or `SpeechToTextDetailed` overloads that takes those audio details.

Since this library was developed with my [audio recorder plugin](https://github.com/NateRickard/Plugin.AudioRecorder) in mind, proper streaming has been made quite simple:

```C#
// start recording audio
var audioRecordTask = await recorder.StartRecording ();

using (var stream = recorder.GetAudioFileStream ())
{
	// this will begin sending the recording audio data as it continues to record
	var simpleResult = await bingSpeechClient.SpeechToTextSimple (stream, recorder.AudioStreamDetails.SampleRate, audioRecordTask);
}
```

As the audio recorder plugin stream will be raw PCM data, the sample rate (at a minimum) must be passed in so the proper RIFF header can be sent.

## Endpoints

By default, the library will use the standard STT and authentication endpoints for the Speech service:

Auth: `https://api.cognitive.microsoft.com/sts/v1.0/issueToken`
STT: `https://speech.platform.bing.com/speech/recognition`

To use a STT endpoint other than the default (for CRIS/Custom Speech Service or if your speech service created a unique endpoint), create a new `Endpoint` with the host, path, and other details:

```c#
speechClient.SpeechEndpoint = new Endpoint ("westus.stt.speech.microsoft.com", "/speech/recognition");
```

To change the endpoint the authentication call will be made to (for token auth only), create a new `Endpoint` with the host, path, and other details:
    
```c#
speechClient.AuthEndpoint = new Endpoint ("westus.api.cognitive.microsoft.com", "/sts/v1.0/issueToken");
```

	
# Contributing

Contributions are welcome and encouraged.  Feel free to file issues and pull requests on the repo and I'll address them as time permits.


# About

- Created by [Nate Rickard](https://github.com/naterickard)

## License

Licensed under the MIT License (MIT). See [LICENSE](LICENSE) for details.

`PushStreamContent` class from the excellent [Refit](https://github.com/paulcbetts/refit) library by Paul Betts and licensed under the Apache License, Version 2.0 (original license from Microsoft).
