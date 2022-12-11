using System;
using System.Windows.Interop;
using System.Windows;
using Partially.Model;
using System.Runtime.InteropServices;
using Partially.Enums;

namespace Partially.Util;

public static class WindowExtensions
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags);

    public static void MoveToScreen(this Window window, Monitor next, bool fullScreen = false)
    {
        if (fullScreen)
        {
            SetWindowPos(new WindowInteropHelper(window).Handle, (IntPtr)SpecialWindowHandles.Top,
                (int)next.NativeBounds.Left, (int)next.NativeBounds.Top, (int)next.NativeBounds.Width, (int)next.NativeBounds.Height, SetWindowPosFlags.ShowWindow);
            return;
        }

        SetWindowPos(new WindowInteropHelper(window).Handle, (IntPtr)SpecialWindowHandles.Top,
            (int)next.NativeBounds.Left, (int)next.NativeBounds.Top, (int)window.Width, (int)window.Height, SetWindowPosFlags.ShowWindow);
    }
}