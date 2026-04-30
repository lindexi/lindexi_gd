// See https://aka.ms/new-console-template for more information

using System.Text.Json.Serialization;

var jsonText =
    """
    {
      "lectureName": "《腊八粥》课文讲解",
      "totalPages": 5,
      "pages": [
        {
          "pageNumber": 1,
          "scriptItems": [
            {
              "tone": "亲切平缓",
              "content": "同学们好，今天我们将开启一篇新课文的学习。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "清晰强调",
              "content": "这篇课文是第2课，《腊八粥》。",
              "pauseAfterSeconds": 2.0
            },
            {
              "tone": "平缓",
              "content": "本篇课文我们会分为两个课时进行讲解，",
              "pauseAfterSeconds": 0.5
            },
            {
              "tone": "平缓",
              "content": "分别是第1课时和第2课时。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "引导",
              "content": "接下来我们首先进入第1课时的学习。",
              "pauseAfterSeconds": 0.0
            }
          ]
        },
        {
          "pageNumber": 2,
          "scriptItems": [
            {
              "tone": "轻松愉悦",
              "content": "第1课时的学习正式开始，我们首先进入第一个环节：儿歌欣赏，激发兴趣，导入新课。",
              "pauseAfterSeconds": 1.5
            },
            {
              "tone": "设问",
              "content": "听完儿歌我们先来回答两个小问题，第一个：这是一首关于什么的儿歌？",
              "pauseAfterSeconds": 2.0
            },
            {
              "tone": "明确",
              "content": "没错，这是一首关于腊八粥的儿歌。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "设问",
              "content": "第二个问题：腊八粥是哪个节日吃的美食？",
              "pauseAfterSeconds": 2.0
            },
            {
              "tone": "肯定",
              "content": "非常棒，腊八粥是腊八节吃的美食。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "带回忆感",
              "content": "我们之前学习过老舍先生的《北京的春节》，大家还记得老舍先生在《北京的春节》是怎样介绍腊八粥的吗？",
              "pauseAfterSeconds": 2.0
            },
            {
              "tone": "平缓",
              "content": "对，老舍先生在课文第1自然段介绍了腊八粥的食材。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "引导思考",
              "content": "那你对腊八粥有更多的了解吗？",
              "pauseAfterSeconds": 1.0
            }
          ]
        },
        {
          "pageNumber": 3,
          "scriptItems": [
            {
              "tone": "引导",
              "content": "要读懂这篇《腊八粥》，我们首先来了解一下本文的作者沈从文先生。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "平缓陈述",
              "content": "沈从文生于1902年，逝世于1988年，是我国优秀作家，原名岳焕，湖南凤凰人，苗族。",
              "pauseAfterSeconds": 1.5
            },
            {
              "tone": "强调",
              "content": "他创作中影响较大的是乡土小说，主要表现士兵、船夫和湘西少数民族的生活，富有人情美和风俗美。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "清晰",
              "content": "他的代表作有《边城》《长河》，散文集《从文自传》《湘行散记》等。",
              "pauseAfterSeconds": 2.0
            }
          ]
        },
        {
          "pageNumber": 4,
          "scriptItems": [
            {
              "tone": "引导",
              "content": "了解完作者沈从文先生，接下来我们正式进入初读课文、学习字词的环节。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "清晰平缓",
              "content": "我们先来看这一部分的自读提示。",
              "pauseAfterSeconds": 0.5
            },
            {
              "tone": "平和",
              "content": "第一条要求，请大家自由地朗读课文，注意读准字音，读通句子。",
              "pauseAfterSeconds": 1.5
            },
            {
              "tone": "强调",
              "content": "第二条要求，大家在朗读的同时还要边读边思考：围绕“腊八粥”这一线索，作者为我们讲了一个什么样的故事？试着用小标题概括出故事情节。",
              "pauseAfterSeconds": 2.0
            }
          ]
        },
        {
          "pageNumber": 5,
          "scriptItems": [
            {
              "tone": "平缓",
              "content": "刚才我们明确了自读课文的要求，接下来我们先来学习本课的生字词。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "清晰",
              "content": "请大家先跟着页面提示走：读一读下面的词语，注意读准字音，不会认读的生字词圈出来多读几遍。",
              "pauseAfterSeconds": 2.0
            },
            {
              "tone": "强调",
              "content": "我们先来梳理几个重点易错的读音，首先看翘舌音，“粥、肿胀、汤匙、碗盏”都是翘舌音，大家发音的时候要注意把舌尖抬起来。",
              "pauseAfterSeconds": 1.5
            },
            {
              "tone": "提醒",
              "content": "这里有个多音字需要大家格外注意，“匙”在“汤匙”这个词里读chí，而组成“钥匙”的时候就读轻声shi，两个读音不要混淆。",
              "pauseAfterSeconds": 2.0
            },
            {
              "tone": "清晰",
              "content": "接下来是平舌音，本课的平舌音生字只有一个，就是“脏水”的“脏”，读的时候舌尖放平就可以了。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "强调",
              "content": "还有一个轻声的读音也不要读错，“搅和”的“和”在这里读轻声huo。",
              "pauseAfterSeconds": 1.0
            },
            {
              "tone": "平缓",
              "content": "现在我们一起把所有词语通读一遍，加深印象：腊八粥、吞咽、汤匙、碗盏、搅和、肿胀、熬粥、褐色、染缸、脏水、筷子、陈旧、感觉、沸腾、何况、资格、可靠、罢了、要不然、猜想、惊异、总之、解释、浪漫、奈何。",
              "pauseAfterSeconds": 2.0
            },
            {
              "tone": "平缓",
              "content": "这些字词都选自“课前预学单”的第1题，大家课后也可以结合预学单再巩固练习。",
              "pauseAfterSeconds": 1.0
            }
          ]
        }
      ]
    }
    """;

var ffmpegFile = @"C:\lindexi\Application\ffmpeg.exe";

Console.WriteLine("Hello, World!");

/// <summary>
/// 课程讲稿根对象
/// </summary>
public class LectureScript
{
    /// <summary>
    /// 课程名称
    /// </summary>
    [JsonPropertyName("lectureName")]
    public string LectureName { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// 所有页面内容集合
    /// </summary>
    [JsonPropertyName("pages")]
    public List<LecturePage> Pages { get; set; } = new();
}

/// <summary>
/// 单页讲稿对象
/// </summary>
public class LecturePage
{
    /// <summary>
    /// 页码
    /// </summary>
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    /// <summary>
    /// 单页下的讲稿片段集合
    /// </summary>
    [JsonPropertyName("scriptItems")]
    public List<LectureScriptItem> ScriptItems { get; set; } = new();
}

/// <summary>
/// 单个讲稿片段对象
/// </summary>
public class LectureScriptItem
{
    /// <summary>
    /// 播报语气
    /// </summary>
    [JsonPropertyName("tone")]
    public string Tone { get; set; }

    /// <summary>
    /// 播报内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// 本段内容播报结束后的停顿时长（单位：秒）
    /// </summary>
    [JsonPropertyName("pauseAfterSeconds")]
    public double PauseAfterSeconds { get; set; }
}