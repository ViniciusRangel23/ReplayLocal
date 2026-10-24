using System.Windows;
using Kairos.Engine.Edit;

namespace Kairos.Ui.Edit;

public sealed partial class EditorViewModel
{
    public const double TextBottomMargin = 0.08;

    /// <summary>The box a text needs in the picture, with the text settings' size and font.</summary>
    Size MeasureText(string text) =>
        ItemRenderer.BoxSize(new TextItem(Guid.Empty, 0, 0, text, default, _textColor, _textSize, _textEffect, _textFont), new Size(_aspect * 1000, 1000));

    /// <summary>
    /// A restyled text keeps its box when only the colour, style or place changed; new words, a new size or a new font
    /// fit the box to the text, centred where it was and standing on the same line.
    /// </summary>
    TextItem Refit(TextItem styled, TextItem old)
    {
        if (styled.SizeRel == old.SizeRel && styled.Font == old.Font && styled.Text == old.Text)
            return styled;
        var size = ItemRenderer.BoxSize(styled, new Size(_aspect * 1000, 1000));
        double x = Math.Clamp(styled.Box.X + styled.Box.Width / 2 - size.Width / 2, 0, Math.Max(0, 1 - size.Width));
        double y = Math.Clamp(styled.Box.Bottom - size.Height, 0, Math.Max(0, 1 - size.Height));
        return styled with { Box = new Rect(x, y, size.Width, size.Height) };
    }
}
