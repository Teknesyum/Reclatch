using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Reclatch.App.Localization;
using Reclatch.Core.Capture;

namespace Reclatch.App;

public partial class MainWindow : Window
{
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;
    private const int DwmwcpDoNotRound = 1;

    private const string GithubUrl = "https://github.com/Teknesyum";
    private const string SponsorUrl = "https://github.com/sponsors/Teknesyum";

    private readonly MonitorCapture _capture = new();
    private IntPtr _handle;

    public MainWindow()
    {
        InitializeComponent();

        _capture.FrameArrived += OnFrameArrived;
        Strings.LanguageChanged += ApplyLanguage;

        SourceInitialized += OnSourceInitialized;
        Activated += (_, _) => UpdateTitleFocus(true);
        Deactivated += (_, _) => UpdateTitleFocus(false);
        StateChanged += (_, _) => ApplyCornerPreference();
        Closed += OnWindowClosed;

        ApplyLanguage();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _handle = new WindowInteropHelper(this).Handle;
        ApplyCornerPreference();
        HwndSource.FromHwnd(_handle)?.AddHook(WindowProc);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _capture.FrameArrived -= OnFrameArrived;
        Strings.LanguageChanged -= ApplyLanguage;
        _capture.Dispose();
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmGetMinMaxInfo = 0x0024;
        if (msg != WmGetMinMaxInfo) return IntPtr.Zero;

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero) return IntPtr.Zero;

        var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return IntPtr.Zero;

        var mmi = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        mmi.ptMaxPosition.X = info.rcWork.Left - info.rcMonitor.Left;
        mmi.ptMaxPosition.Y = info.rcWork.Top - info.rcMonitor.Top;
        mmi.ptMaxSize.X = info.rcWork.Right - info.rcWork.Left;
        mmi.ptMaxSize.Y = info.rcWork.Bottom - info.rcWork.Top;
        Marshal.StructureToPtr(mmi, lParam, true);

        handled = true;
        return IntPtr.Zero;
    }

    private void ApplyCornerPreference()
    {
        if (_handle == IntPtr.Zero) return;
        var preference = WindowState == WindowState.Maximized ? DwmwcpDoNotRound : DwmwcpRound;
        try
        {
            DwmSetWindowAttribute(_handle, DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
    }

    private void ApplyLanguage()
    {
        Title = Strings.Get("app.title");
        TitleText.Text = Strings.Get("app.title");
        Heading.Text = Strings.Get("smoke.heading");
        Intro.Text = Strings.Get("smoke.intro");
        Purpose.Text = Strings.Get("smoke.purpose");
        FramesLabel.Text = Strings.Get("label.frames");
        ResolutionLabel.Text = Strings.Get("label.resolution");
        FpsLabel.Text = Strings.Get("label.fps");
        LanguageButton.Content = Strings.Get("btn.language");
        SupportButton.Content = Strings.Get("btn.support");

        MinimizeButton.ToolTip = Strings.Get("caption.minimize");
        MaximizeButton.ToolTip = Strings.Get("caption.maximize");
        CloseButton.ToolTip = Strings.Get("caption.close");

        UpdateToggleButton();
        UpdateState();
    }

    private void UpdateToggleButton()
    {
        var supported = MonitorCapture.IsSupported;
        ToggleButton.IsEnabled = supported;
        ToggleButton.Content = Strings.Get(_capture.IsRunning ? "btn.stop" : "btn.start");
    }

    private void UpdateState()
    {
        if (!MonitorCapture.IsSupported)
        {
            StateValue.Text = Strings.Get("state.unsupported");
            StateDot.Fill = (Brush)FindResource("NeonPink");
            return;
        }

        StateValue.Text = Strings.Get(_capture.IsRunning ? "state.running" : "state.idle");
        StateDot.Fill = _capture.IsRunning
            ? (Brush)FindResource("NeonSuccess")
            : (Brush)FindResource("TextHint");
    }

    private void UpdateTitleFocus(bool focused)
        => TitleText.Foreground = focused ? (Brush)FindResource("NeonBlue") : (Brush)FindResource("TextBody");

    private void OnFrameArrived(CaptureStats stats)
        => Dispatcher.BeginInvoke(() =>
        {
            FramesValue.Text = stats.Frames.ToString("N0", Strings.Culture);
            ResolutionValue.Text = $"{stats.Width}×{stats.Height}";
            FpsValue.Text = stats.Fps.ToString("F1", Strings.Culture);
        });

    private void OnToggleCapture(object sender, RoutedEventArgs e)
    {
        if (_capture.IsRunning)
        {
            _capture.Stop();
        }
        else
        {
            try
            {
                _capture.Start(new WindowInteropHelper(this).Handle);
            }
            catch (NotSupportedException)
            {
                ShowError(Strings.Get("error.unsupported"));
            }
            catch (Exception ex)
            {
                ShowError(Strings.Get("error.failed", ("message", ex.Message)));
            }
        }

        UpdateToggleButton();
        UpdateState();
    }

    private void ShowError(string message)
    {
        Heading.Text = message;
        Heading.Foreground = (Brush)FindResource("NeonPink");
    }

    private void OnToggleLanguage(object sender, RoutedEventArgs e)
        => Strings.Use(Strings.Current == "tr" ? "en" : "tr");

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnOpenSupport(object sender, RoutedEventArgs e) => OpenUrl(SponsorUrl);

    private void OnOpenGithub(object sender, RoutedEventArgs e) => OpenUrl(GithubUrl);

    private static void OpenUrl(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    private const uint MonitorDefaultToNearest = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point ptReserved;
        public Point ptMaxSize;
        public Point ptMaxPosition;
        public Point ptMinTrackSize;
        public Point ptMaxTrackSize;
    }
}
