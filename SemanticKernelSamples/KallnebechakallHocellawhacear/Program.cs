// See https://aka.ms/new-console-template for more information

using System.Text.Json;

using static System.Net.Mime.MediaTypeNames;

var text = """
           {
                # 文本提示词与参数组合
                "type": "text",
                "text": "无人机以极快速度穿越复杂障碍或自然奇观，带来沉浸式飞行体验  --duration 5 --camerafixed false --watermark true"
            }
           """;

Console.WriteLine(JsonEncodedText.Encode(text).Value);