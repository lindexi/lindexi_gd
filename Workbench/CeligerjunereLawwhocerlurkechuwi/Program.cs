// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;

var t =
    """
    Type=DiagonalWipeIn Text=擦入 Category=Appearance
    Type=TranslateIn Text=移入 Category=Appearance
    Type=TranslateFadeIn Text=浮现 Category=Appearance
    Type=ScaleIn Text=放大 Category=Appearance
    Type=RandomizedLinesInEffect Text=线条 Category=Appearance
    Type=DissolveInEffect Text=溶解 Category=Appearance
    Type=BlindIn Text=百叶窗 Category=Appearance
    Type=RippleInEffect Text=涟漪 Category=Appearance
    Type=RotationFadeIn Text=旋转 Category=Appearance
    Type=WipeDropIn Text=掉落 Category=Appearance
    Type=InchingScaleIn Text=强调 Category=Appearance
    Type=JumpIn Text=跳跃 Category=Appearance
    Type=WhirlFadeIn Text=飞旋 Category=Appearance
    Type=SpiralScaleIn Text=陀螺 Category=Appearance
    
    Type=Curve Text=自定义 Category=Emphasis
    Type=Line Text=直线 Category=Emphasis
    Type=Flicker Text=闪烁 Category=Emphasis
    Type=Shake Text=抖动 Category=Emphasis
    Type=Heartbeat Text=心跳 Category=Emphasis
    Type=Bounce Text=弹跳 Category=Emphasis
    Type=Wave Text=波浪 Category=Emphasis
    Type=RotationEmphasis Text=旋转 Category=Emphasis
    Type=Zoom Text=缩放 Category=Emphasis
    Type=Reversal Text=翻转 Category=Emphasis
    Type=Transparency Text=透明度 Category=Emphasis
    
    Type=DiagonalWipeOut Text=擦出 Category=Disappearance
    Type=TranslateOut Text=移出 Category=Disappearance
    Type=TranslateFadeOut Text=浮出 Category=Disappearance
    Type=ScaleOut Text=缩小 Category=Disappearance
    Type=RandomizedLinesOutEffect Text=线条 Category=Disappearance
    Type=DissolveOutEffect Text=溶解 Category=Disappearance
    Type=RippleOutEffect Text=涟漪 Category=Disappearance
    Type=JumpOut Text=跳跃 Category=Disappearance
    Type=WhirlFadeOut Text=飞旋 Category=Disappearance
    Type=SpiralScaleOut Text=螺旋 Category=Disappearance
    """;

var stringReader = new StringReader(t);
string result = "";
while (true)
{
    var line = stringReader.ReadLine();
    if (line == null) break;

    var match = Regex.Match(line, @"Type=(\w+) Text=(\w+) ");
    var type = match.Groups[1].Value;
    var text = match.Groups[2].Value;

    var code =
        $$"""
        // {{text}}动画
        new {{type}}Animation(),
        
        """;
    result += code;
}

Console.WriteLine(result);
