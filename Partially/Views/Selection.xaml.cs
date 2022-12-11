using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Partially.Model;
using Partially.Native;
using Partially.Util;
using Monitor = Partially.Model.Monitor;

namespace Partially.Views;

public partial class Selection : Window
{
    public RoutedUICommand SelectCommand { get; set; } = new();
    
    /// <summary>
    /// Keyboard and mouse hooks helper.
    /// </summary>
    private readonly InputHook _actHook;

    private Monitor _currentMonitor;
    private Rect _selection;
    private bool _canChange;

    public Selection()
    {
        InitializeComponent();

        try
        {
            _actHook = new InputHook(true, true); //true for the mouse, true for the keyboard.
            _actHook.KeyDown += KeyHookTarget;
        }
        catch (Exception) { }

        CommandBindings.Clear();
        CommandBindings.AddRange(new CommandBindingCollection
        {
            new CommandBinding(SelectCommand, async (_, _) => await RequestRegionSelection(), (_, args) => args.CanExecute = true)
        });
    }

    //Transparent window, click through
    //Possible of being selected by the browser share window option
    //Possible to be selected
    //Possible to be resizable 
    //Re-select or resize should alter the size, without creating new window

    private async void Selection_Loaded(object sender, RoutedEventArgs e)
    {
        await RequestRegionSelection();
    }

    private void KeyHookTarget(object sender, CustomKeyEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift))
        {
            //Enable manipulation.
            _canChange = true;
            return;
        }

        //Disable manipulation.
        _canChange = false;
    }

    private void Selection_Closing(object sender, CancelEventArgs e)
    {
        try
        {
            if (_actHook != null)
            {
                _actHook.KeyDown -= KeyHookTarget;
                _actHook.Stop(); //Stop the user activity watcher.
            }
        }
        catch (Exception) { }
    }

    private static TaskCompletionSource<(Monitor, Rect)> _taskCompletionSource;

    private static readonly List<RegionSelector> Selectors = new();

    private async Task RequestRegionSelection()
    {
        var (monitor, selection) = await SelectRegion();

        if (selection != Rect.Empty)
        {
            _currentMonitor = monitor;
            _selection = selection;

            this.MoveToScreen(_currentMonitor);

            Left = _selection.Left;
            Top = _selection.Top;
            Width = _selection.Width;
            Height = _selection.Height;
        }
    }

    private Task<(Monitor, Rect)> SelectRegion()
    {
        Selectors.Clear();

        WindowState = WindowState.Minimized;

        var monitors = MonitorHelper.AllMonitorsGranular();

        foreach (var monitor in monitors)
        {
            var selector = new RegionSelector();
            selector.Select(monitor, SelectionAccepted, RegionGotHover, SelectionAborted);

            Selectors.Add(selector);
        }

        //Return only when the region gets selected.
        _taskCompletionSource = new TaskCompletionSource<(Monitor, Rect)>();

        return _taskCompletionSource.Task;
    }

    private void SelectionAccepted(Monitor monitor, Rect region)
    {
        foreach (var selector in Selectors)
            selector.Close();

        WindowState = WindowState.Normal;
        Activate();

        _taskCompletionSource.SetResult((monitor, region));
    }

    private void RegionGotHover(Monitor monitor)
    {
        //When one monitor gets the focus, the other ones should be cleaned.
        foreach (var selector in Selectors)//.Where(w => w.Monitor.Handle != monitor.Handle))
            selector.ClearHoverEffects();
    }
    
    private void SelectionAborted()
    {
        foreach (var selector in Selectors)
            selector.Close();

        WindowState = WindowState.Normal;
        Activate();

        _taskCompletionSource.SetResult((null, Rect.Empty));
    }
}