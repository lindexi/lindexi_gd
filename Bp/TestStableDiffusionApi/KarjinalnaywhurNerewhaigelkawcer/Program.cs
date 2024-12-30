// See https://aka.ms/new-console-template for more information

using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json.Nodes;

// https://soulteary.com/2022/12/09/use-docker-to-quickly-get-started-with-the-chinese-stable-diffusion-model-taiyi.html

var host = "http://127.0.0.1:56622";

var minSize = 260;
var width = 260;
var height = 260;

width = Math.Max(minSize, width);
height = Math.Max(minSize, height);

string prompt = "一看有几笔";

int steps = 5;

// lang=json
var json =
    $$"""
      {
        "enable_hr": false,
        "denoising_strength": 0,
        "firstphase_width": 0,
        "firstphase_height": 0,
        "prompt": "{{prompt}}",
        "styles": [
          "string"
        ],
        "seed": -1,
        "subseed": -1,
        "subseed_strength": 0,
        "seed_resize_from_h": -1,
        "seed_resize_from_w": -1,
        "batch_size": 1,
        "n_iter": 1,
        "steps": {{steps}},
        "cfg_scale": 7,
        "width": {{width}},
        "height": {{height}},
        "restore_faces": false,
        "tiling": false,
        "negative_prompt": "string",
        "eta": 0,
        "s_churn": 0,
        "s_tmax": 0,
        "s_tmin": 0,
        "s_noise": 1,
        "override_settings": {},
        "sampler_index": "Euler"
      }
      """;

using var httpClient = new HttpClient()
{
    BaseAddress = new Uri(host)
};

var httpResponseMessage = await httpClient.PostAsync("sdapi/v1/txt2img", new StringContent(json,MediaTypeHeaderValue.Parse("application/json")));

httpResponseMessage.EnsureSuccessStatusCode();

var response = await httpResponseMessage.Content.ReadAsStringAsync();

if (JsonNode.Parse(response)?["images"] is JsonArray jsonArray)
{
    var imageBase64 = jsonArray[0]?.ToString();
    if(imageBase64 is not null)
    {
        var image = Convert.FromBase64String(imageBase64);
        await File.WriteAllBytesAsync("1.png", image);
    }
}

//Console.WriteLine(response);
