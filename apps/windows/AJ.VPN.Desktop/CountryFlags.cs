using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace AJ.VPN.Desktop;

public static class CountryFlags
{
    private static readonly Dictionary<string, ImageSource> cache = [];
    public static ImageSource Get(string? country)
    {
        var code = country?.ToLowerInvariant() ?? "";
        if (cache.TryGetValue(code, out var value)) return value;
        if (code.Length == 2 && code.All(c => c is >= 'a' and <= 'z'))
        {
            try { var image = new BitmapImage(new Uri($"pack://application:,,,/Assets/Flags/{code}.png")); image.Freeze(); cache[code] = image; return image; }
            catch (Exception ex) when (ex is System.IO.IOException or System.IO.FileNotFoundException) { }
        }
        // Original neutral globe artwork; unknown metadata never acquires a country flag.
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(148,163,184)), 1.7);
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(null, pen, new EllipseGeometry(new System.Windows.Point(16,16),13,13)));
        group.Children.Add(new GeometryDrawing(null, pen, new EllipseGeometry(new System.Windows.Point(16,16),6,13)));
        group.Children.Add(new GeometryDrawing(null, pen, Geometry.Parse("M3,16 L29,16 M6,9 L26,9 M6,23 L26,23")));
        var globe = new DrawingImage(group); globe.Freeze(); cache[""] = globe; return globe;
    }
}
