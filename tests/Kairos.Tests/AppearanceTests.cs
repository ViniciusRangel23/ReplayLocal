// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using System.Windows.Media;
using Kairos.App;
using Kairos.Settings;
using Kairos.Ui;

namespace Kairos.Tests;

public static class AppearanceTests
{
    [Test]
    public static void Replay_Local_uses_red_on_neutral_dark_grey()
    {
        Assert.Equal(0xEF4444u, ThemeColors.Accent, "approved red");
        Assert.Equal(0x181818u, ThemeColors.Background, "dark grey window");
    }

    [Test]
    public static void Settings_saved_with_a_theme_color_still_load()
    {
        var s = SettingsStore.Deserialize("""{"general":{"accent":"red","language":"en"}}""");
        Assert.Equal("en", s.General.Language, "the rest is read; the old color choice is ignored");
    }

    [Test]
    public static void Text_on_the_accent_stays_readable()
    {
        Assert.True(ThemeColors.Contrast(ThemeColors.Accent, ThemeColors.OnAccent) >= 4.5,
            $"dark text on red: {ThemeColors.Contrast(ThemeColors.Accent, ThemeColors.OnAccent):F1}:1");
        Assert.True(ThemeColors.Contrast(ThemeColors.Accent, 0xFFFFFF) < 4.5, "white text does not meet the text contrast target on red");
    }

    [Test]
    public static void Hover_is_lighter_and_the_soft_tint_stays_a_tint_of_the_background()
    {
        Assert.True(ThemeColors.Luminance(ThemeColors.Hover) > ThemeColors.Luminance(ThemeColors.Accent), "hover");
        Assert.True(ThemeColors.Contrast(ThemeColors.Soft, ThemeColors.Background) < 1.6, "soft is a tint, not a fill");
        Assert.True(ThemeColors.Contrast(ThemeColors.Soft, 0xF5F5F7) >= 7, "text stays easy to read on it");
    }

    [Test]
    public static void Theme_resources_take_the_one_theme_and_fluent_controls_follow_it()
    {
        var r = new System.Windows.ResourceDictionary();
        ThemeAccent.Apply(r);
        Assert.Equal(Color.FromRgb(0xEF, 0x44, 0x44), ((SolidColorBrush)r["Accent"]).Color, "accent");
        Assert.Equal(Color.FromRgb(0x12, 0x12, 0x12), ((SolidColorBrush)r["OnAccent"]).Color, "dark text on red");
        Assert.Equal(Color.FromRgb(0xEF, 0x44, 0x44), (Color)r["SystemAccentColor"], "sliders and switches, not the Windows accent");
        Assert.True(r.Contains("AccentHover") && r.Contains("AccentSoft"), "hover and soft");
    }

    [Test]
    public static void Tray_icon_uses_the_theme_color()
    {
        Assert.Equal(0xEF4444u, TrayController.ColorFor("active"), "active");
        Assert.Equal(0xEF4444u, TrayController.ColorFor("recording"), "recording");
        Assert.Equal(0x2ED47Au, TrayController.ColorFor("saved"), "saved stays green");
        Assert.Equal(0xF05265u, TrayController.ColorFor("error"), "errors use the approved red");
    }

    [Test]
    public static void Appearance_language_sound_and_hotkeys_keep_the_buffer()
    {
        var before = new AppSettings();
        var after = SettingsStore.Deserialize(SettingsStore.Serialize(before));
        after.General.Language = "en";
        after.General.SoundOnSave = false;
        after.General.ToastOnSave = false;
        after.General.StartWithWindows = false;
        after.Hotkeys.Save = "Ctrl+F8";
        Assert.Equal(false, SettingsDiff.NeedsRebuild(before, after), "no restart");
    }

    [Test]
    public static void Capture_and_output_changes_restart_the_buffer()
    {
        var before = new AppSettings();
        before.Capture.Confirmed = true;
        before.Audio.SelectMic("explicit-test-device");
        AppSettings Changed(Action<AppSettings> change)
        {
            var s = SettingsStore.Deserialize(SettingsStore.Serialize(before));
            change(s);
            return s;
        }
        Assert.Equal(true, SettingsDiff.NeedsRebuild(before, Changed(s => s.Video.Fps = 30)), "fps");
        Assert.Equal(true, SettingsDiff.NeedsRebuild(before, Changed(s => s.Replay.Seconds = 120)), "length");
        Assert.Equal(true, SettingsDiff.NeedsRebuild(before, Changed(s => s.Audio.Mic = false)), "mic");
        Assert.Equal(true, SettingsDiff.NeedsRebuild(before, Changed(s => s.Output.Folder = @"D:\Other")), "folder");
        Assert.Equal(true, SettingsDiff.NeedsRebuild(before, Changed(s => s.General.OnlyWhenGaming = !s.General.OnlyWhenGaming)), "games only");
    }
}
