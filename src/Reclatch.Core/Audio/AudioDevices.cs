using NAudio.CoreAudioApi;

namespace Reclatch.Core.Audio;

public sealed record AudioDevice(string Id, string Name, bool IsDefault)
{
    public override string ToString() => Name;
}

public enum AudioKind
{
    System,
    Microphone
}

public static class AudioDevices
{
    public static IReadOnlyList<AudioDevice> List(AudioKind kind)
    {
        var flow = kind == AudioKind.System ? DataFlow.Render : DataFlow.Capture;
        using var enumerator = new MMDeviceEnumerator();

        var defaultId = DefaultId(enumerator, flow);
        var devices = new List<AudioDevice>();

        foreach (var device in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            using (device)
            {
                devices.Add(new AudioDevice(device.ID, device.FriendlyName, device.ID == defaultId));
            }
        }

        return devices;
    }

    internal static MMDevice Open(AudioKind kind, string? id)
    {
        var flow = kind == AudioKind.System ? DataFlow.Render : DataFlow.Capture;
        var enumerator = new MMDeviceEnumerator();

        if (string.IsNullOrEmpty(id))
            return enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);

        foreach (var device in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            if (device.ID == id) return device;
            device.Dispose();
        }

        return enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
    }

    private static string? DefaultId(MMDeviceEnumerator enumerator, DataFlow flow)
    {
        try
        {
            using var device = enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
            return device.ID;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
