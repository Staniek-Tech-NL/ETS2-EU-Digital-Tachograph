using System.Windows.Markup;

namespace ETS2Tachograph.Desktop.Localization;

[MarkupExtensionReturnType(typeof(string))]
public sealed class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) => UiStrings.Get(Key);
}
