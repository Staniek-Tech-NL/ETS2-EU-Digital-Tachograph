using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ETS2Tachograph.Desktop;

public partial class MainWindow : Window
{
    private const int LegacySlot1HotkeyId = 0x5100;
    private const int Slot1HotkeyId = 0x5101;
    private const int Slot2HotkeyId = 0x5102;
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModNoRepeat = 0x4000;
    private const uint KeyQ = 0x51;
    private const uint Key1 = 0x31;
    private const uint Key2 = 0x32;

    private readonly OverlayViewModel _overlayViewModel1;
    private readonly OverlayViewModel _overlayViewModel2;
    private readonly OverlayWindow _overlay1;
    private readonly OverlayWindow _overlay2;
    private DateTime _driver1PressedAt;
    private DateTime _driver2PressedAt;
    private HwndSource? _windowSource;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _overlayViewModel1 = new OverlayViewModel(viewModel, 1);
        _overlayViewModel2 = new OverlayViewModel(viewModel, 2);
        _overlay1 = new OverlayWindow(1) { DataContext = _overlayViewModel1 };
        _overlay2 = new OverlayWindow(2) { DataContext = _overlayViewModel2 };
        SourceInitialized += RegisterOverlayHotkey;
        Closed += (_, _) => CleanupOverlay();
    }

    private void Driver1Button_Down(object sender, MouseButtonEventArgs e) => _driver1PressedAt = DateTime.UtcNow;
    private void Driver2Button_Down(object sender, MouseButtonEventArgs e) => _driver2PressedAt = DateTime.UtcNow;

    private void Driver1Button_Up(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        var command = DateTime.UtcNow - _driver1PressedAt >= TimeSpan.FromSeconds(3)
            ? viewModel.EjectCardCommand : viewModel.Driver1ActivityCommand;
        if (command.CanExecute(null)) command.Execute(null);
    }

    private void Driver2Button_Up(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        var command = DateTime.UtcNow - _driver2PressedAt >= TimeSpan.FromSeconds(3)
            ? viewModel.EjectCard2Command : viewModel.Driver2ActivityCommand;
        if (command.CanExecute(null)) command.Execute(null);
    }

    private void RegisterOverlayHotkey(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _windowSource = HwndSource.FromHwnd(handle);
        _windowSource?.AddHook(WindowHook);
        var modifiers = ModAlt | ModNoRepeat;
        RegisterHotKey(handle, LegacySlot1HotkeyId, modifiers, KeyQ);
        RegisterHotKey(handle, Slot1HotkeyId, modifiers, Key1);
        RegisterHotKey(handle, Slot2HotkeyId, modifiers, Key2);
    }

    private IntPtr WindowHook(IntPtr window, int message, IntPtr wordParameter, IntPtr longParameter, ref bool handled)
    {
        if (message != WmHotkey) return IntPtr.Zero;

        switch (wordParameter.ToInt32())
        {
            case LegacySlot1HotkeyId:
            case Slot1HotkeyId:
                ToggleOverlay(_overlay1);
                handled = true;
                break;
            case Slot2HotkeyId:
                ToggleOverlay(_overlay2);
                handled = true;
                break;
        }

        return IntPtr.Zero;
    }

    private static void ToggleOverlay(Window overlay)
    {
        if (overlay.IsVisible) overlay.Hide();
        else overlay.Show();
    }

    private void CleanupOverlay()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            UnregisterHotKey(handle, LegacySlot1HotkeyId);
            UnregisterHotKey(handle, Slot1HotkeyId);
            UnregisterHotKey(handle, Slot2HotkeyId);
        }
        _windowSource?.RemoveHook(WindowHook);
        _overlay1.Close();
        _overlay2.Close();
        _overlayViewModel1.Dispose();
        _overlayViewModel2.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr window, int id);
}
