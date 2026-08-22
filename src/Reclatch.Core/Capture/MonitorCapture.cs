using System.Diagnostics;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace Reclatch.Core.Capture;

public readonly record struct CaptureStats(long Frames, int Width, int Height, double Fps);

public sealed class MonitorCapture : IDisposable
{
    private const int BufferedFrames = 2;

    private readonly Stopwatch _clock = new();
    private ID3D11Device? _d3dDevice;
    private IDirect3DDevice? _winrtDevice;
    private Direct3D11CaptureFramePool? _framePool;
    private GraphicsCaptureSession? _session;
    private GraphicsCaptureItem? _item;
    private long _frames;
    private int _width;
    private int _height;

    public static bool IsSupported => GraphicsCaptureSession.IsSupported();

    public event Action<CaptureStats>? FrameArrived;

    public bool IsRunning => _session is not null;

    public void Start(IntPtr ownerWindow)
    {
        if (IsRunning) return;
        if (!IsSupported) throw new NotSupportedException("Windows.Graphics.Capture bu makinede desteklenmiyor.");

        var monitor = CaptureInterop.MonitorFromWindow(ownerWindow, CaptureInterop.MonitorDefaultToPrimary);
        _item = CaptureInterop.CreateItemForMonitor(monitor);

        D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            null,
            out _d3dDevice).CheckError();

        using var dxgiDevice = _d3dDevice!.QueryInterface<IDXGIDevice>();
        _winrtDevice = CaptureInterop.CreateDirect3DDevice(dxgiDevice.NativePointer);

        _width = _item.Size.Width;
        _height = _item.Size.Height;
        _frames = 0;
        _clock.Restart();

        _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            _winrtDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            BufferedFrames,
            _item.Size);

        _framePool.FrameArrived += OnFrameArrived;
        _item.Closed += OnItemClosed;

        _session = _framePool.CreateCaptureSession(_item);
        _session.StartCapture();
    }

    public void Stop()
    {
        if (_framePool is not null) _framePool.FrameArrived -= OnFrameArrived;
        if (_item is not null) _item.Closed -= OnItemClosed;

        _session?.Dispose();
        _framePool?.Dispose();
        _winrtDevice?.Dispose();
        _d3dDevice?.Dispose();

        _session = null;
        _framePool = null;
        _winrtDevice = null;
        _d3dDevice = null;
        _item = null;
        _clock.Stop();
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using var frame = sender.TryGetNextFrame();
        if (frame is null) return;

        _width = frame.ContentSize.Width;
        _height = frame.ContentSize.Height;
        var frames = Interlocked.Increment(ref _frames);

        var seconds = _clock.Elapsed.TotalSeconds;
        var fps = seconds > 0 ? frames / seconds : 0;
        FrameArrived?.Invoke(new CaptureStats(frames, _width, _height, fps));
    }

    private void OnItemClosed(GraphicsCaptureItem sender, object args) => Stop();

    public void Dispose() => Stop();
}
