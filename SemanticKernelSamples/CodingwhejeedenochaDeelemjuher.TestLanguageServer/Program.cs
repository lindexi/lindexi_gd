using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

Stream input = Console.OpenStandardInput();
Stream output = Console.OpenStandardOutput();
string? workspaceUri = null;

while (true)
{
    JsonNode? request = await ReadMessageAsync(input);
    if (request is null)
    {
        break;
    }

    string? method = request["method"]?.GetValue<string>();
    JsonNode? id = request["id"];
    if (method == "initialize")
    {
        workspaceUri = request["params"]?["workspaceFolders"]?[0]?["uri"]?.GetValue<string>();
        await RespondAsync(output, id, new JsonObject { ["capabilities"] = new JsonObject() });
    }
    else if (method == "shutdown")
    {
        await RespondAsync(output, id, null);
    }
    else if (method == "exit")
    {
        break;
    }
    else if (method == "workspace/symbol")
    {
        string query = request["params"]?["query"]?.GetValue<string>() ?? string.Empty;
        JsonArray symbols = query.Contains("Calculator", StringComparison.OrdinalIgnoreCase)
            ? [CreateSymbol(workspaceUri!)]
            : [];
        await RespondAsync(output, id, symbols);
    }
    else if (method is "textDocument/definition" or "textDocument/implementation")
    {
        await RespondAsync(output, id, new JsonArray(CreateLocation(workspaceUri!, "SampleApp/Calculator.cs", 7, 20)));
    }
    else if (method == "textDocument/references")
    {
        await RespondAsync(output, id, new JsonArray(
            CreateLocation(workspaceUri!, "SampleApp/Calculator.cs", 7, 20),
            CreateLocation(workspaceUri!, "SampleApp/Program.cs", 2, 29)));
    }
}

static JsonObject CreateSymbol(string workspaceUri) => new()
{
    ["name"] = "Calculator",
    ["kind"] = 5,
    ["containerName"] = "SampleApp",
    ["location"] = CreateLocation(workspaceUri, "SampleApp/Calculator.cs", 7, 20)
};

static JsonObject CreateLocation(string workspaceUri, string relativePath, int line, int character)
{
    Uri root = new(workspaceUri.EndsWith('/') ? workspaceUri : workspaceUri + "/");
    return new JsonObject
    {
        ["uri"] = new Uri(root, relativePath).AbsoluteUri,
        ["range"] = new JsonObject
        {
            ["start"] = new JsonObject { ["line"] = line, ["character"] = character },
            ["end"] = new JsonObject { ["line"] = line, ["character"] = character + 10 }
        }
    };
}

static async Task<JsonNode?> ReadMessageAsync(Stream input)
{
    int? contentLength = null;
    while (true)
    {
        string? line = await ReadLineAsync(input);
        if (line is null)
        {
            return null;
        }

        if (line.Length == 0)
        {
            break;
        }

        line = line.TrimStart('\uFEFF');
        const string prefix = "Content-Length:";
        if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            contentLength = int.Parse(line[prefix.Length..].Trim());
        }
    }

    byte[] body = new byte[contentLength ?? throw new InvalidDataException()];
    await input.ReadExactlyAsync(body);
    return JsonNode.Parse(body);
}

static async Task<string?> ReadLineAsync(Stream input)
{
    List<byte> bytes = [];
    while (true)
    {
        int value = input.ReadByte();
        if (value < 0)
        {
            return bytes.Count == 0 ? null : Encoding.ASCII.GetString([.. bytes]);
        }

        if (value == '\n')
        {
            if (bytes.Count > 0 && bytes[^1] == '\r')
            {
                bytes.RemoveAt(bytes.Count - 1);
            }

            return Encoding.ASCII.GetString([.. bytes]);
        }

        bytes.Add((byte)value);
    }
}

static async Task RespondAsync(Stream output, JsonNode? id, JsonNode? result)
{
    JsonObject response = new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["result"] = result
    };
    byte[] body = JsonSerializer.SerializeToUtf8Bytes(response);
    byte[] header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");
    await output.WriteAsync(header);
    await output.WriteAsync(body);
    await output.FlushAsync();
}
