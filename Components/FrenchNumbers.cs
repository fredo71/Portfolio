using System.Globalization;

namespace Portfolio.Components;

/// <summary>
/// Number formatting for text the reader sees, on a French-language site:
/// comma for the decimal mark, narrow no-break space (U+202F) for thousands.
///
/// Only for display. SVG path data, CSS percentages and any other machine-read
/// value must stay on <see cref="CultureInfo.InvariantCulture"/> — a comma
/// there is a syntax error, not a typography choice.
///
/// Built by overriding the invariant culture's two separators rather than by
/// loading fr-FR, so the output does not depend on which ICU data the runtime
/// happens to ship, and matches what tools/GameStatsGen bakes into
/// Pages/Game.razor.cs.
/// </summary>
public static class FrenchNumbers
{
    public static readonly CultureInfo Culture = Build();

    static CultureInfo Build()
    {
        var fr = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        fr.NumberFormat.NumberGroupSeparator = " ";
        fr.NumberFormat.NumberDecimalSeparator = ",";
        return fr;
    }

    public static string Format(double value, string format) => value.ToString(format, Culture);
}
