using System;
using System.Windows;

namespace Partially.Model;

public class Monitor
{
    public override int GetHashCode() => Handle.GetHashCode();

    public IntPtr Handle { get; }

    public Rect Bounds { get; set; }

    public Rect NativeBounds { get; set; }

    public Rect WorkingArea { get; set; }

    public string Name { get; set; }

    public string AdapterName { get; set; }

    public string FriendlyName { get; set; }

    public int Dpi { get; set; }

    public double Scale => Dpi / 96d;

    public bool IsPrimary { get; set; }

    public Monitor(IntPtr handle)
    {
        Handle = handle;
    }

    public static bool operator !=(Monitor a, Monitor b) => a?.Handle != b?.Handle;

    public static bool operator ==(Monitor a, Monitor b) => a?.Handle == b?.Handle;

    protected bool Equals(Monitor other) => other != null && Handle.Equals(other.Handle);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((Monitor)obj);
    }
}