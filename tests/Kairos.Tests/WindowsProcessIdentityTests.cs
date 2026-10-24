using Kairos.Native;
namespace Kairos.Tests;
public static class WindowsProcessIdentityTests
{
    [Test]
    public static void Limited_query_resolves_own_image_and_retained_handle_lifetime()
    {
        string? path=WindowsProcessIdentity.ReadImagePath(Environment.ProcessId);
        Assert.True(string.Equals(path,Environment.ProcessPath,StringComparison.OrdinalIgnoreCase),"exact executable path");
        var identity=WindowsProcessIdentity.Open(Environment.ProcessId);
        Assert.True(identity.IsAlive && identity.ExecutablePath==path,"query and lifetime use same process object");
        identity.Dispose();identity.Dispose();Assert.True(!identity.IsAlive,"disposed handle cannot authorize packets");
    }
    [Test]
    public static void Missing_process_never_becomes_an_identity()
    {
        Assert.True(WindowsProcessIdentity.ReadImagePath(0)==null && WindowsProcessIdentity.ReadImagePath(-1)==null,"invalid identifiers rejected");
        Assert.True(WindowsProcessIdentity.ReadImagePath(int.MaxValue)==null,"missing process has no path");
        bool failed=false;try{using var identity=WindowsProcessIdentity.Open(0);}catch(InvalidOperationException){failed=true;}
        Assert.True(failed,"cannot retain missing process");
    }
}
