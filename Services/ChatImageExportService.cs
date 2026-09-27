using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Chat.Services;

public static class ChatImageExportService
{
    public static void Export(FrameworkElement visual, string filePath)
    {
        visual.UpdateLayout();

        var width = Math.Max(visual.ActualWidth, visual.DesiredSize.Width);
        var height = Math.Max(visual.ActualHeight, visual.DesiredSize.Height);

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException("The chat view has no renderable content.");

        var bitmap = new RenderTargetBitmap(
            Math.Max(1, (int)Math.Ceiling(width)),
            Math.Max(1, (int)Math.Ceiling(height)),
            96,
            96,
            PixelFormats.Pbgra32);

        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }
}