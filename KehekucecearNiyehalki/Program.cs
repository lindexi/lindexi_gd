// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;

var code =
    """
    <SolidColorBrush x:Key="TextFillColorPrimaryBrush" Color="{StaticResource TextFillColorPrimary}" />
     <SolidColorBrush x:Key="TextFillColorSecondaryBrush" Color="{StaticResource TextFillColorSecondary}" />
     <SolidColorBrush x:Key="TextFillColorTertiaryBrush" Color="{StaticResource TextFillColorTertiary}" />
     <SolidColorBrush x:Key="TextFillColorDisabledBrush" Color="{StaticResource TextFillColorDisabled}" />
     <SolidColorBrush x:Key="TextPlaceholderColorBrush" Color="{StaticResource TextPlaceholderColor}" />
     <SolidColorBrush x:Key="TextFillColorInverseBrush" Color="{StaticResource TextFillColorInverse}" />
    
     <SolidColorBrush x:Key="AccentTextFillColorDisabledBrush" Color="{StaticResource AccentTextFillColorDisabled}" />
    
     <SolidColorBrush x:Key="TextOnAccentFillColorSelectedTextBrush" Color="{StaticResource TextOnAccentFillColorSelectedText}" />
    
     <SolidColorBrush x:Key="TextOnAccentFillColorPrimaryBrush" Color="{StaticResource TextOnAccentFillColorPrimary}" />
     <SolidColorBrush x:Key="TextOnAccentFillColorSecondaryBrush" Color="{StaticResource TextOnAccentFillColorSecondary}" />
     <SolidColorBrush x:Key="TextOnAccentFillColorDisabledBrush" Color="{StaticResource TextOnAccentFillColorDisabled}" />
    
     <SolidColorBrush x:Key="ControlFillColorDefaultBrush" Color="{StaticResource ControlFillColorDefault}" />
     <SolidColorBrush x:Key="ControlFillColorSecondaryBrush" Color="{StaticResource ControlFillColorSecondary}" />
     <SolidColorBrush x:Key="ControlFillColorTertiaryBrush" Color="{StaticResource ControlFillColorTertiary}" />
     <SolidColorBrush x:Key="ControlFillColorDisabledBrush" Color="{StaticResource ControlFillColorDisabled}" />
     <SolidColorBrush x:Key="ControlFillColorTransparentBrush" Color="{StaticResource ControlFillColorTransparent}" />
     <SolidColorBrush x:Key="ControlFillColorInputActiveBrush" Color="{StaticResource ControlFillColorInputActive}" />
    
     <SolidColorBrush x:Key="ControlStrongFillColorDefaultBrush" Color="{StaticResource ControlStrongFillColorDefault}" />
     <SolidColorBrush x:Key="ControlStrongFillColorDisabledBrush" Color="{StaticResource ControlStrongFillColorDisabled}" />
    
     <SolidColorBrush x:Key="ControlSolidFillColorDefaultBrush" Color="{StaticResource ControlSolidFillColorDefault}" />
    
     <SolidColorBrush x:Key="SubtleFillColorTransparentBrush" Color="{StaticResource SubtleFillColorTransparent}" />
     <SolidColorBrush x:Key="SubtleFillColorSecondaryBrush" Color="{StaticResource SubtleFillColorSecondary}" />
     <SolidColorBrush x:Key="SubtleFillColorTertiaryBrush" Color="{StaticResource SubtleFillColorTertiary}" />
     <SolidColorBrush x:Key="SubtleFillColorDisabledBrush" Color="{StaticResource SubtleFillColorDisabled}" />
    
     <SolidColorBrush x:Key="ControlAltFillColorTransparentBrush" Color="{StaticResource ControlAltFillColorTransparent}" />
     <SolidColorBrush x:Key="ControlAltFillColorSecondaryBrush" Color="{StaticResource ControlAltFillColorSecondary}" />
     <SolidColorBrush x:Key="ControlAltFillColorTertiaryBrush" Color="{StaticResource ControlAltFillColorTertiary}" />
     <SolidColorBrush x:Key="ControlAltFillColorQuarternaryBrush" Color="{StaticResource ControlAltFillColorQuarternary}" />
     <SolidColorBrush x:Key="ControlAltFillColorDisabledBrush" Color="{StaticResource ControlAltFillColorDisabled}" />
    
     <SolidColorBrush x:Key="ControlOnImageFillColorDefaultBrush" Color="{StaticResource ControlOnImageFillColorDefault}" />
     <SolidColorBrush x:Key="ControlOnImageFillColorSecondaryBrush" Color="{StaticResource ControlOnImageFillColorSecondary}" />
     <SolidColorBrush x:Key="ControlOnImageFillColorTertiaryBrush" Color="{StaticResource ControlOnImageFillColorTertiary}" />
     <SolidColorBrush x:Key="ControlOnImageFillColorDisabledBrush" Color="{StaticResource ControlOnImageFillColorDisabled}" />
    
     <SolidColorBrush x:Key="AccentFillColorDisabledBrush" Color="{StaticResource AccentFillColorDisabled}" />
    
     <SolidColorBrush x:Key="ControlStrokeColorDefaultBrush" Color="{StaticResource ControlStrokeColorDefault}" />
     <SolidColorBrush x:Key="ControlStrokeColorSecondaryBrush" Color="{StaticResource ControlStrokeColorSecondary}" />
     <SolidColorBrush x:Key="ControlStrokeColorTertiaryBrush" Color="{StaticResource ControlStrokeColorTertiary}" />
     <SolidColorBrush x:Key="ControlStrokeColorOnAccentDefaultBrush" Color="{StaticResource ControlStrokeColorOnAccentDefault}" />
     <SolidColorBrush x:Key="ControlStrokeColorOnAccentSecondaryBrush" Color="{StaticResource ControlStrokeColorOnAccentSecondary}" />
     <SolidColorBrush x:Key="ControlStrokeColorOnAccentTertiaryBrush" Color="{StaticResource ControlStrokeColorOnAccentTertiary}" />
     <SolidColorBrush x:Key="ControlStrokeColorOnAccentDisabledBrush" Color="{StaticResource ControlStrokeColorOnAccentDisabled}" />
    
     <SolidColorBrush x:Key="ControlStrokeColorForStrongFillWhenOnImageBrush" Color="{StaticResource ControlStrokeColorForStrongFillWhenOnImage}" />
    
     <SolidColorBrush x:Key="CardStrokeColorDefaultBrush" Color="{StaticResource CardStrokeColorDefault}" />
     <SolidColorBrush x:Key="CardStrokeColorDefaultSolidBrush" Color="{StaticResource CardStrokeColorDefaultSolid}" />
    
     <SolidColorBrush x:Key="ControlStrongStrokeColorDefaultBrush" Color="{StaticResource ControlStrongStrokeColorDefault}" />
     <SolidColorBrush x:Key="ControlStrongStrokeColorDisabledBrush" Color="{StaticResource ControlStrongStrokeColorDisabled}" />
    
     <SolidColorBrush x:Key="SurfaceStrokeColorDefaultBrush" Color="{StaticResource SurfaceStrokeColorDefault}" />
     <SolidColorBrush x:Key="SurfaceStrokeColorFlyoutBrush" Color="{StaticResource SurfaceStrokeColorFlyout}" />
     <SolidColorBrush x:Key="SurfaceStrokeColorInverseBrush" Color="{StaticResource SurfaceStrokeColorInverse}" />
    
     <SolidColorBrush x:Key="DividerStrokeColorDefaultBrush" Color="{StaticResource DividerStrokeColorDefault}" />
    
     <SolidColorBrush x:Key="FocusStrokeColorOuterBrush" Color="{StaticResource FocusStrokeColorOuter}" />
     <SolidColorBrush x:Key="FocusStrokeColorInnerBrush" Color="{StaticResource FocusStrokeColorInner}" />
    
     <SolidColorBrush x:Key="CardBackgroundFillColorDefaultBrush" Color="{StaticResource CardBackgroundFillColorDefault}" />
     <SolidColorBrush x:Key="CardBackgroundFillColorSecondaryBrush" Color="{StaticResource CardBackgroundFillColorSecondary}" />
    
     <SolidColorBrush x:Key="SmokeFillColorDefaultBrush" Color="{StaticResource SmokeFillColorDefault}" />
    
     <SolidColorBrush x:Key="LayerFillColorDefaultBrush" Color="{StaticResource LayerFillColorDefault}" />
     <SolidColorBrush x:Key="LayerFillColorAltBrush" Color="{StaticResource LayerFillColorAlt}" />
     <SolidColorBrush x:Key="LayerOnAcrylicFillColorDefaultBrush" Color="{StaticResource LayerOnAcrylicFillColorDefault}" />
     <SolidColorBrush x:Key="LayerOnAccentAcrylicFillColorDefaultBrush" Color="{StaticResource LayerOnAccentAcrylicFillColorDefault}" />
    
     <SolidColorBrush x:Key="LayerOnMicaBaseAltFillColorDefaultBrush" Color="{StaticResource LayerOnMicaBaseAltFillColorDefault}" />
     <SolidColorBrush x:Key="LayerOnMicaBaseAltFillColorSecondaryBrush" Color="{StaticResource LayerOnMicaBaseAltFillColorSecondary}" />
     <SolidColorBrush x:Key="LayerOnMicaBaseAltFillColorTertiaryBrush" Color="{StaticResource LayerOnMicaBaseAltFillColorTertiary}" />
     <SolidColorBrush x:Key="LayerOnMicaBaseAltFillColorTransparentBrush" Color="{StaticResource LayerOnMicaBaseAltFillColorTransparent}" />
    
     <SolidColorBrush x:Key="SolidBackgroundFillColorBaseBrush" Color="{StaticResource SolidBackgroundFillColorBase}" />
     <SolidColorBrush x:Key="SolidBackgroundFillColorSecondaryBrush" Color="{StaticResource SolidBackgroundFillColorSecondary}" />
     <SolidColorBrush x:Key="SolidBackgroundFillColorTertiaryBrush" Color="{StaticResource SolidBackgroundFillColorTertiary}" />
     <SolidColorBrush x:Key="SolidBackgroundFillColorQuarternaryBrush" Color="{StaticResource SolidBackgroundFillColorQuarternary}" />
     <SolidColorBrush x:Key="SolidBackgroundFillColorBaseAltBrush" Color="{StaticResource SolidBackgroundFillColorBaseAlt}" />
    
     <SolidColorBrush x:Key="SystemFillColorSuccessBrush" Color="{StaticResource SystemFillColorSuccess}" />
     <SolidColorBrush x:Key="SystemFillColorCautionBrush" Color="{StaticResource SystemFillColorCaution}" />
     <SolidColorBrush x:Key="SystemFillColorCriticalBrush" Color="{StaticResource SystemFillColorCritical}" />
     <SolidColorBrush x:Key="SystemFillColorNeutralBrush" Color="{StaticResource SystemFillColorNeutral}" />
     <SolidColorBrush x:Key="SystemFillColorSolidNeutralBrush" Color="{StaticResource SystemFillColorSolidNeutral}" />
     <SolidColorBrush x:Key="SystemFillColorAttentionBackgroundBrush" Color="{StaticResource SystemFillColorAttentionBackground}" />
     <SolidColorBrush x:Key="SystemFillColorSuccessBackgroundBrush" Color="{StaticResource SystemFillColorSuccessBackground}" />
     <SolidColorBrush x:Key="SystemFillColorCautionBackgroundBrush" Color="{StaticResource SystemFillColorCautionBackground}" />
     <SolidColorBrush x:Key="SystemFillColorCriticalBackgroundBrush" Color="{StaticResource SystemFillColorCriticalBackground}" />
     <SolidColorBrush x:Key="SystemFillColorNeutralBackgroundBrush" Color="{StaticResource SystemFillColorNeutralBackground}" />
     <SolidColorBrush x:Key="SystemFillColorSolidAttentionBackgroundBrush" Color="{StaticResource SystemFillColorSolidAttentionBackground}" />
     <SolidColorBrush x:Key="SystemFillColorSolidNeutralBackgroundBrush" Color="{StaticResource SystemFillColorSolidNeutralBackground}" />
    """;

var stringReader = new StringReader(code);
string line;
while ((line = stringReader.ReadLine()) != null)
{
    if (!string.IsNullOrEmpty(line) && line.Contains("<SolidColorBrush ", StringComparison.Ordinal))
    {
        var match = Regex.Match(line,@"Key=""(\w+)"" Color=""{StaticResource (\w+)}""");
        if (match.Success)
        {
            var brush = match.Groups[1].Value;
            var color = match.Groups[2].Value;

            if (brush != (color + "Brush"))
            {

            }
        }
    }
}

Console.WriteLine("Hello, World!");
