using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ETS2Tachograph.Desktop;

/// <summary>
/// Presents the counters of one card slot through a common set of overlay bindings.
/// </summary>
public sealed class OverlayViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly MainViewModel _source;

    public OverlayViewModel(MainViewModel source, int slot)
    {
        if (slot is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(slot));

        _source = source;
        Slot = slot;
        _source.PropertyChanged += SourceOnPropertyChanged;
    }

    public int Slot { get; }
    public string SlotLabel => $"S{Slot}";
    public string HotkeyLabel => $"ALT+{Slot}";
    public string WindowTitle => $"ETS2 Tachograph Overlay {SlotLabel}";

    public string ActivityText => Slot == 1 ? _source.ActivityText : _source.Driver2ActivityText;
    public string ContinuousDriving => Slot == 1 ? _source.ContinuousDriving : _source.Driver2ContinuousDriving;
    public string UntilBreak => Slot == 1 ? _source.UntilBreak : _source.Driver2UntilBreak;
    public string DailyDriving => Slot == 1 ? _source.DailyDrivingWithLimit : _source.Driver2DailyDrivingWithLimit;
    public string DailyWork => Slot == 1 ? _source.DailyWorkWithLimit : _source.Driver2DailyWorkWithLimit;
    public string Compensation => Slot == 1 ? _source.CompensationText : _source.Driver2CompensationText;
    public string CompensationForeground => Slot == 1 ? _source.CompensationForeground : _source.Driver2CompensationForeground;
    public CompensationOverview CompensationOverview =>
        Slot == 1 ? _source.CompensationOverview : _source.Driver2CompensationOverview;
    public string RestTargetText => Slot == 1 ? _source.RestTargetText : _source.RestTargetText2;
    public string RestElapsed => Slot == 1 ? _source.RestElapsed : _source.RestElapsed2;
    public string RestRemaining => Slot == 1 ? _source.RestRemaining : _source.RestRemaining2;
    public string RestStatus => Slot == 1 ? _source.RestStatus : _source.RestStatus2;
    public string ModesText => _source.ModesText;
    public string ConnectionStatus => _source.ConnectionStatus;

    private void SourceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // A frame can update several related counters. Refreshing the compact overlay as a
        // single projection also keeps derived properties such as RestTargetText in sync.
        OnPropertyChanged(string.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => _source.PropertyChanged -= SourceOnPropertyChanged;
}
