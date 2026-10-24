// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using System.Windows;
using System.Windows.Controls;

namespace Kairos.Ui.Edit;

/// <summary>
/// A column that never scrolls: its children one under another, and when they do not all fit, the ones marked
/// <c>Optional</c> (a hint, a label) are left out, the last first, until the rest fits. The inspector's pages use it so
/// nothing is cut off and nothing scrolls at the window's smallest size.
/// </summary>
public sealed class FitPanel : Panel
{
    public static readonly DependencyProperty FlexibleProperty=DependencyProperty.RegisterAttached("Flexible",typeof(bool),typeof(FitPanel),new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.AffectsParentMeasure));
    public static bool GetFlexible(UIElement element)=>(bool)element.GetValue(FlexibleProperty);
    public static void SetFlexible(UIElement element,bool value)=>element.SetValue(FlexibleProperty,value);
    public static readonly DependencyProperty OptionalProperty = DependencyProperty.RegisterAttached("Optional", typeof(bool), typeof(FitPanel),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    public static bool GetOptional(UIElement element) => (bool)element.GetValue(OptionalProperty);

    public static void SetOptional(UIElement element, bool value) => element.SetValue(OptionalProperty, value);

    readonly HashSet<UIElement> _left = [];

    public FitPanel() => ClipToBounds = true;

    /// <summary>The children left out at the last layout (tests read it).</summary>
    public IReadOnlyCollection<UIElement> LeftOut => _left;

    protected override Size MeasureOverride(Size available)
    {
        double width = 0, total = 0;
        var children = InternalChildren.Cast<UIElement>().ToList();
        foreach (var child in children)
        {
            child.Measure(new Size(available.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            total += child.DesiredSize.Height;
        }
        _left.Clear();
        for (int i = children.Count - 1; i >= 0 && total > available.Height; i--)
        {
            if (!GetOptional(children[i]) || children[i].DesiredSize.Height <= 0)
                continue;
            _left.Add(children[i]);
            total -= children[i].DesiredSize.Height;
        }
        if(!double.IsInfinity(available.Height))
            foreach(var child in children.Where(GetFlexible).Where(c=>!_left.Contains(c)))
            {
                total-=child.DesiredSize.Height;
                child.Measure(new Size(available.Width,Math.Max(0,available.Height-total)));
                total+=child.DesiredSize.Height;
            }
        return new Size(width, double.IsInfinity(available.Height) ? total : Math.Min(total, available.Height));
    }

    protected override Size ArrangeOverride(Size final)
    {
        double y = 0;
        foreach (UIElement child in InternalChildren)
        {
            double h = child.DesiredSize.Height;
            if (_left.Contains(child))
            {
                child.Arrange(new Rect(0, -10_000, final.Width, h)); // out of sight (the panel clips)
                continue;
            }
            child.Arrange(new Rect(0, y, final.Width, h));
            y += h;
        }
        return final;
    }
}
