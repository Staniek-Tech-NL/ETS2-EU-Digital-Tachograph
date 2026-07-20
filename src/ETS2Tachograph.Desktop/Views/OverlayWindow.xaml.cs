using System.Runtime.InteropServices;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ETS2Tachograph.Desktop;

public partial class OverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private readonly int _slot;
    private readonly string _positionFile;
    private bool _positionLoaded;

    public OverlayWindow(int slot)
    {
        if (slot is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(slot));

        _slot = slot;
        _positionFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ETS2Tachograph",
            $"overlay-position-s{slot}.json");

        InitializeComponent();
        SourceInitialized += (_, _) => ConfigureOverlayWindow();
        Loaded += (_, _) => LoadPosition();
    }

    private void LoadPosition()
    {
        if (_positionLoaded) return;
        _positionLoaded = true;

        try
        {
            if (File.Exists(_positionFile))
            {
                var position = JsonSerializer.Deserialize<OverlayPosition>(File.ReadAllText(_positionFile));
                if (position is not null && IsOnScreen(position.Left, position.Top))
                {
                    Left = position.Left;
                    Top = position.Top;
                    return;
                }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (JsonException) { }

        PositionInWorkArea();
    }

    private void PositionInWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 24;
        Top = workArea.Top + 24 + (_slot - 1) * (Height + 12);

        if (Top + Height > workArea.Bottom)
            Top = Math.Max(workArea.Top, workArea.Bottom - Height - 24);
    }

    private bool IsOnScreen(double left, double top)
    {
        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        var window = new Rect(left, top, Width, Height);
        return virtualScreen.IntersectsWith(window);
    }

    private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        try
        {
            DragMove();
            SavePosition();
        }
        catch (InvalidOperationException)
        {
            // The mouse button may have been released before WPF began the drag.
        }
    }

    private void SavePosition()
    {
        try
        {
            var directory = Path.GetDirectoryName(_positionFile)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                _positionFile,
                JsonSerializer.Serialize(new OverlayPosition(Left, Top)));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void ConfigureOverlayWindow()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExToolWindow | WsExNoActivate));
    }

    private sealed record OverlayPosition(double Left, double Top);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr newValue);
}
