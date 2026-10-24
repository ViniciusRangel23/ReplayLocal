using Kairos.Ui.Edit;
namespace Kairos.Tests;
public static class NoCaptionFeatureTests
{
    [Test] public static void Interface_contains_no_automatic_caption_actions()
    {
        var repo = new DirectoryInfo(AppContext.BaseDirectory);
        while (repo != null && !Directory.Exists(Path.Combine(repo.FullName, "src", "Kairos"))) repo = repo.Parent;
        Assert.True(repo != null, "source checkout found");
        foreach (string path in new[] { "Ui/Edit/EditorView.xaml", "Ui/Pages/GeneralPage.xaml" })
        {
            string xaml = File.ReadAllText(Path.Combine(repo!.FullName, "src/Kairos", path));
            Assert.True(!new[] { "CaptionsCommand", "CaptionModelText", "CaptionModelOnDisk", "ShowingIntro", "AskingRecaption" }.Any(xaml.Contains), "no caption or download widgets: " + path);
        }
    }
    [Test] public static void Automatic_caption_and_download_components_are_absent()
    {
        var assembly = typeof(AppInfo).Assembly;
        Assert.True(!assembly.GetTypes().Any(t => t.Namespace == "Kairos.Engine.Captions" || t.Name is "CaptionMaker" or "ICaptionMaker"), "caption engine and downloader removed from application");
        Assert.True(!assembly.GetReferencedAssemblies().Any(a => a.Name == "System.Net.Http"), "application has no HTTP download client");
        Assert.True(typeof(EditorViewModel).GetProperty("CaptionsCommand") == null, "automatic caption command removed");
    }
    [Test] public static void Manual_text_editing_remains_available()
    {
        Assert.True(typeof(EditorViewModel).GetProperty("AddTextCommand") != null, "manual text command retained");
        Assert.True(typeof(EditorViewModel).GetProperty("Words") != null, "manual text content retained");
    }
}
