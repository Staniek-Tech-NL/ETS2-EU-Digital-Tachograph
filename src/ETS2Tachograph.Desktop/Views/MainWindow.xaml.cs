using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNoTopmost = new(-2);

    private readonly OverlayViewModel _overlayViewModel1;
    private readonly OverlayViewModel _overlayViewModel2;
    private readonly OverlayWindow _overlay1;
    private readonly OverlayWindow _overlay2;
    private readonly MainWindowLevelController _windowLevelController;
    private DateTime _driver1PressedAt;
    private DateTime _driver2PressedAt;
    private string _countrySearchText = string.Empty;
    private DateTime _countrySearchUpdatedAtUtc = DateTime.MinValue;
    private HwndSource? _windowSource;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _overlayViewModel1 = new OverlayViewModel(viewModel, 1);
        _overlayViewModel2 = new OverlayViewModel(viewModel, 2);
        _overlay1 = new OverlayWindow(1) { DataContext = _overlayViewModel1 };
        _overlay2 = new OverlayWindow(2) { DataContext = _overlayViewModel2 };
        _windowLevelController = new MainWindowLevelController(SetMainWindowLevel);
        SourceInitialized += RegisterOverlayHotkey;
        Activated += KeepActiveWindowAboveGame;
        Deactivated += RestoreNormalWindowLevel;
        Closed += (_, _) => CleanupOverlay();
    }

    private void KeepActiveWindowAboveGame(object? sender, EventArgs e)
    {
        // Borderless and exclusive-fullscreen games can keep a normal desktop
        // window behind their swap chain even after Alt+Tab. Promote the main
        // window only while the user is actively working with it.
        _windowLevelController.Update(isActive: true);
    }

    private void RestoreNormalWindowLevel(object? sender, EventArgs e)
    {
        // Do not cover ETS after focus returns to the game. The two dedicated
        // overlays retain their independent always-on-top behavior.
        _windowLevelController.Update(isActive: false);
    }

    private bool SetMainWindowLevel(bool topmost)
    {
        var handle = new WindowInteropHelper(this).Handle;
        return handle != IntPtr.Zero &&
               SetWindowPos(
                   handle,
                   topmost ? HwndTopmost : HwndNoTopmost,
                   0,
                   0,
                   0,
                   0,
                   SwpNoMove | SwpNoSize | SwpNoActivate);
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

    private void ManualEntryPlan_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid { SelectedItem: ManualEntrySegmentRow segment } ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.EditManualEntrySegmentCommand.CanExecute(segment))
            viewModel.EditManualEntrySegmentCommand.Execute(segment);
    }

    private void CountryComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not ComboBox comboBox || string.IsNullOrWhiteSpace(e.Text)) return;

        var now = DateTime.UtcNow;
        if (now - _countrySearchUpdatedAtUtc > TimeSpan.FromSeconds(1))
            _countrySearchText = string.Empty;

        var searchText = _countrySearchText + e.Text.Trim();
        var match = FindCountryMatch(comboBox, searchText);
        if (match is null)
        {
            searchText = e.Text.Trim();
            match = FindCountryMatch(comboBox, searchText);
        }
        if (match is null) return;

        _countrySearchText = searchText;
        _countrySearchUpdatedAtUtc = now;
        comboBox.SelectedItem = match;
        comboBox.IsDropDownOpen = true;
        e.Handled = true;
    }

    private static CountryOption? FindCountryMatch(ComboBox comboBox, string searchText)
    {
        var countries = comboBox.Items.OfType<CountryOption>().ToList();
        return countries.FirstOrDefault(country =>
                   string.Equals(country.IsoAlpha2, searchText, StringComparison.CurrentCultureIgnoreCase))
               ?? countries.FirstOrDefault(country =>
                   country.TachographCode is not ("EUR" or "WLD") &&
                   string.Equals(country.TachographCode, searchText, StringComparison.CurrentCultureIgnoreCase))
               ?? countries.FirstOrDefault(country =>
                   country.IsoAlpha2.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase))
               ?? countries.FirstOrDefault(country =>
                   country.DisplayName.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase))
               ?? countries.FirstOrDefault(country =>
                   country.TachographCode is not ("EUR" or "WLD") &&
                   country.TachographCode.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase));
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}

internal sealed class MainWindowLevelController(Func<bool, bool> apply)
{
    private bool? _isTopmost;

    internal bool Update(bool isActive)
    {
        if (_isTopmost == isActive)
            return true;
        if (!apply(isActive))
            return false;
        _isTopmost = isActive;
        return true;
    }
}
