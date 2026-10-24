using Kairos;
using Kairos.Settings;
namespace Kairos.Tests;
public static class PersonalIdentityTests
{
    [Test] public static void Output_files_use_personal_root()
    {
        Assert.Equal("ReplayLocal", Path.GetFileName(OutputSettings.DefaultFolder), "output must not target original Kairos data");
        Assert.True(AppInfo.PipeName != "Kairos", "original engine must not receive personal commands");
    }
    [Test] public static void Personal_build_has_no_updater_dependency()
    {
        Assert.True(!typeof(AppInfo).Assembly.GetReferencedAssemblies().Any(a => a.Name == "Velopack"), "no original feed/updater authority");
    }
}
