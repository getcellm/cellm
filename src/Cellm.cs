using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.Exceptions;
using Cellm.RenderMarkdownTable;
using ExcelDna.Integration;

namespace Cellm;

public class Cellm
{
    private static readonly string ApiKey = new Secrets().ApiKey;
    private static readonly string ApiUrl = "https://api.anthropic.com/v1/messages";
    private static readonly string SystemMessage = @"
The user has called you via the ""Prompt"" Excel function in a cell formula. The argument to the formula is the range of cells the user selected, e.g. ""=Prompt(A1:D10)"" 
The selected range of cells are rendered as a markdown table. The first column and the header row will contain cell coordinates. 
You will find instructions before the table or somewhere in the table. Follow these instructions.
If you cannot find any instructions, or you cannot complete the user's request, reply with ""#INSTRUCTION_ERROR?"" and nothing else.
You are limited to returning data to a cell in a spreadsheet. You cannot solve a task whose output is not fit for cell.
Be concise. Cells are small. 
Return the result of following the user's instructions and ONLY the result. 
The result must be one word or number, a comma-seperated list of or numbers, or one brief sentence.
Do not explain how to do something. Do not chat with the user.
";

    [ExcelFunction(Description = "Call a model with a prompt")]
    public static string Prompt(
        [ExcelArgument(Name = "Cells", Description = "String or range of cells to render")] object input,
        [ExcelArgument(Name = "Instructions", Description = "Model instructions")] string instructions = "")
    {
        try
        {
            string cells;

            if (input is string userMessage)
            {
                cells = userMessage;
            }
            else if (input is object[,] range)
            {
                cells = MarkdownTable.Render(range);
            }
            else
            {
                return "Error: Invalid input type. Please provide a string or a range of cells.";
            }

            return CallModelSync(cells, "");
        }
        catch (CellmException ex)
        {
            return ex.ToString();
        }
    }


    private static string CallModelSync(string cells, string instructions)
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

            var content = new StringBuilder();
            content.AppendLine(instructions);
            content.AppendLine(cells);

            var requestBody = new RequestBody
            {
                System = SystemMessage,
                Messages = new List<Message>
                {
                    new Message
                    {
                        Role = "user",
                        Content = content.ToString()
                    }
                },
                Model = "claude-3-5-sonnet-20240620",
                MaxTokens = 128
            };

            try
            {
                var json = JsonSerializer.Serialize(requestBody);
                var jsonAsString = new StringContent(json, Encoding.UTF8, "application/json");

                var response = httpClient.PostAsync(ApiUrl, jsonAsString).Result;
                var responseBodyAsString = response.Content.ReadAsStringAsync().Result;

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(responseBodyAsString, null, response.StatusCode);
                }

                var responseBody = JsonSerializer.Deserialize<ResponseBody>(responseBodyAsString);
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
        [JsonPropertyName("content")]
        public List<Content> Content { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public string? StopSequence { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }
    }

    public class RequestBody
    {
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }
}