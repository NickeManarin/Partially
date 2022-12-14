using System.Windows.Input;
using Partially.Enums;

namespace Partially.Model;

/// <summary>
/// Custom mouse event arguments.
/// </summary>
public class SimpleMouseGesture
{
    #region Properties

    /// <summary>
    /// The type of the mouse event.
    /// </summary>
    public NativeMouseEvents EventType { get; }

    /// <summary>
    /// X Axis position
    /// </summary>
    public int PosX { get; }

    /// <summary>
    /// Y Axis position.
    /// </summary>
    public int PosY { get; }

    /// <summary>
    /// State of the left mouse button.
    /// </summary>
    public MouseButtonState LeftButton { get; }

    /// <summary>
    /// State of the right mouse button.
    /// </summary>
    public MouseButtonState RightButton { get; }

    /// <summary>
    /// State of the middle mouse button.
    /// </summary>
    public MouseButtonState MiddleButton { get; }

    /// <summary>
    /// State of the first extra mouse buttons.
    /// </summary>
    public MouseButtonState FirstExtraButton { get; }

    /// <summary>
    /// State of the second extra mouse buttons.
    /// </summary>
    public MouseButtonState SecondExtraButton { get; }

    /// <summary>
    /// The state of the scroll wheel. Up or down scroll flow.
    /// </summary>
    public short MouseDelta { get; }

    /// <summary>
    /// True if this event args is a registration of a mouse down event.
    /// </summary>
    public bool IsMouseDown => EventType is NativeMouseEvents.LeftButtonDown or NativeMouseEvents.RightButtonDown or NativeMouseEvents.MiddleButtonDown
        or NativeMouseEvents.ExtraButtonDown or NativeMouseEvents.OutsideExtraButtonDown;

    /// <summary>
    /// True if this event args is a registration of a mouse double click event.
    /// </summary>
    public bool IsMouseDoubleClick => EventType is NativeMouseEvents.LeftButtonDoubleClick or NativeMouseEvents.RightButtonDoubleClick or NativeMouseEvents.MiddleButtonDoubleClick
        or NativeMouseEvents.ExtraButtonDoubleClick or NativeMouseEvents.OutsideExtraButtonDoubleClick;

    /// <summary>
    /// True if the action of clicking or scrolling happened.
    /// </summary>
    public bool IsInteraction => IsMouseDown || IsMouseDoubleClick || MouseDelta != 0;

    /// <summary>
    /// True if the any button is being clicked.
    /// </summary>
    public bool IsClicked => LeftButton == MouseButtonState.Pressed || RightButton == MouseButtonState.Pressed || MiddleButton == MouseButtonState.Pressed;

    /// <summary>
    /// True if the scroll event is upwards.
    /// </summary>
    public bool IsScrollUp => EventType == NativeMouseEvents.MouseWheel && MouseDelta > 0;

    /// <summary>
    /// True if the scroll event is downwards.
    /// </summary>
    public bool IsScrollDown => EventType == NativeMouseEvents.MouseWheel && MouseDelta < 0;

    /// <summary>
    /// True if the scroll event is upwards.
    /// </summary>
    public bool IsScrollLeft => EventType == NativeMouseEvents.MouseWheelHorizontal && MouseDelta < 0;

    /// <summary>
    /// True if the scroll event is downwards.
    /// </summary>
    public bool IsScrollRight => EventType == NativeMouseEvents.MouseWheelHorizontal && MouseDelta > 0;

    #endregion

    public SimpleMouseGesture(NativeMouseEvents eventType, int x, int y, MouseButtonState left, MouseButtonState right, MouseButtonState middle, MouseButtonState firstExtra, MouseButtonState secondExtra, short mouseDelta = 0)
    {
        EventType = eventType;

        PosX = x;
        PosY = y;

        LeftButton = left;
        RightButton = right;
        MiddleButton = middle;
        FirstExtraButton = firstExtra;
        SecondExtraButton = secondExtra;
        MouseDelta = mouseDelta;
    }
}
