using ETS2Tachograph.Desktop;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class MainWindowLevelPolicyTests
{
    [Fact]
    public void Active_main_window_is_promoted_without_repeating_the_native_operation()
    {
        var operations = new List<bool>();
        var controller = new MainWindowLevelController(isTopmost =>
        {
            operations.Add(isTopmost);
            return true;
        });

        Assert.True(controller.Update(isActive: true));
        Assert.True(controller.Update(isActive: true));

        Assert.Equal([true], operations);
    }

    [Fact]
    public void Returning_focus_to_the_game_restores_normal_window_level_once()
    {
        var operations = new List<bool>();
        var controller = new MainWindowLevelController(isTopmost =>
        {
            operations.Add(isTopmost);
            return true;
        });

        Assert.True(controller.Update(isActive: true));
        Assert.True(controller.Update(isActive: false));
        Assert.True(controller.Update(isActive: false));

        Assert.Equal([true, false], operations);
    }

    [Fact]
    public void Failed_native_operation_can_be_retried()
    {
        var attempts = 0;
        var controller = new MainWindowLevelController(_ => ++attempts > 1);

        Assert.False(controller.Update(isActive: true));
        Assert.True(controller.Update(isActive: true));

        Assert.Equal(2, attempts);
    }
}
