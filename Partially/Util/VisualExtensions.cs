using System.Windows.Media;
using System.Windows;

namespace Partially.Util;

public static class VisualHelper
{
    /// <summary>
    /// Gets the scale of the current window.
    /// </summary>
    /// <param name="window">The Window.</param>
    /// <returns>The scale of the given Window.</returns>
    public static double GetVisualScale(this Visual window)
    {
        var source = PresentationSource.FromVisual(window);

        return source?.CompositionTarget != null ? source.CompositionTarget.TransformToDevice.M11 : 1d;
    }
}