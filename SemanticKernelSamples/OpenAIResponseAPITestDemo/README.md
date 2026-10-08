# Migrate to the Responses API

> For the complete documentation index, see [llms.txt](/llms.txt). Markdown versions of documentation pages are available by appending `.md` to the page URL.

The [Responses API](https://developers.openai.com/api/reference/resources/responses) is our new API primitive, an evolution of [Chat Completions](https://developers.openai.com/api/reference/resources/chat) which brings added simplicity and powerful agentic primitives to your integrations.

**While Chat Completions remains supported, Responses is recommended for all new projects.**

## About the Responses API

The Responses API is a unified interface for building powerful, agent-like applications. It contains:

- Built-in tools like [web search](https://developers.openai.com/api/docs/guides/tools-web-search), [file search](https://developers.openai.com/api/docs/guides/tools-file-search), [computer use](https://developers.openai.com/api/docs/guides/tools-computer-use), [code interpreter](https://developers.openai.com/api/docs/guides/tools-code-interpreter), and [remote MCPs](https://developers.openai.com/api/docs/guides/tools-connectors-mcp).
- Seamless multi-turn interactions that allow you to pass previous responses for higher accuracy reasoning results.
- Native multimodal support for text and images.

## Responses benefits

The Responses API contains several benefits over Chat Completions:

- **Better performance**: Using reasoning models, like GPT-5, with Responses will result in better model intelligence when compared to Chat Completions. Our internal evals reveal a 3% improvement in SWE-bench with same prompt and setup.
- **Agentic by default**: The Responses API is an agentic loop, allowing the model to call multiple tools, like `web_search`, `image_generation`, `file_search`, `code_interpreter`, remote MCP servers, as well as your own custom functions, within the span of one API request.
- **Lower costs**: Results in lower costs due to improved cache utilization (40% to 80% improvement when compared to Chat Completions in internal tests).
- **Stateful context**: Use `store: true` to maintain state from turn to turn, preserving reasoning and tool context from turn-to-turn.
- **Flexible inputs**: Pass a string with input or a list of messages; use instructions for system-level guidance.
- **Encrypted reasoning**: Opt-out of statefulness while still benefiting from advanced reasoning.
- **Future-proof**: Future-proofed for upcoming models.




| Capabilities        | Chat Completions API  | Responses API         |
| ------------------- | --------------------- | --------------------- |
| Text generation     | | |
| Audio               | | Coming soon           |
| Vision              | | |
| Structured Outputs  | | |
| Function calling    | | |
| Web search          | | |
| File search         | | |
| Computer use        | | |
| Code interpreter    | | |
| MCP                 | | |
| Image generation    | | |
| Reasoning summaries | | |




### Examples

See how the Responses API compares to the Chat Completions API in specific scenarios.

#### Messages vs. Items

Both APIs make it easy to generate output from our models. The input to, and result of, a call to Chat completions is an array of _Messages_, while
the Responses API uses _Items_. An Item is a union of many types, representing the range of possibilities
of model actions. A `message` is a type of Item, as is a `function_call` or `function_call_output`. Unlike a Chat Completions Message, where
many concerns are glued together into one object, Items are distinct from one another and better represent the basic unit of model context.

Additionally, Chat Completions can return multiple parallel generations as `choices`, using the `n` param. In Responses, we've removed this param, leaving only one generation.



  Chat Completions API

```python
from openai import OpenAI

client = OpenAI()

completion = client.chat.completions.create(
    model="gpt-6-astra",
    messages=[
        {
            "role": "user",
            "content": "Write a one-sentence bedtime story about a unicorn.",
        }
    ],
)

print(completion.choices[0].message.content)
```

```ruby
require "openai"

client = OpenAI::Client.new
completion = client.chat.completions.create(
  model: "gpt-6-astra",
  messages: [
    {
      role: :user,
      content: "Write a one-sentence bedtime story about a unicorn."
    }
  ]
)
puts(completion.choices.fetch(0).message.content)
```

  Responses API

```python
from openai import OpenAI

client = OpenAI()

response = client.responses.create(
    model="gpt-6-astra",
    input="Write a one-sentence bedtime story about a unicorn.",
)

print(response.output_text)
```

```ruby
require "openai"

client = OpenAI::Client.new
response = client.responses.create(
  model: "gpt-6-astra",
  input: "Write a one-sentence bedtime story about a unicorn."
)
puts(response.output_text)
```




When you get a response back from the Responses API, the fields differ slightly.
Instead of a `message`, you receive a typed `response` object with its own `id`.
Responses are stored by default. Chat completions are stored by default for new accounts.
To disable storage when using either API, set `store: false`.

The objects you receive back from these APIs will differ slightly. In Chat Completions, you receive an array of
`choices`, each containing a `message`. In Responses, you receive an array of Items labeled `output`.



#### Chat Completions API

```json
{
    "id": "chatcmpl-C9EDpkjH60VPPIB86j2zIhiR8kWiC",
    "object": "chat.completion",
    "created": 1756315657,
    "model": "gpt-5.5",
    "choices": [
      {
        "index": 0,
        "message": {
          "role": "assistant",
          "content": "Under a blanket of starlight, a sleepy unicorn tiptoed through moonlit meadows, gathering dreams like dew to tuck beneath its silver mane until morning.",
          "refusal": null,
          "annotations": []
        },
        "finish_reason": "stop"
      }
    ],
    ...
}
```

#### Responses API

```json
{
    "id": "resp_68af4030592c81938ec0a5fbab4a3e9f05438e46b5f69a3b",
    "object": "response",
    "created_at": 1756315696,
    "model": "gpt-5.5",
    "output": [
      {
        "id": "rs_68af4030baa48193b0b43b4c2a176a1a05438e46b5f69a3b",
        "type": "reasoning",
        "content": [],
        "summary": []
      },
      {
        "id": "msg_68af40337e58819392e935fb404414d005438e46b5f69a3b",
        "type": "message",
        "status": "completed",
        "content": [
          {
            "type": "output_text",
            "annotations": [],
            "logprobs": [],
            "text": "Under a quilt of moonlight, a drowsy unicorn wandered through quiet meadows, brushing blossoms with her glowing horn so they sighed soft lullabies that carried every dreamer gently to sleep."
          }
        ],
        "role": "assistant"
      }
    ],
    ...
}
```



### Additional differences

- Responses are stored by default. Chat completions are stored by default for new accounts. To disable storage in either API, set `store: false`.
- [Reasoning](https://developers.openai.com/api/docs/guides/reasoning) models have a richer experience in the Responses API with [improved tool usage](https://developers.openai.com/api/docs/guides/reasoning#keeping-reasoning-items-in-context). Starting with GPT-5.4, Chat Completions does not support tool calling with `reasoning_effort` values other than `none`.
- Structured Outputs API shape is different. Instead of `response_format`, use `text.format` in Responses. Learn more in the [Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs) guide.
- The function-calling API shape is different, both for the function config on the request, and function calls sent back in the response. See the full difference in the [function calling guide](https://developers.openai.com/api/docs/guides/function-calling).
- The Responses SDK has an `output_text` helper, which the Chat Completions SDK does not have.
- In Chat Completions, conversation state must be managed manually. The Responses API has compatibility with the [Conversations API](https://developers.openai.com/api/docs/guides/conversation-state?api-mode=responses#using-the-conversations-api) for persistent conversations, or the ability to pass a `previous_response_id` to easily chain Responses together.

## Migrating from Chat Completions

Treat migration as three related changes: send requests to `/v1/responses`, read output from a typed `output` array, and choose how your application will carry state between turns.

### 1. Update generation endpoints

Start by updating your generation endpoints from `post /v1/chat/completions` to `post /v1/responses`.

If you are not using functions or multimodal inputs, simple message inputs are compatible from one API to the other:

Reuse simple message input

```javascript
const context = [
  { role: "system", content: "You are a helpful assistant." },
  { role: "user", content: "Hello!" },
];

const completion = await client.chat.completions.create({
  model: "gpt-6-astra",
  messages: context,
});

const response = await client.responses.create({
  model: "gpt-6-astra",
  input: context,
});
```

```python
context = [
    {"role": "system", "content": "You are a helpful assistant."},
    {"role": "user", "content": "Hello!"},
]

completion = client.chat.completions.create(model="gpt-6-astra", messages=context)

response = client.responses.create(model="gpt-6-astra", input=context)
```

```go
package main

import (
    "context"
    "fmt"

    "github.com/openai/openai-go/v3"
    "github.com/openai/openai-go/v3/responses"
)

func main() {
    client := openai.NewClient()

    completion, err := client.Chat.Completions.New(context.Background(), openai.ChatCompletionNewParams{
        Model: "gpt-6-astra",
        Messages: []openai.ChatCompletionMessageParamUnion{
            openai.SystemMessage("You are a helpful assistant."),
            openai.UserMessage("Hello!"),
        },
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(completion.Choices[0].Message.Content)

    response, err := client.Responses.New(context.Background(), responses.ResponseNewParams{
        Model: "gpt-6-astra",
        Input: responses.ResponseNewParamsInputUnion{OfInputItemList: responses.ResponseInputParam{
            responses.ResponseInputItemParamOfMessage("You are a helpful assistant.", responses.EasyInputMessageRoleSystem),
            responses.ResponseInputItemParamOfMessage("Hello!", responses.EasyInputMessageRoleUser),
        }},
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(response.OutputText())
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.models.chat.completions.ChatCompletionCreateParams;
import com.openai.models.responses.EasyInputMessage;
import com.openai.models.responses.ResponseCreateParams;
import com.openai.models.responses.ResponseInputItem;
import java.util.List;

var completion =
    client
        .chat()
        .completions()
        .create(
            ChatCompletionCreateParams.builder()
                .model("gpt-6-astra")
                .addSystemMessage("You are a helpful assistant.")
                .addUserMessage("Hello!")
                .build());
completion.choices().stream()
    .flatMap(choice -> choice.message().content().stream())
    .forEach(System.out::println);

var response =
    client
        .responses()
        .create(
            ResponseCreateParams.builder()
                .model("gpt-6-astra")
                .inputOfResponse(
                    List.of(
                        ResponseInputItem.ofEasyInputMessage(
                            EasyInputMessage.builder()
                                .role(EasyInputMessage.Role.SYSTEM)
                                .content("You are a helpful assistant.")
                                .build()),
                        ResponseInputItem.ofEasyInputMessage(
                            EasyInputMessage.builder()
                                .role(EasyInputMessage.Role.USER)
                                .content("Hello!")
                                .build())))
                .build());
response.output().stream()
    .flatMap(item -> item.message().stream())
    .flatMap(message -> message.content().stream())
    .flatMap(content -> content.outputText().stream())
    .forEach(text -> System.out.println(text.text()));
```

```csharp
using OpenAI.Chat;
using OpenAI.Responses;
#pragma warning disable OPENAI001

string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
string model = "gpt-6-astra";

ChatClient chat = new(model, key);

ChatCompletion completion = await chat.CompleteChatAsync(
    [
        new SystemChatMessage("You are a helpful assistant."),
        new UserChatMessage("Hello!"),
    ]
);
Console.WriteLine(completion.Content[0].Text);

ResponsesClient responses = new(key);

ResponseResult response = await responses.CreateResponseAsync(
    model,
    [
        ResponseItem.CreateSystemMessageItem("You are a helpful assistant."),
        ResponseItem.CreateUserMessageItem("Hello!"),
    ]
);
Console.WriteLine(response.GetOutputText());
```

```ruby
require "openai"

client = OpenAI::Client.new
messages = [
  {
    role: :system,
    content: "You are a helpful assistant."
  },
  {
    role: :user,
    content: "Hello!"
  }
]

completion = client.chat.completions.create(
  model: "gpt-6-astra",
  messages: messages
)
puts(completion.choices.fetch(0).message.content)

response = client.responses.create(
  model: "gpt-6-astra",
  input: messages
)
puts(response.output_text)
```

```bash
INPUT='[
  { "role": "system", "content": "You are a helpful assistant." },
  { "role": "user", "content": "Hello!" }
]'

curl -s https://api.openai.com/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $OPENAI_API_KEY" \
  -d "{
    \"model\": \"gpt-6-astra\",
    \"messages\": $INPUT
  }"

curl -s https://api.openai.com/v1/responses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $OPENAI_API_KEY" \
  -d "{
    \"model\": \"gpt-6-astra\",
    \"input\": $INPUT
  }"
```




Chat Completions

    With Chat Completions, you create a `messages` array and read the model text
    from `completion.choices[0].message.content`.
    Generate text from a model

```javascript
import OpenAI from "openai";
const client = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });

const completion = await client.chat.completions.create({
  model: "gpt-6-astra",
  messages: [
    { role: "system", content: "You are a helpful assistant." },
    { role: "user", content: "Hello!" },
  ],
});
console.log(completion.choices[0].message.content);
```

```python
from openai import OpenAI

client = OpenAI()

completion = client.chat.completions.create(
    model="gpt-6-astra",
    messages=[
        {"role": "system", "content": "You are a helpful assistant."},
        {"role": "user", "content": "Hello!"},
    ],
)
print(completion.choices[0].message.content)
```

```go
package main

import (
    "context"
    "fmt"

    "github.com/openai/openai-go/v3"
)

func main() {
    client := openai.NewClient()

    completion, err := client.Chat.Completions.New(context.Background(), openai.ChatCompletionNewParams{
        Model: "gpt-6-astra",
        Messages: []openai.ChatCompletionMessageParamUnion{
            openai.SystemMessage("You are a helpful assistant."),
            openai.UserMessage("Hello!"),
        },
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(completion.Choices[0].Message.Content)
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.models.chat.completions.ChatCompletionCreateParams;

ChatCompletionCreateParams params =
    ChatCompletionCreateParams.builder()
        .model("gpt-6-astra")
        .addSystemMessage("You are a helpful assistant.")
        .addUserMessage("Hello!")
        .build();

client.chat().completions().create(params).choices().stream()
    .flatMap(choice -> choice.message().content().stream())
    .forEach(System.out::println);
```

```csharp
using OpenAI.Chat;

string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
string model = "gpt-6-astra";
ChatClient client = new(model, key);

ChatCompletion completion = await client.CompleteChatAsync(
    [
        new SystemChatMessage("You are a helpful assistant."),
        new UserChatMessage("Hello!"),
    ]
);

Console.WriteLine(completion.Content[0].Text);
```

```ruby
require "openai"

client = OpenAI::Client.new

completion = client.chat.completions.create(
  model: "gpt-6-astra",
  messages: [
    {
      role: :system,
      content: "You are a helpful assistant."
    },
    {
      role: :user,
      content: "Hello!"
    }
  ]
)

puts(completion.choices.fetch(0).message.content)
```

```bash
curl https://api.openai.com/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $OPENAI_API_KEY" \
  -d '{
      "model": "gpt-6-astra",
      "messages": [
          {"role": "system", "content": "You are a helpful assistant."},
          {"role": "user", "content": "Hello!"}
      ]
  }'
```


  

  

    
Responses

    With Responses, you can separate `instructions` and `input` at the top level
    and read generated text from `response.output_text`.
    Generate text from a model

```javascript
import OpenAI from "openai";
const client = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });

const response = await client.responses.create({
  model: "gpt-6-astra",
  instructions: "You are a helpful assistant.",
  input: "Hello!",
});

console.log(response.output_text);
```

```python
from openai import OpenAI

client = OpenAI()

response = client.responses.create(
    model="gpt-6-astra", instructions="You are a helpful assistant.", input="Hello!"
)
print(response.output_text)
```

```go
package main

import (
    "context"
    "fmt"

    "github.com/openai/openai-go/v3"
    "github.com/openai/openai-go/v3/responses"
)

func main() {
    client := openai.NewClient()

    response, err := client.Responses.New(context.Background(), responses.ResponseNewParams{
        Model:        "gpt-6-astra",
        Instructions: openai.String("You are a helpful assistant."),
        Input:        responses.ResponseNewParamsInputUnion{OfString: openai.String("Hello!")},
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(response.OutputText())
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.models.responses.ResponseCreateParams;

ResponseCreateParams params =
    ResponseCreateParams.builder()
        .model("gpt-6-astra")
        .input("Hello!")
        .instructions("You are a helpful assistant.")
        .build();

client.responses().create(params).output().stream()
    .flatMap(item -> item.message().stream())
    .flatMap(message -> message.content().stream())
    .flatMap(content -> content.outputText().stream())
    .forEach(text -> System.out.println(text.text()));
```

```csharp
using OpenAI.Responses;
#pragma warning disable OPENAI001

string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
ResponsesClient client = new(key);

CreateResponseOptions options = new()
{
    Model = "gpt-6-astra",
    Instructions = "You are a helpful assistant.",
};
options.InputItems.Add(ResponseItem.CreateUserMessageItem("Hello!"));

ResponseResult response = await client.CreateResponseAsync(options);

Console.WriteLine(response.GetOutputText());
```

```ruby
require "openai"

client = OpenAI::Client.new

response = client.responses.create(
  model: "gpt-6-astra",
  instructions: "You are a helpful assistant.",
  input: "Hello!"
)

puts(response.output_text)
```

```bash
curl https://api.openai.com/v1/responses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $OPENAI_API_KEY" \
  -d '{
      "model": "gpt-6-astra",
      "instructions": "You are a helpful assistant.",
      "input": "Hello!"
  }'
```



### 2. Map Messages to Items

Chat Completions uses `messages` as both input and output. Responses uses `input` and `output` arrays of typed Items. A `message` is one Item type, alongside Items such as `reasoning`, `function_call`, and `function_call_output`.

| Chat Completions concept      | Responses mapping                                                                                      |
| ----------------------------- | ------------------------------------------------------------------------------------------------------ |
| `messages[]`                  | `input`, as a string or an array of input Items                                                        |
| System or developer guidance  | Top-level `instructions`, or compatible message Items when you need to preserve an existing transcript |
| User message                  | An input message Item with `role: "user"`                                                              |
| Assistant message             | An output message Item in `response.output`; pass it back in `input` if you manually manage state      |
| Tool or function call         | A `function_call` output Item                                                                          |
| Tool or function result       | A `function_call_output` input Item linked to the call with `call_id`                                  |
| Multiple generations with `n` | Not available in Responses; make separate requests if you need multiple candidate outputs              |

When you only need the final text, use the SDK `output_text` helper. When your flow uses reasoning, tools, or multimodal output, iterate over `response.output` and handle each Item by its `type`.

### 3. Update multi-turn conversations

If you have multi-turn conversations in your application, update your context logic. Responses gives you three common state-management options:

- Use `previous_response_id` when you want OpenAI to manage prior response context. Resend stable `instructions` on each request, because `previous_response_id` does not carry over the previous response's top-level `instructions`.
- Pass prior `output` Items back into the next request when you need to manage or trim context yourself.
- Use the [Conversations API](https://developers.openai.com/api/docs/guides/conversation-state?api-mode=responses#using-the-conversations-api) when you need a persistent conversation object.



Chat Completions

    In Chat Completions, you store the transcript and send the accumulated
    `messages` array on each request.
    Multi-turn conversation

```javascript
let messages = [
  { role: "system", content: "You are a helpful assistant." },
  { role: "user", content: "What is the capital of France?" },
];
const res1 = await client.chat.completions.create({
  model: "gpt-6-astra",
  messages,
});

messages = messages.concat([res1.choices[0].message]);
messages.push({ role: "user", content: "And its population?" });

const res2 = await client.chat.completions.create({
  model: "gpt-6-astra",
  messages,
});
```

```python
messages = [
    {"role": "system", "content": "You are a helpful assistant."},
    {"role": "user", "content": "What is the capital of France?"},
]
res1 = client.chat.completions.create(model="gpt-6-astra", messages=messages)

messages += [res1.choices[0].message]
messages += [{"role": "user", "content": "And its population?"}]

res2 = client.chat.completions.create(model="gpt-6-astra", messages=messages)
```

```go
package main

import (
    "context"
    "fmt"

    "github.com/openai/openai-go/v3"
)

func main() {
    client := openai.NewClient()
    messages := []openai.ChatCompletionMessageParamUnion{
        openai.SystemMessage("You are a helpful assistant."),
        openai.UserMessage("What is the capital of France?"),
    }

    first, err := client.Chat.Completions.New(context.Background(), openai.ChatCompletionNewParams{Model: "gpt-6-astra", Messages: messages})
    if err != nil {
        panic(err)
    }
    messages = append(messages, openai.AssistantMessage(first.Choices[0].Message.Content), openai.UserMessage("And its population?"))

    second, err := client.Chat.Completions.New(context.Background(), openai.ChatCompletionNewParams{Model: "gpt-6-astra", Messages: messages})
    if err != nil {
        panic(err)
    }
    fmt.Println(second.Choices[0].Message.Content)
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.models.chat.completions.ChatCompletionCreateParams;

var params =
    ChatCompletionCreateParams.builder()
        .model("gpt-6-astra")
        .addSystemMessage("You are a helpful assistant.")
        .addUserMessage("What is the capital of France?")
        .build();
var first = client.chat().completions().create(params);

var second =
    client
        .chat()
        .completions()
        .create(
            params.toBuilder()
                .addAssistantMessage(first.choices().get(0).message().content().orElseThrow())
                .addUserMessage("And its population?")
                .build());
second.choices().stream()
    .flatMap(choice -> choice.message().content().stream())
    .forEach(System.out::println);
```

```csharp
using OpenAI.Chat;

string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
string model = "gpt-6-astra";
ChatClient client = new(model, key);

List<ChatMessage> messages =
[
    new SystemChatMessage("You are a helpful assistant."),
    new UserChatMessage("What is the capital of France?"),
];
ChatCompletion first = await client.CompleteChatAsync(messages);

messages.Add(new AssistantChatMessage(first));
messages.Add(new UserChatMessage("And its population?"));
ChatCompletion second = await client.CompleteChatAsync(messages);

Console.WriteLine(second.Content[0].Text);
```

```ruby
require "openai"

client = OpenAI::Client.new
messages = [
  {
    role: :system,
    content: "You are a helpful assistant."
  },
  {
    role: :user,
    content: "What is the capital of France?"
  }
]

first = client.chat.completions.create(
  model: "gpt-6-astra",
  messages: messages
)
messages << {
  role: :assistant,
  content: first.choices.fetch(0).message.content
}
messages << {
  role: :user,
  content: "And its population?"
}

second = client.chat.completions.create(
  model: "gpt-6-astra",
  messages: messages
)

puts(second.choices.fetch(0).message.content)
```


  

  

    
Responses

    With Responses, you can manually pass outputs from one response into the
    input of another.
    Multi-turn conversation

```javascript
import { toResponseInputItems } from "openai/lib/responses/ResponseInputItems";

let context = [{ role: "user", content: "What is the capital of France?" }];

const res1 = await client.responses.create({
  model: "gpt-6-astra",
  input: context,
});

// Append the first response’s output to context
context = context.concat(toResponseInputItems(res1.output));

// Add the next user message
context.push({ role: "user", content: "And its population?" });

const res2 = await client.responses.create({
  model: "gpt-6-astra",
  input: context,
});
```

```python
context = [{"role": "user", "content": "What is the capital of France?"}]
res1 = client.responses.create(
    model="gpt-6-astra",
    input=context,
)

# Append the first response's output to context
context += res1.output

# Add the next user message
context += [{"role": "user", "content": "And its population?"}]

res2 = client.responses.create(
    model="gpt-6-astra",
    input=context,
)
```

```go
package main

import (
    "context"
    "encoding/json"
    "fmt"

    "github.com/openai/openai-go/v3"
    "github.com/openai/openai-go/v3/responses"
)

func main() {
    client := openai.NewClient()
    contextItems := responses.ResponseInputParam{
        responses.ResponseInputItemParamOfMessage("What is the capital of France?", responses.EasyInputMessageRoleUser),
    }
    first, err := client.Responses.New(context.Background(), responses.ResponseNewParams{
        Model: "gpt-6-astra",
        Input: responses.ResponseNewParamsInputUnion{OfInputItemList: contextItems},
    })
    if err != nil {
        panic(err)
    }
    contextItems = append(contextItems, outputAsInput(first.Output)...)
    contextItems = append(contextItems, responses.ResponseInputItemParamOfMessage("And its population?", responses.EasyInputMessageRoleUser))
    second, err := client.Responses.New(context.Background(), responses.ResponseNewParams{
        Model: "gpt-6-astra",
        Input: responses.ResponseNewParamsInputUnion{OfInputItemList: contextItems},
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(second.OutputText())
}

func outputAsInput(output []responses.ResponseOutputItemUnion) []responses.ResponseInputItemUnionParam {
    input := make([]responses.ResponseInputItemUnionParam, 0, len(output))
    for _, item := range output {
        var converted responses.ResponseInputItemUnion
        if err := json.Unmarshal([]byte(item.RawJSON()), &converted); err != nil {
            panic(err)
        }
        input = append(input, converted.ToParam())
    }
    return input
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.core.JsonValue;
import com.openai.models.responses.EasyInputMessage;
import com.openai.models.responses.ResponseCreateParams;
import com.openai.models.responses.ResponseInputItem;
import java.util.ArrayList;

var history = new ArrayList<ResponseInputItem>();
history.add(
    ResponseInputItem.ofEasyInputMessage(
        EasyInputMessage.builder()
            .role(EasyInputMessage.Role.USER)
            .content("What is the capital of France?")
            .build()));

var first =
    client
        .responses()
        .create(
            ResponseCreateParams.builder().model("gpt-6-astra").inputOfResponse(history).build());
first.output().stream()
    .map(item -> JsonValue.from(item).convert(ResponseInputItem.class))
    .forEach(history::add);
history.add(
    ResponseInputItem.ofEasyInputMessage(
        EasyInputMessage.builder()
            .role(EasyInputMessage.Role.USER)
            .content("And its population?")
            .build()));

client
    .responses()
    .create(ResponseCreateParams.builder().model("gpt-6-astra").inputOfResponse(history).build())
    .output()
    .stream()
    .flatMap(item -> item.message().stream())
    .flatMap(message -> message.content().stream())
    .flatMap(content -> content.outputText().stream())
    .forEach(text -> System.out.println(text.text()));
```

```csharp
using OpenAI.Responses;
#pragma warning disable OPENAI001

string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
ResponsesClient client = new(key);

List<ResponseItem> history =
[
    ResponseItem.CreateUserMessageItem("What is the capital of France?"),
];

ResponseResult first = await client.CreateResponseAsync("gpt-6-astra", history);
history.AddRange(first.OutputItems);
history.Add(ResponseItem.CreateUserMessageItem("And its population?"));

ResponseResult second = await client.CreateResponseAsync("gpt-6-astra", history);
Console.WriteLine(second.GetOutputText());
```

```ruby
require "openai"

client = OpenAI::Client.new
context = [
  {
    role: :user,
    content: "What is the capital of France?"
  }
]

first = client.responses.create(
  model: "gpt-6-astra",
  input: context
)
context.concat(first.output)
context << {
  role: :user,
  content: "And its population?"
}

second = client.responses.create(
  model: "gpt-6-astra",
  input: context
)

puts(second.output_text)
```

    You can also use `previous_response_id` to reference the previous response
    and create response chains or forks.
    Multi-turn conversation

```javascript
const res1 = await client.responses.create({
  model: "gpt-6-astra",
  input: "What is the capital of France?",
  store: true,
});

const res2 = await client.responses.create({
  model: "gpt-6-astra",
  input: "And its population?",
  previous_response_id: res1.id,
  store: true,
});
```

```python
res1 = client.responses.create(
    model="gpt-6-astra", input="What is the capital of France?", store=True
)

res2 = client.responses.create(
    model="gpt-6-astra",
    input="And its population?",
    previous_response_id=res1.id,
    store=True,
)
```

```go
package main

import (
    "context"
    "fmt"

    "github.com/openai/openai-go/v3"
    "github.com/openai/openai-go/v3/responses"
)

func main() {
    client := openai.NewClient()

    first, err := client.Responses.New(context.Background(), responses.ResponseNewParams{
        Model: "gpt-6-astra",
        Store: openai.Bool(true),
        Input: responses.ResponseNewParamsInputUnion{OfString: openai.String("What is the capital of France?")},
    })
    if err != nil {
        panic(err)
    }

    second, err := client.Responses.New(context.Background(), responses.ResponseNewParams{
        Model:              "gpt-6-astra",
        Store:              openai.Bool(true),
        PreviousResponseID: openai.String(first.ID),
        Input:              responses.ResponseNewParamsInputUnion{OfString: openai.String("And its population?")},
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(second.OutputText())
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.models.responses.ResponseCreateParams;

var first =
    client
        .responses()
        .create(
            ResponseCreateParams.builder()
                .model("gpt-6-astra")
                .input("What is the capital of France?")
                .store(true)
                .build());

var second =
    client
        .responses()
        .create(
            ResponseCreateParams.builder()
                .model("gpt-6-astra")
                .input("And its population?")
                .previousResponseId(first.id())
                .store(true)
                .build());
second.output().stream()
    .flatMap(item -> item.message().stream())
    .flatMap(message -> message.content().stream())
    .flatMap(content -> content.outputText().stream())
    .forEach(text -> System.out.println(text.text()));
```

```csharp
using OpenAI.Responses;
#pragma warning disable OPENAI001

string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
ResponsesClient client = new(key);

ResponseResult first = await client.CreateResponseAsync(
    "gpt-6-astra",
    "What is the capital of France?"
);
ResponseResult second = await client.CreateResponseAsync(
    "gpt-6-astra",
    "And its population?",
    previousResponseId: first.Id
);

Console.WriteLine(second.GetOutputText());
```

```ruby
require "openai"

client = OpenAI::Client.new

first = client.responses.create(
  model: "gpt-6-astra",
  input: "What is the capital of France?",
  store: true
)

second = client.responses.create(
  model: "gpt-6-astra",
  previous_response_id: first.id,
  input: "And its population?",
  store: true
)

puts(second.output_text)
```



Even when using `previous_response_id`, all previous input tokens for responses in the chain are billed as input tokens in the API.

### 4. Decide when to use statefulness

Responses are stored by default. Chat Completions are stored by default for new accounts. To disable storage in either API, set `store: false`.

Some organizations, such as those with Zero Data Retention (ZDR) requirements, cannot use the Responses API in a stateful way due to compliance or data retention policies. To support these cases, OpenAI offers encrypted reasoning items, allowing you to keep your workflow stateless while still benefiting from reasoning items.

To disable statefulness but still take advantage of reasoning:

- Set `store: false` in the [store field](https://developers.openai.com/api/reference/resources/responses/methods/create#responses_create-store).
- Preserve and replay every returned reasoning item. Each item includes `encrypted_content` by default when you create a response.

The API will then return an encrypted version of the reasoning tokens, which you can pass back in future requests just like regular reasoning items.
For ZDR organizations, OpenAI enforces `store: false` automatically. When a request includes `encrypted_content`, it is decrypted in memory, used for generating the next response, and then securely discarded. Any new reasoning tokens are immediately encrypted and returned to you, ensuring no intermediate state is persisted.

### 5. Update function definitions and outputs

There are two minor, but notable, differences in how functions are defined between Chat Completions and Responses.

1. In Chat Completions, function definitions are externally tagged. In Responses, they are internally tagged.
2. In Chat Completions, functions are non-strict by default. In Responses, omitting `strict` attempts strict mode; if the schema cannot be made compatible, Responses falls back to non-strict, best-effort function calling and returns the resolved tool with `strict: false`. To keep non-strict behavior in Responses explicitly, set `strict: false`.

The Responses API function example on the right is functionally equivalent to the Chat Completions example on the left.



#### Chat Completions API

```json
{
    "type": "function",
    "function": {
      "name": "get_weather",
      "description": "Determine weather in my location",
      "strict": true,
      "parameters": {
        "type": "object",
        "properties": {
          "location": {
            "type": "string"
          }
        },
        "additionalProperties": false,
        "required": [
          "location"
        ]
      }
    }
}
```

#### Responses API

```json
{
    "type": "function",
    "name": "get_weather",
    "description": "Determine weather in my location",
    "parameters": {
      "type": "object",
      "properties": {
        "location": {
          "type": "string"
        }
      },
      "additionalProperties": false,
      "required": [
        "location"
      ]
    }
}
```



#### Follow function-calling best practices

In Responses, tool calls and their outputs are two distinct types of Items that are correlated using a `call_id`. See
the [function calling docs](https://developers.openai.com/api/docs/guides/function-calling#function-tool-example) for more detail on how function calling works in Responses.

### 6. Update Structured Outputs definitions

In the Responses API, Structured Outputs definitions have moved from `response_format` to `text.format`:



Chat Completions

    Structured Outputs

```javascript
const completion = await openai.chat.completions.create({
  model: "gpt-6-astra",
  messages: [
    {
      role: "user",
      content: "Jane, 54 years old",
    },
  ],
  response_format: {
    type: "json_schema",
    json_schema: {
      name: "person",
      strict: true,
      schema: {
        type: "object",
        properties: {
          name: {
            type: "string",
            minLength: 1,
          },
          age: {
            type: "number",
            minimum: 0,
            maximum: 130,
          },
        },
        required: ["name", "age"],
        additionalProperties: false,
      },
    },
  },
  reasoning_effort: "medium",
});
```

```python
from openai import OpenAI

client = OpenAI()

response = client.chat.completions.create(
    model="gpt-6-astra",
    messages=[
        {
            "role": "user",
            "content": "Jane, 54 years old",
        }
    ],
    response_format={
        "type": "json_schema",
        "json_schema": {
            "name": "person",
            "strict": True,
            "schema": {
                "type": "object",
                "properties": {
                    "name": {"type": "string", "minLength": 1},
                    "age": {"type": "number", "minimum": 0, "maximum": 130},
                },
                "required": ["name", "age"],
                "additionalProperties": False,
            },
        },
    },
    reasoning_effort="medium",
)
```

```go
package main

import (
    "context"
    "fmt"

    "github.com/openai/openai-go/v3"
    "github.com/openai/openai-go/v3/shared"
)

func main() {
    client := openai.NewClient()
    schema := map[string]any{
        "type": "object",
        "properties": map[string]any{
            "name": map[string]any{"type": "string", "minLength": 1},
            "age":  map[string]any{"type": "number", "minimum": 0, "maximum": 130},
        },
        "required":             []string{"name", "age"},
        "additionalProperties": false,
    }

    completion, err := client.Chat.Completions.New(context.Background(), openai.ChatCompletionNewParams{
        Model:           "gpt-6-astra",
        ReasoningEffort: openai.ReasoningEffortMedium,
        Messages: []openai.ChatCompletionMessageParamUnion{
            openai.UserMessage("Jane, 54 years old"),
        },
        ResponseFormat: openai.ChatCompletionNewParamsResponseFormatUnion{
            OfJSONSchema: &shared.ResponseFormatJSONSchemaParam{JSONSchema: shared.ResponseFormatJSONSchemaJSONSchemaParam{
                Name: "person", Strict: openai.Bool(true), Schema: schema,
            }},
        },
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(completion.Choices[0].Message.Content)
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.core.JsonValue;
import com.openai.models.ReasoningEffort;
import com.openai.models.chat.completions.ChatCompletionCreateParams;
import java.util.List;
import java.util.Map;

ChatCompletionCreateParams params =
    ChatCompletionCreateParams.builder()
        .model("gpt-6-astra")
        .reasoningEffort(ReasoningEffort.MEDIUM)
        .addUserMessage("Jane, 54 years old")
        .putAdditionalBodyProperty(
            "response_format",
            JsonValue.from(
                Map.of(
                    "type",
                    "json_schema",
                    "json_schema",
                    Map.of(
                        "name",
                        "person",
                        "strict",
                        true,
                        "schema",
                        Map.of(
                            "type",
                            "object",
                            "properties",
                            Map.of(
                                "name",
                                Map.of("type", "string", "minLength", 1),
                                "age",
                                Map.of("type", "number", "minimum", 0, "maximum", 130)),
                            "required",
                            List.of("name", "age"),
                            "additionalProperties",
                            false)))))
        .build();

client.chat().completions().create(params).choices().stream()
    .flatMap(choice -> choice.message().content().stream())
    .forEach(System.out::println);
```

```csharp
using OpenAI.Chat;
#pragma warning disable OPENAI001

string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
string model = "gpt-6-astra";
ChatClient client = new(model, key);

BinaryData schema = BinaryData.FromString(
    """
    {
      "type": "object",
      "properties": {
        "name": { "type": "string", "minLength": 1 },
        "age": { "type": "number", "minimum": 0, "maximum": 130 }
      },
      "required": ["name", "age"],
      "additionalProperties": false
    }
    """
);
ChatCompletionOptions options = new()
{
    ReasoningEffortLevel = ChatReasoningEffortLevel.Medium,
    ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
        "person",
        schema,
        jsonSchemaIsStrict: true
    ),
};

ChatCompletion completion = await client.CompleteChatAsync(
    [new UserChatMessage("Jane, 54 years old")],
    options
);

Console.WriteLine(completion.Content[0].Text);
```

```ruby
require "openai"

client = OpenAI::Client.new
schema = {
  type: "object",
  properties: {
    name: {
      type: "string",
      minLength: 1
    },
    age: {
      type: "number",
      minimum: 0,
      maximum: 130
    }
  },
  required: ["name", "age"],
  additionalProperties: false
}

completion = client.chat.completions.create(
  model: "gpt-6-astra",
  reasoning_effort: :medium,
  messages: [
    {
      role: :user,
      content: "Jane, 54 years old"
    }
  ],
  response_format: {
    type: :json_schema,
    json_schema: {
      name: "person",
      strict: true,
      schema: schema
    }
  }
)

puts(completion.choices.fetch(0).message.content)
```

```bash
curl https://api.openai.com/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $OPENAI_API_KEY" \
  -d '{
  "model": "gpt-6-astra",
  "messages": [
    {
      "role": "user",
      "content": "Jane, 54 years old"
    }
  ],
  "response_format": {
    "type": "json_schema",
    "json_schema": {
      "name": "person",
      "strict": true,
      "schema": {
        "type": "object",
        "properties": {
          "name": {
            "type": "string",
            "minLength": 1
          },
          "age": {
            "type": "number",
            "minimum": 0,
            "maximum": 130
          }
        },
        "required": [
          "name",
          "age"
        ],
        "additionalProperties": false
      }
    }
  },
  "reasoning_effort": "medium"
}'
```

  

  

    
Responses

    Structured Outputs

```javascript
const response = await openai.responses.create({
  model: "gpt-6-astra",
  input: "Jane, 54 years old",
  text: {
    format: {
      type: "json_schema",
      name: "person",
      strict: true,
      schema: {
        type: "object",
        properties: {
          name: {
            type: "string",
            minLength: 1,
          },
          age: {
            type: "number",
            minimum: 0,
            maximum: 130,
          },
        },
        required: ["name", "age"],
        additionalProperties: false,
      },
    },
  },
});
```

```python
response = client.responses.create(
    model="gpt-6-astra",
    input="Jane, 54 years old",
    text={
        "format": {
            "type": "json_schema",
            "name": "person",
            "strict": True,
            "schema": {
                "type": "object",
                "properties": {
                    "name": {"type": "string", "minLength": 1},
                    "age": {"type": "number", "minimum": 0, "maximum": 130},
                },
                "required": ["name", "age"],
                "additionalProperties": False,
            },
        }
    },
)
```

```go
package main

import (
    "context"
    "fmt"

    "github.com/openai/openai-go/v3"
    "github.com/openai/openai-go/v3/responses"
)

func main() {
    client := openai.NewClient()
    schema := map[string]any{
        "type": "object",
        "properties": map[string]any{
            "name": map[string]any{"type": "string", "minLength": 1},
            "age":  map[string]any{"type": "number", "minimum": 0, "maximum": 130},
        },
        "required":             []string{"name", "age"},
        "additionalProperties": false,
    }

    response, err := client.Responses.New(context.Background(), responses.ResponseNewParams{
        Model: "gpt-6-astra",
        Input: responses.ResponseNewParamsInputUnion{OfString: openai.String("Jane, 54 years old")},
        Text: responses.ResponseTextConfigParam{Format: responses.ResponseFormatTextConfigUnionParam{
            OfJSONSchema: &responses.ResponseFormatTextJSONSchemaConfigParam{Name: "person", Schema: schema, Strict: openai.Bool(true)},
        }},
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(response.OutputText())
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.core.JsonValue;
import com.openai.models.responses.ResponseCreateParams;
import com.openai.models.responses.ResponseFormatTextJsonSchemaConfig;
import com.openai.models.responses.ResponseTextConfig;
import java.util.List;
import java.util.Map;

ResponseCreateParams params =
    ResponseCreateParams.builder()
        .model("gpt-6-astra")
        .input("Jane, 54 years old")
        .text(
            ResponseTextConfig.builder()
                .format(
                    ResponseFormatTextJsonSchemaConfig.builder()
                        .name("person")
                        .strict(true)
                        .schema(
                            ResponseFormatTextJsonSchemaConfig.Schema.builder()
                                .putAdditionalProperty("type", JsonValue.from("object"))
                                .putAdditionalProperty(
                                    "properties",
                                    JsonValue.from(
                                        Map.of(
                                            "name",
                                            Map.of("type", "string", "minLength", 1),
                                            "age",
                                            Map.of(
                                                "type", "number", "minimum", 0, "maximum",
                                                130))))
                                .putAdditionalProperty(
                                    "required", JsonValue.from(List.of("name", "age")))
                                .putAdditionalProperty(
                                    "additionalProperties", JsonValue.from(false))
                                .build())
                        .build())
                .build())
        .build();

client.responses().create(params).output().stream()
    .flatMap(item -> item.message().stream())
    .flatMap(message -> message.content().stream())
    .flatMap(content -> content.outputText().stream())
    .forEach(text -> System.out.println(text.text()));
```

```csharp
using OpenAI.Responses;
#pragma warning disable OPENAI001

string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
ResponsesClient client = new(key);

BinaryData schema = BinaryData.FromString(
    """
    {
      "type": "object",
      "properties": {
        "name": { "type": "string", "minLength": 1 },
        "age": { "type": "number", "minimum": 0, "maximum": 130 }
      },
      "required": ["name", "age"],
      "additionalProperties": false
    }
    """
);
CreateResponseOptions options = new()
{
    Model = "gpt-6-astra",
    TextOptions = new ResponseTextOptions
    {
        TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
            "person",
            schema,
            jsonSchemaIsStrict: true
        ),
    },
};
options.InputItems.Add(
    ResponseItem.CreateUserMessageItem("Jane, 54 years old")
);

ResponseResult response = await client.CreateResponseAsync(options);

Console.WriteLine(response.GetOutputText());
```

```ruby
require "openai"

client = OpenAI::Client.new
schema = {
  type: "object",
  properties: {
    name: {
      type: "string",
      minLength: 1
    },
    age: {
      type: "number",
      minimum: 0,
      maximum: 130
    }
  },
  required: ["name", "age"],
  additionalProperties: false
}

response = client.responses.create(
  model: "gpt-6-astra",
  input: "Jane, 54 years old",
  text: {
    format: {
      type: :json_schema,
      name: "person",
      strict: true,
      schema: schema
    }
  }
)

puts(response.output_text)
```

```bash
curl https://api.openai.com/v1/responses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $OPENAI_API_KEY" \
  -d '{
  "model": "gpt-6-astra",
  "input": "Jane, 54 years old",
  "text": {
    "format": {
      "type": "json_schema",
      "name": "person",
      "strict": true,
      "schema": {
        "type": "object",
        "properties": {
          "name": {
            "type": "string",
            "minLength": 1
          },
          "age": {
            "type": "number",
            "minimum": 0,
            "maximum": 130
          }
        },
        "required": [
          "name",
          "age"
        ],
        "additionalProperties": false
      }
    }
  }
}'
```



### 7. Update streaming consumers

Chat Completions streaming returns incremental chunks with a `delta` field. Responses streaming uses typed server-sent events. Update stream consumers to branch on each event's `type` and handle the events your UI or orchestration layer needs.

For text streaming, listen for events such as:

- `response.created`
- `response.output_text.delta`
- `response.completed`
- `error`

Function-calling streams can also emit events such as `response.function_call_arguments.delta` and `response.function_call_arguments.done`. See the [streaming Responses guide](https://developers.openai.com/api/docs/guides/streaming-responses?api-mode=responses) and [Responses streaming events reference](https://developers.openai.com/api/reference/resources/responses).

### 8. Upgrade to native tools

If your application has use cases that would benefit from OpenAI's native [tools](https://developers.openai.com/api/docs/guides/tools), you can update your tool calls to use OpenAI's tools out of the box.



Chat Completions

    With Chat Completions, you cannot use OpenAI-hosted tools natively and have
    to write your own tool integration.
    This example uses GPT-5.6 because GPT-6 Astra requires the Responses API
    for tool calling.
    Web search tool

```javascript
async function web_search(query) {
  const res = await fetch(`https://api.example.com/search?q=${query}`);
  const data = await res.json();
  return data.results;
}

const completion = await client.chat.completions.create({
  model: "gpt-5.6",
  messages: [
    { role: "system", content: "You are a helpful assistant." },
    { role: "user", content: "Who is the current president of France?" },
  ],
  functions: [
    {
      name: "web_search",
      description: "Search the web for information",
      parameters: {
        type: "object",
        properties: { query: { type: "string" } },
        required: ["query"],
      },
    },
  ],
});
```

```python
import requests


def web_search(query):
    r = requests.get(f"https://api.example.com/search?q={query}")
    return r.json().get("results", [])


completion = client.chat.completions.create(
    model="gpt-5.6",
    messages=[
        {"role": "system", "content": "You are a helpful assistant."},
        {"role": "user", "content": "Who is the current president of France?"},
    ],
    functions=[
        {
            "name": "web_search",
            "description": "Search the web for information",
            "parameters": {
                "type": "object",
                "properties": {"query": {"type": "string"}},
                "required": ["query"],
            },
        }
    ],
)
```

```go
package main

import (
    "context"
    "fmt"

    "github.com/openai/openai-go/v3"
    "github.com/openai/openai-go/v3/shared"
)

func main() {
    client := openai.NewClient()
    completion, err := client.Chat.Completions.New(context.Background(), openai.ChatCompletionNewParams{
        Model: "gpt-5.6",
        Messages: []openai.ChatCompletionMessageParamUnion{
            openai.SystemMessage("You are a helpful assistant."),
            openai.UserMessage("Who is the current president of France?"),
        },
        Functions: []openai.ChatCompletionNewParamsFunction{{
            Name:        "web_search",
            Description: openai.String("Search the web for information"),
            Parameters: map[string]any{
                "type":       "object",
                "properties": map[string]any{"query": map[string]any{"type": "string"}},
                "required":   []string{"query"},
            },
        }},
        ReasoningEffort: shared.ReasoningEffortNone,
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(completion.Choices[0].Message)
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.core.JsonValue;
import com.openai.models.FunctionParameters;
import com.openai.models.ReasoningEffort;
import com.openai.models.chat.completions.ChatCompletionCreateParams;
import java.util.List;
import java.util.Map;

ChatCompletionCreateParams params =
    ChatCompletionCreateParams.builder()
        .model("gpt-5.6")
        .reasoningEffort(ReasoningEffort.NONE)
        .addSystemMessage("You are a helpful assistant.")
        .addUserMessage("Who is the current president of France?")
        .addFunction(
            ChatCompletionCreateParams.Function.builder()
                .name("web_search")
                .description("Search the web for information")
                .parameters(
                    FunctionParameters.builder()
                        .putAdditionalProperty("type", JsonValue.from("object"))
                        .putAdditionalProperty(
                            "properties",
                            JsonValue.from(Map.of("query", Map.of("type", "string"))))
                        .putAdditionalProperty("required", JsonValue.from(List.of("query")))
                        .build())
                .build())
        .build();

client.chat().completions().create(params).choices().stream()
    .map(choice -> choice.message())
    .forEach(System.out::println);
```

```ruby
require "openai"

client = OpenAI::Client.new

completion = client.chat.completions.create(
  model: "gpt-5.6",
  reasoning_effort: :none,
  messages: [
    {
      role: :system,
      content: "You are a helpful assistant."
    },
    {
      role: :user,
      content: "Who is the current president of France?"
    }
  ],
  functions: [
    {
      name: "web_search",
      description: "Search the web for information",
      parameters: {
        type: "object",
        properties: { query: { type: "string" } },
        required: ["query"]
      }
    }
  ]
)

puts(completion.choices.fetch(0).message)
```

```bash
curl https://api.example.com/search \
  -G \
  --data-urlencode "q=your+search+term" \
  --data-urlencode "key=$SEARCH_API_KEY"\
```

  

  

    
Responses

    With Responses, you can specify the tools that you want the model to use.
    Web search tool

```javascript
const answer = await client.responses.create({
  model: "gpt-6-astra",
  input: "Who is the current president of France?",
  tools: [{ type: "web_search" }],
});

console.log(answer.output_text);
```

```python
answer = client.responses.create(
    model="gpt-6-astra",
    input="Who is the current president of France?",
    tools=[{"type": "web_search"}],
)

print(answer.output_text)
```

```go
package main

import (
    "context"
    "fmt"

    "github.com/openai/openai-go/v3"
    "github.com/openai/openai-go/v3/responses"
)

func main() {
    client := openai.NewClient()
    response, err := client.Responses.New(context.Background(), responses.ResponseNewParams{
        Model: "gpt-6-astra",
        Input: responses.ResponseNewParamsInputUnion{OfString: openai.String("Who is the current president of France?")},
        Tools: []responses.ToolUnionParam{
            responses.ToolParamOfWebSearch(responses.WebSearchToolTypeWebSearch),
        },
    })
    if err != nil {
        panic(err)
    }
    fmt.Println(response.OutputText())
}
```

```java
import com.openai.client.OpenAIClient;
import com.openai.client.okhttp.OpenAIOkHttpClient;
import com.openai.models.responses.ResponseCreateParams;
import com.openai.models.responses.WebSearchTool;

ResponseCreateParams params =
    ResponseCreateParams.builder()
        .model("gpt-6-astra")
        .input("Who is the current president of France?")
        .addTool(WebSearchTool.builder().type(WebSearchTool.Type.WEB_SEARCH).build())
        .build();

client.responses().create(params).output().stream()
    .flatMap(item -> item.message().stream())
    .flatMap(message -> message.content().stream())
    .flatMap(content -> content.outputText().stream())
    .forEach(text -> System.out.println(text.text()));
```

```csharp
using OpenAI.Responses;
#pragma warning disable OPENAI001

string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
ResponsesClient client = new(key);

CreateResponseOptions options = new() { Model = "gpt-6-astra" };
options.Tools.Add(ResponseTool.CreateWebSearchTool());
options.InputItems.Add(
    ResponseItem.CreateUserMessageItem("Who is the current president of France?")
);

ResponseResult response = await client.CreateResponseAsync(options);
Console.WriteLine(response.GetOutputText());
```

```ruby
require "openai"

client = OpenAI::Client.new

response = client.responses.create(
  model: "gpt-6-astra",
  input: "Who is the current president of France?",
  tools: [{ type: :web_search }]
)

puts(response.output_text)
```

```bash
curl https://api.openai.com/v1/responses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $OPENAI_API_KEY" \
  -d '{
    "model": "gpt-6-astra",
    "input": "Who is the current president of France?",
    "tools": [{"type": "web_search"}]
  }'
```



### 9. Check common migration errors

Watch for these issues when moving code from Chat Completions to Responses:

- Reading `choices[0].message.content` instead of `response.output_text` or `response.output`.
- Treating every `output` entry as a message. Reasoning, tool, and function calls are separate Item types.
- Dropping reasoning, function call, or function call output Items when manually carrying context into the next response.
- Sending a function result without the matching `call_id`.
- Using `response_format` in a Responses request instead of `text.format`.
- Reusing Chat Completions streaming chunk handlers without handling typed Responses events.
- Assuming `previous_response_id` removes billing for prior context. Previous input tokens in the response chain are still billed as input tokens.

## Incremental rollout checklist

Chat Completions remains supported, so you can migrate one user flow at a time.

- [ ] Start with a simple text-generation flow.
- [ ] Update the endpoint, request body, and output handling.
- [ ] Decide whether the flow uses `previous_response_id`, manual Item replay, or the Conversations API.
- [ ] If the flow is stateless or ZDR, add `store: false` and include encrypted reasoning items when reasoning context must continue across turns.
- [ ] Migrate function definitions and verify function call outputs include the correct `call_id`.
- [ ] Move Structured Outputs schemas from `response_format` to `text.format`.
- [ ] Update streaming consumers to handle typed Responses events.
- [ ] Replace custom orchestration with OpenAI-hosted tools where they fit the workflow.
- [ ] Compare behavior, latency, token usage, and errors before routing more traffic to Responses.

We recommend migrating all flows to the Responses API over time to take advantage of the latest OpenAI features and improvements.

## Assistants API

Based on developer feedback from the [Assistants API](https://developers.openai.com/api/reference/resources/beta/subresources/assistants) beta, we've incorporated key improvements into the Responses API to make it more flexible, faster, and easier to use. The Responses API represents the future direction for building agents on OpenAI.

The Assistants API was officially sunset on August 26, 2026, and is no longer available. Follow the [migration guide](https://developers.openai.com/api/docs/assistants/migration) to update your integration to the Responses API.