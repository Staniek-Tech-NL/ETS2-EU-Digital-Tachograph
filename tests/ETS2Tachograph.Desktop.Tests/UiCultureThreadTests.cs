using System.Globalization;
using ETS2Tachograph.Desktop.Localization;

namespace ETS2Tachograph.Desktop.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UiCultureSerialCollection
{
    public const string Name = "UI culture serial";
}

[Collection(UiCultureSerialCollection.Name)]
public sealed class UiCultureThreadTests
{
    [Fact]
    public void Ui_resources_use_selected_process_culture_on_a_thread_with_different_culture()
    {
        var originalCulture = UiCulture.Current.Name;
        try
        {
            UiCulture.Apply(UiCulture.EnglishUnitedKingdom, applyWpfLanguage: false);
            string? menuTitle = null;
            var thread = new Thread(() =>
            {
                CultureInfo.CurrentUICulture =
                    CultureInfo.GetCultureInfo(UiCulture.Polish);
                menuTitle = UiStrings.Get("DeviceMenu_MainTitle");
            });

            thread.Start();
            thread.Join();

            Assert.Equal("MAIN MENU", menuTitle);
        }
        finally
        {
            UiCulture.Apply(originalCulture, applyWpfLanguage: false);
        }
    }
}
