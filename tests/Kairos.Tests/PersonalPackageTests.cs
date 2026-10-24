namespace Kairos.Tests;
public static class PersonalPackageTests
{
    [Test] public static void Package_rejects_forbidden_components()
    {
        var refs = typeof(AppInfo).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
        Assert.True(!refs.Any(n => n != null && (n.Contains("WebView2") || n.Contains("Velopack"))), "browser and updater absent");
    }
}
