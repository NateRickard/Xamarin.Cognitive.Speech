# Xamarin.Cognitive.BingSpeech ![NuGet](https://img.shields.io/nuget/v/Xamarin.Cognitive.BingSpeech.svg?label=NuGet)

`Xamarin.Cognitive.BingSpeech` is a managed client library that makes it easy to work with the [Microsoft Cognitive Services Bing Speech API](https://azure.microsoft.com/en-us/services/cognitive-services/speech/) on Xamarin.iOS, Xamarin.Android, and Xamarin.Forms and/or Portable Class Library (PCL) projects.

Includes a Xamarin.Forms sample with iOS and Android apps.

Resources about the Bing Speech API and what it is:

- [Learn about the Bing Speech API](https://azure.microsoft.com/en-us/services/cognitive-services/speech/)
- [Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/home)


# Features

- 100% `HttpClient`-based
- Works with the latest version of the Bing Speech API
- Chunked transfer encoding for efficient audio streaming to server
- Ability to stream audio to the server as it's being recorded
- Easy to use API
- Works seamlessly with [Plugin.AudioRecorder](https://www.nuget.org/packages/Plugin.AudioRecorder/), a cross platform way to record device microphone input


# Setup

`Xamarin.Cognitive.BingSpeech` is available as a [NuGet package](https://www.nuget.org/packages/Xamarin.Cognitive.BingSpeech/) to be added to your Xamarin.iOS, Xamarin.Android, or Xamarin.Forms project(s).

The included PCL assembly is compiled against Profile 111, which is currently the same profile used for PCL-based Xamarin.Forms solutions.




## Usage




# Gotchas

	
# Contributing

Contributions are welcome and encouraged.  Feel free to file issues and pull requests on the repo and I'll address them as time permits.


# About

- Created by [Nate Rickard](https://github.com/naterickard)

## License

Licensed under the MIT License (MIT). See [LICENSE](LICENSE) for details.

`PushStreamContent` class from the excellent [Refit](https://github.com/paulcbetts/refit) library by Paul Betts and licensed under the Apache License, Version 2.0 (original license from Microsoft).