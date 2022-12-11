using System;
using System.Windows;
using System.Windows.Input;
using Partially.Model;
using Partially.Util;

namespace Partially.Views;

public partial class RegionSelector : Window
{
    private Action<Monitor, Rect> _selected;
    private Action<Monitor> _gotHover;
    private Action _aborted;

    private Monitor _monitor;

    public RegionSelector()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _aborted.Invoke();
            Close();
        }

        base.OnKeyDown(e);
    }

    public void Select(Monitor monitor,  Action<Monitor, Rect> selected, Action<Monitor> gotHover, Action aborted)
    {
        //Resize to fit given window.
        Left = monitor.Bounds.Left;
        Top = monitor.Bounds.Top;
        Width = monitor.Bounds.Width;
        Height = monitor.Bounds.Height;

        _monitor = monitor;

        _selected = selected;
        _gotHover = gotHover;
        _aborted = aborted;

        SelectElement.Scale = monitor.Scale;
        SelectElement.ParentLeft = Left;
        SelectElement.ParentTop = Top;
        //SelectElement.BackgroundImage = _viewModel.CaptureBackground();

        //if (false) //TODO: SelectionImprovement
        //{
        //    AllowsTransparency = false;
        //    Background = new ImageBrush(_viewModel.CaptureBackground(false));
        //}
        
        Show();

        this.MoveToScreen(monitor, true);
    }

    public void ClearHoverEffects()
    {
        SelectElement.HideZoom();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Activate();
    }
    
    private void SelectElement_MouseHovering(object sender, RoutedEventArgs e)
    {
        _gotHover.Invoke(_monitor);
    }

    private void SelectElement_SelectionAccepted(object sender, RoutedEventArgs e)
    {
        _selected.Invoke(_monitor, SelectElement.Selected.Translate(_monitor.Bounds.Left, _monitor.Bounds.Top));

        Close();
    }
}