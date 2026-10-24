using Kairos.App;
using Kairos.Settings;
using Kairos.Ui;
namespace Kairos.Tests;
public static class ReviewRegressionTests
{
    static void DirtyWindow(bool reloadBeforeApply, bool pendingEnable = false)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            string path = SettingsStore.DefaultPath;
            string? previous = File.Exists(path) ? File.ReadAllText(path) : null;
            try
            {
                var settings = new AppSettings(); settings.Capture.Confirmed = true; settings.Capture.Enabled = !pendingEnable;
                SettingsStore.Save(path, settings);
                var vm = new SettingsViewModel(); vm.Fps = 90;
                if (pendingEnable) vm.CaptureEnabled = true;
                var external = SettingsStore.Load(path); external.Capture.Enabled = false;
                external.Capture.DisableRevision = Guid.NewGuid().ToString("N");
                SettingsStore.Save(path, external); // tray's immediate disable
                if (reloadBeforeApply) vm.ReloadIfChangedOutside();
                vm.ApplyCommand.Execute(null);
                var disk = SettingsStore.Load(path);
                Assert.True(!disk.Capture.Enabled, "unrelated pending save must not restart disabled capture");
                Assert.Equal(90, disk.Video.Fps, "pending unrelated edit preserved");
                Assert.True(!vm.CaptureEnabled, "checkbox reflects disable");
                vm.CaptureEnabled = true; vm.ApplyCommand.Execute(null);
                Assert.True(SettingsStore.Load(path).Capture.Enabled, "a subsequent explicit enable is allowed");
            }
            catch (Exception e) { failure = e; }
            finally { if (previous == null) File.Delete(path); else File.WriteAllText(path, previous); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (failure != null) throw failure;
    }
    [Test] public static void External_disable_survives_dirty_window_reload() => DirtyWindow(true);
    [Test] public static void External_disable_survives_apply_before_file_watcher() => DirtyWindow(false);
    [Test] public static void External_disable_revokes_pending_enable() => DirtyWindow(true, true);
    [Test] public static void External_disable_revokes_pending_enable_before_watcher() => DirtyWindow(false, true);
    [Test] public static void Capture_startup_failure_remains_actionable()
    {
        var status = new StatusDto(null, [], null, "Encoder initialization failed", false, false, "test", CaptureEnabled: false);
        Assert.Equal("error", StatusLogic.ReplayState(status, true), "startup failure must not be hidden as ordinary off");
        Assert.Equal(status.Error, StatusLogic.ReplayDetail("error", status.Error), "actionable error shown");
    }
}
