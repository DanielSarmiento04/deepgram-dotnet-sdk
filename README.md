# Deepgram .NET SDK

![Nuget](https://img.shields.io/nuget/v/deepgram) [![Build Status](https://github.com/deepgram-devs/deepgram-dotnet-sdk/workflows/CI/badge.svg)](https://github.com/deepgram-devs/deepgram-dotnet-sdk/actions?query=CI) [![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-v2.0%20adopted-ff69b4.svg)](CODE_OF_CONDUCT.md)

Official .NET SDK for [Deepgram](https://www.deepgram.com/)'s automated
speech recognition APIs.

> This SDK only supports hosted usage of api.deepgram.com.

To access the API you will need a Deepgram account. Sign up for free at
[signup](https://console.deepgram.com/signup?utm_medium=github&utm_source=DEVREL&utm_content=dotnet-sdk).

## Documentation

Full documentation of the .NET SDK can be found on the
[Deepgram Developer Portal](https://developers.deepgram.com/sdks-tools/sdks/dotnet-sdk/).

You can learn more about the full Deepgram API at [https://developers.deepgram.com](https://developers.deepgram.com).

## Installation

To install the C# client library using NuGet:

Run the following command from your terminal in your projects directory:

```bash
dotnet add package Deepgram
```

## Targeted Frameworks

- 6.0.0
- 5.0.0
- .NET Core 3.1
- .NET Standard 2.0

## Configuration

To setup the configuration of the Deepgram Client you can do one of the following:

- Create a Deepgram Client instance and pass in credentials in the constructor.

```csharp
var credentials = new Credentials(YOUR_DEEPGRAM_API_KEY);
var deepgramClient = new DeepgramClient(credentials);
```

Or

- Provide the Deepgram API key and optional API Url in `appsettings.json`:

```json
{
  "appSettings": {
    "Deepgram.Api.Key": "YOUR_DEEPGRAM_API_KEY",
    "Deepgram.Api.Uri": "api.deepgram.com"
  }
}
```

> Note: In the event multiple configuration files are found, the order of precedence is as follows:

```
* ```appsettings.json``` which overrides
* ```settings.json```
```

Or

- Access the Configuration instance and set the appropriate key in your code for example:

```csharp
Configuration.Instance.Settings["appSettings:Deepgram.Api.Key"] = "YOUR_DEEPGRAM_API_KEY";
Configuration.Instance.Settings["appSettings:Deepgram.Api.Uri"] = "api.deepgram.com";
```

## Examples

### Sending a Remote File for Transcription

```csharp
var credentials = new Credentials(DEEPGRAM_API_KEY);

var deepgramClient = new DeepgramClient(credentials);

var response = await deepgramClient.Transcription.Prerecorded.GetTranscriptionAsync(
    new Deepgram.Transcription.UrlSource("https://static.deepgram.com/examples/Bueller-Life-moves-pretty-fast.wav"),
    new Deepgram.Transcription.PrerecordedTranscriptionOptions()
    {
        Punctuate = true
    });
```

### Sending a Local File for Transcription

```csharp
var credentials = new Credentials(DEEPGRAM_API_KEY);

var deepgramClient = new DeepgramClient(credentials);

using (FileStream fs = File.OpenRead("path\\to\\file"))
{
    var response = await deepgramClient.Transcription.Prerecorded.GetTranscriptionAsync(
        new Deepgram.Transcription.StreamSource(
            fs,
            "audio/wav"),
        new Deepgram.Transcription.PrerecordedTranscriptionOptions()
        {
            Punctuate = true
        });
}
```

### Real-time Transcription

> The example below demonstrates sending a pre-recorded audio to simulate a real-time
stream of audio. In a real application, this type of audio is better handled using the
pre-recorded transcription.

```csharp
var credentials = new Credentials(DEEPGRAM_API_KEY);

var deepgramClient = new DeepgramClient(credentials);

using (var deepgramLive = deepgramClient.CreateLiveTranscriptionClient())
{
    deepgramLive.ConnectionOpened += HandleConnectionOpened;
    deepgramLive.ConnectionClosed += HandleConnectionClosed;
    deepgramLive.ConnectionError += HandleConnectionError;
    deepgramLive.TranscriptReceived += HandleTranscriptReceived;

    // Connection opened so start sending audio.
    async void HandleConnectionOpened(object? sender, ConnectionOpenEventArgs e)
    {
        byte[] buffer;

        using (FileStream fs = File.OpenRead("path\\to\\file"))
        {
            buffer = new byte[fs.Length];
            fs.Read(buffer, 0, (int)fs.Length);
        }

        var chunks = buffer.Chunk(1000);

        foreach (var chunk in chunks)
        {
            deepgramLive.SendData(chunk);
            await Task.Delay(50);
        }

        await deepgramLive.FinishAsync();
    }

    void HandleTranscriptReceived(object? sender, TranscriptReceivedEventArgs e)
    {
        if (e.Transcript.IsFinal && e.Transcript.Channel.Alternatives.First().Transcript.Length > 0) { 
            var transcript = e.Transcript;
            Console.WriteLine($"[Speaker: {transcript.Channel.Alternatives.First().Words.First().Speaker}] {transcript.Channel.Alternatives.First().Transcript}");
        }
    }

    void HandleConnectionClosed(object? sender, ConnectionClosedEventArgs e)
    {
        Console.Write("Connection Closed");
    }

    void HandleConnectionError(object? sender, ConnectionErrorEventArgs e)
    {
        Console.WriteLine(e.Exception.Message);
    }

    var options = new LiveTranscriptionOptions() { Punctuate = true, Diarize = true, Encoding = Deepgram.Common.AudioEncoding.Linear16 };
    await deepgramLive.StartConnectionAsync(options);

    while (deepgramLive.State() == WebSocketState.Open) { }
}
```
### Real-time Transcription (With device socket)
> The example below demonstrates sending a byte[] captured on th socket device.
```csharp
    string audioInputFile = Path.Combine(Directory.GetCurrentDirectory(), "test.wav");
    // get the enumeration of the audio-in devices
    var deviceEnum = new MMDeviceEnumerator();
    var device = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);

    Console.WriteLine($"Using input device: {device.FriendlyName}");
    // just in case some other program has muted it
    device.AudioEndpointVolume.Mute = false;

    // Setup the audio capture and output of NAudio.
    // Most important is the sample rate because we down sample the input signal
    IWaveIn _waveIn = new WasapiCapture(device);
    Console.WriteLine($"Waveformat from device");
    Console.WriteLine($"   Samplerate: {_waveIn.WaveFormat.SampleRate}");
    Console.WriteLine($"   Encoding: {_waveIn.WaveFormat.Encoding}");
    Console.WriteLine($"   Bits per sample: {_waveIn.WaveFormat.BitsPerSample}");
    Console.WriteLine($"   Channels: {_waveIn.WaveFormat.Channels}");
    var waveOutFormat = new WaveFormat(sampleRate: _waveIn.WaveFormat.SampleRate / 3, channels: 2);

    WaveFileWriter _waveWriter = new WaveFileWriter(audioInputFile, waveOutFormat);
    Console.WriteLine($"Saving audio input to: {audioInputFile}");

    // Setup the connection in the deepgram sdk. Put in the secret key of your API
    const string secret = "<Your secrect>";
    var credentials = new Credentials(secret);

    var deepgramClient = new DeepgramClient(credentials);
    var options = new LiveTranscriptionOptions()
    {
        Punctuate = true,
        Diarize = true,
        Encoding = Deepgram.Common.AudioEncoding.Linear16,
        Language = "es",//"es-419",
        Utterances = true,
        SampleRate = 44100 // 
    };

    var _deepgramLive = deepgramClient.CreateLiveTranscriptionClient();
    _deepgramLive.ConnectionClosed += (object? sender, ConnectionClosedEventArgs e) =>
    {
        Console.WriteLine("Deepgram Connection closed");
    };
    _deepgramLive.TranscriptReceived += (object? sender, TranscriptReceivedEventArgs e) =>
    {
        // Console.WriteLine($"Transcript received");
        {
            if (e.Transcript.IsFinal &&
                e.Transcript.Channel.Alternatives.First().Transcript.Length > 0)
            {
                var transcript = e.Transcript;
                Console.WriteLine($"Deepgram Recognition: {transcript.Channel.Alternatives.First().Transcript}");
            }
        }
    };


    _deepgramLive.ConnectionOpened += (object? sender, ConnectionOpenEventArgs e) =>
    {
        Console.WriteLine("Deepgram Connection opened");
    };

    _deepgramLive.ConnectionError += (object? sender, ConnectionErrorEventArgs e) =>
    {
        Console.WriteLine($"Deepgram Error: {e.Exception.Message}");
    };

    await _deepgramLive.StartConnectionAsync(options);

    _waveIn.DataAvailable += (object? sender, WaveInEventArgs e) =>
    {
        var convertedBuffer = Convert32BitTo16Bit(e.Buffer, e.BytesRecorded);
        // Console.WriteLine($"Numer of bytes: {e.BytesRecorded}");
        _deepgramLive.SendData(convertedBuffer);

        _waveWriter.Write(convertedBuffer, 0, convertedBuffer.Length);

        int secondsRecorded = (int)(_waveWriter.Length / _waveWriter.WaveFormat.AverageBytesPerSecond);
        // Console.WriteLine($"{secondsRecorded} Segundos");
        if (secondsRecorded >= 40)
        {
            Console.WriteLine("NAudio listening stopped automatically after 30 seconds!");
            _waveIn.StopRecording();
            _deepgramLive.FinishAsync();
        }
    };
    _waveIn.RecordingStopped += (object? sender, StoppedEventArgs e) =>
    {
        Console.WriteLine($"Waveformat from file (same that is send to Deepgram)");
        Console.WriteLine($"   Samplerate: {_waveWriter.WaveFormat.SampleRate}");
        Console.WriteLine($"   Encoding: {_waveWriter.WaveFormat.Encoding}");
        Console.WriteLine($"   Bits per sample: {_waveWriter.WaveFormat.BitsPerSample}");
        Console.WriteLine($"   Channels: {_waveWriter.WaveFormat.Channels}");
        _waveWriter.Close();
        _waveWriter.Dispose();
    };
    _waveIn.StartRecording();
```

```csharp
    private static byte[] Convert32BitTo16Bit(byte[] buffer, int length)
    {
        byte[] output = new byte[length / 2];
        byte[] resampledOutput = new byte[length / 6];
        WaveBuffer sourceWaveBuffer = new WaveBuffer(buffer);
        WaveBuffer outputWaveBuffer = new WaveBuffer(output);
        WaveBuffer resampledWaveBuffer = new WaveBuffer(resampledOutput);
        int resampledValueCounter = 0;
        int outputCounter = 0;
        var resampleSourceSamples = new short[3];
        for (int n = 0; n < length / 4; n += 1)
        {
            float sample32 = sourceWaveBuffer.FloatBuffer[n];
            if (sample32 > 1.0f) sample32 = 1.0f;
            if (sample32 < -1.0f) sample32 = -1.0f;
            outputWaveBuffer.ShortBuffer[n] = (short)(sample32 * 32767);
            resampleSourceSamples[resampledValueCounter] = (short)(sample32 * 32767);
            resampledValueCounter++;
            // Take averga of 3 data points and create the avergae to resample the audio data by 1/3
            if (resampledValueCounter == 3)
            {
                resampledValueCounter = 0;
                short resample = (short)((resampleSourceSamples[0] + resampleSourceSamples[1] + resampleSourceSamples[2]) / 3);
                resampledWaveBuffer.ShortBuffer[outputCounter] = resample;
                outputCounter++;
            }
        }
        return resampledOutput;
    }
```
## Logging

The Library uses Microsoft.Extensions.Logging to preform all of it's logging tasks. To configure
logging for your app simply create a new ILoggerFactory and call the LogProvider.SetLogFactory()
method to tell the Deepgram library how to log. For example, to log to the console with Serilog, you'd need to install the Serilog package with `dotnet add package Serilog` and then do the following:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Deepgram.Logger;
using Serilog;

var log = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm} [{Level}]: {Message}\n")
    .CreateLogger();
var factory = new LoggerFactory();
factory.AddSerilog(log);
LogProvider.SetLogFactory(factory);
```

## Development and Contributing

Interested in contributing? We ❤️ pull requests!

To make sure our community is safe for all, be sure to review and agree to our
[Code of Conduct](./CODE_OF_CONDUCT.md). Then see the
[Contribution](./CONTRIBUTING.md) guidelines for more information.

## Getting Help

We love to hear from you so if you have questions, comments or find a bug in the
project, let us know! You can either:

- [Open an issue](https://github.com/deepgram-devs/deepgram-dotnet-sdk/issues/new) on this repository
- Tweet at us! We're [@DeepgramDevs on Twitter](https://twitter.com/DeepgramDevs)

## Further Reading

Check out the Developer Documentation at [https://developers.deepgram.com/](https://developers.deepgram.com/)

[signup]: https://console.deepgram.com?utm_source=dotnet-sdk&utm_content=readme
