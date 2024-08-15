using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.Exceptions;
using Cellm.Prompts;
using ExcelDna.Integration;

namespace Cellm;

public class Cellm
{
    private static readonly string ApiKey = new AddIn.AddIn().ApiKey;
    private static readonly string ApiUrl = new AddIn.AddIn().ApiUrl;

    private static readonly string SystemMessage = @"
<input>
The user has called you via the ""Prompt"" Excel function in a cell formula. The argument to the formula is the range of cells the user selected, e.g. ""=Prompt(A1)"" or ""=Prompt(A1:D10)"" 
Multiple cells are rendered as a table where each cell is prepended with the its coordinates.
<input>

<constraints>
You can only solve tasks that return data suitable for a single cell in a spreadsheet and in a format that is plain text or a numeric value.
If you cannot find any instructions, or you cannot follow user's instructions in a cell-appropriate format, reply with ""#INSTRUCTION_ERROR?"" and nothing else.
</constraints>

<output>
Return ONLY the result of following the user's instructions.
The result must be one of the following:

- A single word or number
- A comma-separated list of words or numbers
- A brief sentence

Be concise. Remember that cells have limited visible space.
Do not provide explanations, steps, or engage in conversation.
Ensure the output is directly usable in a spreadsheet cell.
</output>
";

    [ExcelFunction(Name = "PROMPT", Description = "Call a model with a prompt")]
    public static string Call(
        [ExcelArgument(AllowReference = true, Name = "Cells", Description = "A cell or range of cells")] object cells,
        [ExcelArgument(Name = "InstructionsOrTemperature", Description = "Model instructions or temperature")] object instructionsOrTemperature,
        [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
        try
        {
            var arguments = new ArgumentParser()
                .AddCells(cells)
                .AddInstructionsOrTemperature(instructionsOrTemperature)
                .AddTemperature(temperature)
                .Parse();

            var userMessage = new StringBuilder()
                .AppendLine(arguments.Cells)
                .AppendLine(arguments.Instructions)
                .ToString();

            var prompt = new PromptBuilder()
                .SetSystemMessage(SystemMessage)
                .SetTemperature(arguments.Temperature)
                .AddUserMessage(userMessage)
                .Build();

            return CallModelSync(prompt);

        }
        catch (CellmException ex)
        {
            return ex.ToString();
        }
    }

    private static string CallModelSync(Prompt prompt)
    {
        using (var httpClient = new HttpClient())
        {
            try
            {
                httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }
            catch (InvalidOperationException ex)
            {
                throw new CellmException("Failed to add request headers", ex);
            }
            catch (ArgumentException ex)
            {
                throw new CellmException("Invalid argument when adding request headers", ex);
            }
            catch (FormatException ex)
            {
                throw new CellmException("Invalid format for request header value", ex);
            }

            var requestBody = new RequestBody
            {
                System = prompt.SystemMessage,
                Messages = prompt.messages.Select(x => new Message { Content = x.Content, Role = x.Role.ToString().ToLower() }).ToList(),
                Model = "claude-3-5-sonnet-20240620",
                MaxTokens = 256,
                Temperature = prompt.Temperature
            };

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(requestBody, options);
                var jsonAsString = new StringContent(json, Encoding.UTF8, "application/json");

                var response = httpClient.PostAsync(ApiUrl, jsonAsString).Result;
                var responseBodyAsString = response.Content.ReadAsStringAsync().Result;

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(responseBodyAsString, null, response.StatusCode);
                }

                var responseBody = JsonSerializer.Deserialize<ResponseBody>(responseBodyAsString, options);
                var assistantMessage = responseBody?.Content?.Last()?.Text ?? "No content received from API";

                if (assistantMessage.StartsWith("#INSTRUCTION_ERROR?"))
                {
                    throw new CellmException(assistantMessage);
                }

                return responseBody?.Content?.Last()?.Text ?? "No content received from API";
            }
            catch (HttpRequestException ex)
            {
                throw new CellmException("API request failed", ex);
            }
            catch (JsonException ex)
            {
                throw new CellmException("Failed to deserialize API response", ex);
            }
            catch (NotSupportedException ex)
            {
                throw new CellmException("Serialization or deserialization of request body is not supported", ex);
            }
            catch (NullReferenceException ex)
            {
                throw new CellmException("Null reference encountered while processing the response", ex);
            }
            catch (Exception ex)
            {
                throw new CellmException("An unexpected error occurred", ex);
            }
        }
    }

    public class ResponseBody
    {
        public List<Content> Content { get; set; }

        public string Id { get; set; }

        public string Model { get; set; }

        public string Role { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public string? StopSequence { get; set; }

        public string Type { get; set; }

        public Usage Usage { get; set; }
    }

    public class RequestBody
    {
        public List<Message> Messages { get; set; }

        public string System { get; set; }

        public string Model { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        public double Temperature { get; set; }
    }

    public class Message
    {
        public string Role { get; set; }

        public string Content { get; set; }
    }

    public class Content
    {
        public string Text { get; set; }

        public string Type { get; set; }
    }

    public class Usage
    {
        public int InputTokens { get; set; }

        public int OutputTokens { get; set; }
    }
}