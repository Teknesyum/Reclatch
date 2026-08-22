using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Reclatch.Core.Audio;

public readonly record struct AudioLevel(AudioKind Kind, double Peak, long Bytes, int SampleRate, int Channels);

public sealed class AudioTrack : IDisposable
{
    private readonly AudioKind _kind;
    private readonly object _gate = new();

    private WasapiCapture? _capture;
    private MMDevice? _device;
    private WaveFormatEncoding _samples = WaveFormatEncoding.Unknown;
    private long _bytes;
    private double _peak;

    public AudioTrack(AudioKind kind) => _kind = kind;

    public AudioKind Kind => _kind;

    public bool IsRunning { get; private set; }

    public WaveFormat? Format { get; private set; }

    public event Action<AudioLevel>? LevelChanged;

    public event Action<AudioKind, Exception>? Failed;

    public void Start(string? deviceId)
    {
        lock (_gate)
        {
            if (IsRunning) return;

            _device = AudioDevices.Open(_kind, deviceId);
            _capture = _kind == AudioKind.System
                ? new WasapiLoopbackCapture(_device)
                : new WasapiCapture(_device);

            Format = _capture.WaveFormat;
            _samples = SampleEncoding(Format);
            _bytes = 0;
            _peak = 0;

            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;
            _capture.StartRecording();
            IsRunning = true;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (_capture is not null)
            {
                _capture.DataAvailable -= OnDataAvailable;
                _capture.RecordingStopped -= OnRecordingStopped;
                try
                {
                    _capture.StopRecording();
                }
                catch (Exception)
                {
                }

                _capture.Dispose();
                _capture = null;
            }

            _device?.Dispose();
            _device = null;
            IsRunning = false;
        }

        LevelChanged?.Invoke(new AudioLevel(_kind, 0, _bytes, Format?.SampleRate ?? 0, Format?.Channels ?? 0));
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;

        Interlocked.Add(ref _bytes, e.BytesRecorded);
        _peak = Peak(e.Buffer, e.BytesRecorded, Format, _samples);

        LevelChanged?.Invoke(new AudioLevel(
            _kind,
            _peak,
            Interlocked.Read(ref _bytes),
            Format?.SampleRate ?? 0,
            Format?.Channels ?? 0));
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null) Failed?.Invoke(_kind, e.Exception);
    }

    private static WaveFormatEncoding SampleEncoding(WaveFormat? format)
    {
        if (format is null) return WaveFormatEncoding.Unknown;

        var resolved = format is WaveFormatExtensible extensible
            ? extensible.ToStandardWaveFormat()
            : format;

        if (resolved.Encoding == WaveFormatEncoding.IeeeFloat) return WaveFormatEncoding.IeeeFloat;
        if (resolved.Encoding == WaveFormatEncoding.Pcm) return WaveFormatEncoding.Pcm;
        return resolved.BitsPerSample == 32 ? WaveFormatEncoding.IeeeFloat : WaveFormatEncoding.Pcm;
    }

    private static double Peak(byte[] buffer, int count, WaveFormat? format, WaveFormatEncoding samples)
    {
        if (format is null) return 0;

        var peak = 0d;

        if (samples == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            for (var i = 0; i + 3 < count; i += 4)
            {
                var sample = Math.Abs(BitConverter.ToSingle(buffer, i));
                if (sample > peak) peak = sample;
            }
        }
        else if (format.BitsPerSample == 16)
        {
            for (var i = 0; i + 1 < count; i += 2)
            {
                var sample = Math.Abs(BitConverter.ToInt16(buffer, i) / 32768d);
                if (sample > peak) peak = sample;
            }
        }
        else if (format.BitsPerSample == 32)
        {
            for (var i = 0; i + 3 < count; i += 4)
            {
                var sample = Math.Abs(BitConverter.ToInt32(buffer, i) / 2147483648d);
                if (sample > peak) peak = sample;
            }
        }

        return Math.Min(peak, 1d);
    }

    public void Dispose() => Stop();
}
