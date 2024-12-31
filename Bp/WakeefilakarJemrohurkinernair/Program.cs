// See https://aka.ms/new-console-template for more information

using System.Text.Json.Nodes;

var minSize = 512;
var width = 260;
var height = 260;

width = Math.Max(minSize, width);
height = Math.Max(minSize, height);

string prompt = "一看有几笔";

int steps = 20;

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

var jsonNode = JsonNode.Parse(json);
var promptText = jsonNode["prompt"].ToString();

Console.WriteLine("Hello, World!");
