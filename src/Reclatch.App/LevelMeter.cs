using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Reclatch.App;

public sealed class LevelMeter
{
    private const int Segments = 18;
    private const int HotSegment = 14;
    private const double Decay = 0.04;
    private const double FloorDb = -60;

    private readonly Border[] _cells = new Border[Segments];
    private readonly Brush _idle;
    private readonly Brush _lit;
    private readonly Brush _hot;
    private readonly Color _litColor;

    private double _shown;

    public LevelMeter(StackPanel host, Brush idle, Brush lit, Brush hot, Color litColor)
    {
        _idle = idle;
        _lit = lit;
        _hot = hot;
        _litColor = litColor;

        host.Children.Clear();
        for (var i = 0; i < Segments; i++)
        {
            var cell = new Border
            {
                Width = 5,
                Height = 20,
                Margin = new Thickness(0, 0, 2, 0),
                CornerRadius = new CornerRadius(2),
                Background = _idle,
                SnapsToDevicePixels = true
            };
            _cells[i] = cell;
            host.Children.Add(cell);
        }
    }

    public void Set(double peak)
    {
        var value = Normalize(peak);
        _shown = value >= _shown ? value : Math.Max(value, _shown - Decay);

        var active = (int)Math.Ceiling(_shown * Segments);

        for (var i = 0; i < Segments; i++)
        {
            var on = i < active;
            var cell = _cells[i];

            if (!on)
            {
                cell.Background = _idle;
                cell.Effect = null;
                continue;
            }

            cell.Background = i >= HotSegment ? _hot : _lit;
            cell.Effect ??= new DropShadowEffect
            {
                Color = _litColor,
                BlurRadius = 8,
                ShadowDepth = 0,
                Opacity = 0.6
            };
        }
    }

    private static double Normalize(double peak)
    {
        var linear = Math.Clamp(peak, 0, 1);
        if (linear <= 0) return 0;

        var db = 20 * Math.Log10(linear);
        return Math.Clamp((db - FloorDb) / -FloorDb, 0, 1);
    }

    public void Clear()
    {
        _shown = 0;
        Set(0);
    }
}
