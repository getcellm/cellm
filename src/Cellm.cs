using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExcelDna.Integration;

namespace Cellm;

public class Cellm
{
    private static readonly string ApiKey = new Secrets().ApiKey;
    private static readonly string ApiUrl = "https://api.anthropic.com/v1/messages";

    [ExcelFunction(Name = "PROMPT", Description = "Call a model with a prompt")]
    public static string Prompt(
        [ExcelArgument(AllowReference = true, Name = "Cells", Description = "A cell or range of cells")] object arg1,
        [ExcelArgument(Name = "Instructions", Description = "Model instructions or temperature")] object arg2,
        [ExcelArgument(Name = "Temperature", Description = "Temperature")] object arg3)
    {
        try
        {
            return CallModelSync(new Arguments(arg1, arg2, arg3));
        }
        catch (CellmException ex)
        {
            return ex.ToString();
        }
    }

    private static string CallModelSync(Arguments arguments)
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
                System = arguments.Instructions,
                Messages = new List<Message>
                {
                    new Message
                    {
                        Role = "user",
                        Content = arguments.Cells
                    }
                },
                Model = "claude-3-5-sonnet-20240620",
                MaxTokens = 256,
                Temperature = arguments.Temperature
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

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
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