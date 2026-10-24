using Kairos.Engine.Edit;
namespace Kairos.Ui.Edit;

/// <summary>Colour graph for one decode generation; project snapshots are read without touching UI controls.</summary>
internal sealed class EditorFrameFilter(Func<EditProject?> project, int width, int height) : IPreviewFrameFilter
{
    FrameFilter? _colour;
    public void Apply(byte[] pixels, double time)
    {
        var p = project();
        if (p == null) return;
        var (look, strength) = p.LookAt(time);
        string? expression = ColourLooks.Filter(look, strength);
        if (expression != null)
        {
            _colour ??= new FrameFilter(width, height, expression);
            _colour.SetFilter(expression);
        }
        EditorPlayback.Filter(p, expression == null ? null : _colour, pixels, width, height, time);
    }
    public void Dispose() { _colour?.Dispose(); _colour = null; }
}
