using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Partially.Model;
using Partially.Util;

namespace Partially.Controls;

[TemplatePart(Name = MainCanvasId, Type = typeof(Canvas))]
[TemplatePart(Name = SelectBorderId, Type = typeof(Border))]
[TemplatePart(Name = SizeBorderId, Type = typeof(Border))]
[TemplatePart(Name = ZoomBorderId, Type = typeof(Border))]
public class ScreenSelector : Control
{
    private const string MainCanvasId = "MainCanvas";
    private const string SelectBorderId = "SelectBorder";
    private const string SizeBorderId = "SizeBorder";
    private const string ZoomBorderId = "ZoomBorder";

    #region Variables

    /// <summary>
    /// The main canvas, the root element.
    /// </summary>
    private Canvas _mainCanvas;

    /// <summary>
    /// The grids that holds the zoomed image and size info.
    /// </summary>
    private Border _sizeBorder;

    /// <summary>
    /// The element that displays a zoomed in image.
    /// </summary>
    private Border _zoomBorder;
    
    /// <summary>
    /// The zoomed image.
    /// </summary>
    private Image _croppedImage;

    /// <summary>
    /// The textblock that lies at the bottom of the zoom view.
    /// </summary>
    private TextBlock _zoomTextBlock;

    /// <summary>
    /// The start point for the drag operation.
    /// </summary>
    private Point _startPoint;

    /// <summary>
    /// The latest window that contains the mouse cursor on top of it.
    /// </summary>
    private DetectedRegion _hitTestWindow;

    /// <summary>
    /// True when this control is ready to process mouse input when using the Screen/Window selection mode.
    /// This was added because the event MouseMove was being fired before the method that adjusts the other controls finished. (TL;DR It was a race condition)
    /// </summary>
    private bool _ready;

    /// <summary>
    /// True if the hover focus was changed to this selector.
    /// Other selectors must lose the hover focus.
    /// This makes the zoom view to be hidden everywhere else.
    /// </summary>
    private bool _wasHoverFocusChanged;
    
    public BitmapSource BackgroundImage;

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty ParentLeftProperty = DependencyProperty.Register(nameof(ParentLeft), typeof(double), typeof(ScreenSelector), new PropertyMetadata(0d));

    public static readonly DependencyProperty ParentTopProperty = DependencyProperty.Register(nameof(ParentTop), typeof(double), typeof(ScreenSelector), new PropertyMetadata(0d));

    public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(Rect), typeof(ScreenSelector), new PropertyMetadata(Rect.Empty, Selected_PropertyChanged));

    public static readonly DependencyProperty NonExpandedSelectionProperty = DependencyProperty.Register(nameof(NonExpandedSelection), typeof(Rect), typeof(ScreenSelector), new PropertyMetadata(Rect.Empty));

    public static readonly DependencyProperty NonExpandedNativeSelectionProperty = DependencyProperty.Register(nameof(NonExpandedNativeSelection), typeof(Rect), typeof(ScreenSelector), new PropertyMetadata(Rect.Empty));

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(nameof(Scale), typeof(double), typeof(ScreenSelector), new PropertyMetadata(1d));

    public static readonly DependencyProperty ExternalRectProperty = DependencyProperty.Register(nameof(ExternalRect), typeof(Rect), typeof(ScreenSelector), new PropertyMetadata(Rect.Empty));

    public static readonly RoutedEvent MouseHoveringEvent = EventManager.RegisterRoutedEvent(nameof(MouseHovering), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ScreenSelector));

    public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(nameof(SelectionChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ScreenSelector));

    public static readonly RoutedEvent SelectionAcceptedEvent = EventManager.RegisterRoutedEvent(nameof(SelectionAccepted), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ScreenSelector));

    #endregion

    #region Properties

    public double ParentLeft
    {
        get => (double)GetValue(ParentLeftProperty);
        set => SetValue(ParentLeftProperty, value);
    }

    public double ParentTop
    {
        get => (double)GetValue(ParentTopProperty);
        set => SetValue(ParentTopProperty, value);
    }

    public Rect Selected
    {
        get => (Rect)GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    public Rect NonExpandedSelection
    {
        get => (Rect)GetValue(NonExpandedSelectionProperty);
        set => SetValue(NonExpandedSelectionProperty, value);
    }

    public Rect NonExpandedNativeSelection
    {
        get => (Rect)GetValue(NonExpandedNativeSelectionProperty);
        set => SetValue(NonExpandedNativeSelectionProperty, value);
    }
    
    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public Rect ExternalRect
    {
        get => (Rect)GetValue(ExternalRectProperty);
        set => SetValue(ExternalRectProperty, value);
    }

    public event RoutedEventHandler MouseHovering
    {
        add => AddHandler(MouseHoveringEvent, value);
        remove => RemoveHandler(MouseHoveringEvent, value);
    }

    public event RoutedEventHandler SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    public event RoutedEventHandler SelectionAccepted
    {
        add => AddHandler(SelectionAcceptedEvent, value);
        remove => RemoveHandler(SelectionAcceptedEvent, value);
    }

    #endregion

    static ScreenSelector()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ScreenSelector), new FrameworkPropertyMetadata(typeof(ScreenSelector)));
    }

    #region Overrides

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _mainCanvas = GetTemplateChild(MainCanvasId) as Canvas;
        _sizeBorder = GetTemplateChild(SizeBorderId) as Border;
        _zoomBorder = GetTemplateChild(ZoomBorderId) as Border;

        _croppedImage = Template.FindName("CroppedImage", this) as Image;
        _zoomTextBlock = Template.FindName("ZoomTextBlock", this) as TextBlock;

        Loaded += Control_Loaded;
        Unloaded += Control_Unloaded;

        SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();

        _startPoint = e.GetPosition(this);

        Selected = new Rect(e.GetPosition(this), new Size(0, 0));

        CaptureMouse();
        RaiseChangedEvent();

        e.Handled = true;
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        //When canceling the selection while the left button is still down, the starting point needs to be reset.
        _startPoint = e.GetPosition(this);

        Retry();

        e.Handled = true;
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var current = e.GetPosition(this);

        AdjustZoomView(current);

        if (!IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
            return;

        if (Keyboard.IsKeyDown(Key.Space))
        {
            //Detect if area selection started from the top/left vs bottom/right.
            var isRightToLeft = _startPoint.X > Selected.X;
            var isBottomToTop = _startPoint.Y > Selected.Y;

            //Move the whole selection along with the mouse.
            var x = isRightToLeft ? current.X : current.X - Selected.Width;
            var y = isBottomToTop ? current.Y : current.Y - Selected.Height;

            //Limit movement to bounds.
            if (x < -1)
                x = -1;

            if (y < -1)
                y = -1;

            if (x + Selected.Width > ActualWidth + 1)
                x = ActualWidth + 1 - Selected.Width;

            if (y + Selected.Height > ActualHeight + 1)
                y = ActualHeight + 1 - Selected.Height;

            Selected = Selected with { X = x, Y = y };
            _startPoint = new Point(isRightToLeft ? Selected.Right : x, isBottomToTop ? Selected.Bottom : y);

            AdjustInfo(current);
        }
        else
        {
            //Move 1 pixel to current the position of the selection to the cursor.
            current.X++;
            current.Y++;

            if (current.X < -1)
                current.X = -1;

            if (current.Y < -1)
                current.Y = -1;

            if (current.X > ActualWidth)
                current.X = ActualWidth;

            if (current.Y > ActualHeight)
                current.Y = ActualHeight;

            Selected = new Rect(Math.Min(current.X, _startPoint.X), Math.Min(current.Y, _startPoint.Y), Math.Abs(current.X - _startPoint.X), Math.Abs(current.Y - _startPoint.Y));

            AdjustInfo(current);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();

        if (Selected.Width < 30 || Selected.Height < 30)
        {
            Retry();
            return;
        }

        RaiseAcceptedEvent();

        //e.Handled = true;
        base.OnMouseLeftButtonUp(e);
    }

    #endregion

    #region Methods

    private void AdjustZoomView(Point point)
    {
        if (BackgroundImage == null || (Selected.Width > 10 && Selected.Height > 10 && Selected.Offset(5).Contains(point)))
        {
            _zoomBorder.Visibility = Visibility.Hidden;
            return;
        }

        //If this selector got the hover, the other selectors must hide their zoom views.
        if (!_wasHoverFocusChanged)
        {
            _wasHoverFocusChanged = true;
            RaiseMouseHoveringEvent();
        }

        var scaledPoint = point.Scale(Scale);
        var scaledSize = (int)Math.Round(15 * Scale, MidpointRounding.AwayFromZero);

        try
        {
            //When using multiple monitors, the mouse cursor can paqss to another screen. This makes sure that to only get a valid screen position.
            if (scaledPoint.X < 0 || scaledPoint.Y < 0 || scaledPoint.X + scaledSize > BackgroundImage.PixelWidth || scaledPoint.Y + scaledSize > BackgroundImage.PixelHeight)
            {
                _zoomBorder.Visibility = Visibility.Hidden;
                return;
            }

            //The image is already 7 pixels offset of the current position.
            _croppedImage.Source = new CroppedBitmap(BackgroundImage, new Int32Rect((int)scaledPoint.X, (int)scaledPoint.Y, scaledSize, scaledSize));
        }
        catch (Exception)
        {
            //Ignored
        }

        var left = point.X + 20;
        var top = point.Y - _zoomBorder.ActualHeight - 20;

        //Right overflow, adjust to the left.
        if (ActualWidth - point.X < _zoomBorder.ActualWidth + 20)
            left = point.X - _zoomBorder.ActualWidth - 20;

        //Top overflow, adjust to the bottom.
        if (point.Y - _zoomBorder.ActualHeight - 20 < 0)
            top = point.Y + 20;

        Canvas.SetLeft(_zoomBorder, left);
        Canvas.SetTop(_zoomBorder, top);

        _zoomTextBlock.Text = $"X: {Math.Round(point.X + ParentLeft, 2)} ◇ Y: {Math.Round(point.Y + ParentTop, 2)}";
        _zoomBorder.Visibility = Visibility.Visible;
    }
    
    private void AdjustInfo(Point? point = null)
    {
        if (_sizeBorder == null)
            return;

        if (point == null || Selected.IsEmpty || Selected.Width < _sizeBorder.ActualWidth || Selected.Height < _sizeBorder.ActualHeight)
        {
            _sizeBorder.Visibility = Visibility.Hidden;
            return;
        }

        //Out of 4 Points, get the one that is farthest from the current mouse position.
        var distances = new[] { (Selected.TopLeft - point.Value).Length, (Selected.TopRight - point.Value).Length, (Selected.BottomLeft - point.Value).Length, (Selected.BottomRight - point.Value).Length };
        var index = Array.IndexOf(distances, distances.Max());

        switch (index)
        {
            case 0:
                Canvas.SetTop(_sizeBorder, Selected.Top);
                Canvas.SetLeft(_sizeBorder, Selected.Left);
                break;
            case 1:
                Canvas.SetTop(_sizeBorder, Selected.Top);
                Canvas.SetLeft(_sizeBorder, Selected.Right - _sizeBorder.ActualWidth - _sizeBorder.Margin.Left - _sizeBorder.Margin.Right);
                break;
            case 2:
                Canvas.SetTop(_sizeBorder, Selected.Bottom - _sizeBorder.ActualHeight - _sizeBorder.Margin.Top - _sizeBorder.Margin.Bottom);
                Canvas.SetLeft(_sizeBorder, Selected.Left);
                break;
            case 3:
                Canvas.SetTop(_sizeBorder, Selected.Bottom - _sizeBorder.ActualHeight - _sizeBorder.Margin.Top - _sizeBorder.Margin.Bottom);
                Canvas.SetLeft(_sizeBorder, Selected.Right - _sizeBorder.ActualWidth - _sizeBorder.Margin.Left - _sizeBorder.Margin.Right);
                break;
        }

        _sizeBorder.Visibility = Visibility.Visible;
    }

    internal void Accept()
    {
        //Selected = Selected.Offset(-1);
        RaiseAcceptedEvent();
    }

    public void Retry()
    {
        Selected = Rect.Empty;

        AdjustInfo();
        HideZoom();
        RaiseChangedEvent();
    }

    public void HideZoom()
    {
        _wasHoverFocusChanged = false;
        _zoomBorder.Visibility = Visibility.Hidden;
    }


    public void RaiseMouseHoveringEvent()
    {
        if (MouseHoveringEvent == null || !IsLoaded)
            return;

        RaiseEvent(new RoutedEventArgs(MouseHoveringEvent));
    }

    public void RaiseChangedEvent()
    {
        if (SelectionChangedEvent == null || !IsLoaded)
            return;

        RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
    }

    public void RaiseAcceptedEvent()
    {
        if (SelectionAcceptedEvent == null || !IsLoaded)
            return;

        RaiseEvent(new RoutedEventArgs(SelectionAcceptedEvent));
    }

    #endregion

    #region Events

    public void Control_Loaded(object o, RoutedEventArgs routedEventArgs)
    {
        _ready = false;

        Keyboard.Focus(this);

        if (IsMouseOver)
            AdjustZoomView(Mouse.GetPosition(this));

        ExternalRect = new Rect(0, 0, ActualWidth, ActualHeight);

        _ready = true;

        //Triggers the mouse event to detect the mouse hit at start.
        OnMouseMove(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
    }

    private void SystemEvents_DisplaySettingsChanged(object o, EventArgs eventArgs)
    {
        Scale = this.GetVisualScale();
    }

    private void Control_Unloaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

        if (_mainCanvas == null)
            return;

        var list = _mainCanvas.Children.OfType<FrameworkElement>().Where(x => x.Tag as string == "T").ToList();

        foreach (var element in list)
            _mainCanvas.Children.Remove(element);
    }

    private static void Selected_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not ScreenSelector control)
            return;

        //If nothing selected, simply ignore.
        if (control.Selected.IsEmpty)
        {
            control.NonExpandedSelection = control.Selected;
            control.NonExpandedNativeSelection = control.Selected;
            return;
        }

        #region Region selection mode

        //For way too small regions, avoid applying the offset. That would throw an exception.
        if (control.Selected.Width < 5 || control.Selected.Height < 5)
        {
            control.NonExpandedSelection = control.Selected;
            control.NonExpandedNativeSelection = control.Selected;
            return;
        }

        control.NonExpandedSelection = control.Selected.Offset(1);
        control.NonExpandedNativeSelection = control.Selected.Scale(control.Scale).Offset(MathExtensions.RoundUpValue(control.Scale));

        #endregion
    }

    #endregion
}