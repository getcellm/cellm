using System.Text.Json;
using Cellm.AddIn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cellm.Models.Prompts;

public static class StructuredOutput
{
    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        RespectRequiredConstructorParameters = true,
        PropertyNameCaseInsensitive = true
    };

    internal static bool TryParse(string message, StructuredOutputShape shape, out string[,]? structuredOutput)
    {
        structuredOutput = null;

        // Quick check to avoid trying to parse simple strings that aren't JSON objects.
        if (string.IsNullOrWhiteSpace(message) || !message.Trim().StartsWith('{'))
        {
            return false;
        }

        try
        {
            // Maybe deserialize
            structuredOutput = shape switch
            {
                // No-op
                StructuredOutputShape.None => null,
                // ["a", "b", "c"] becomes [ ["a", "b", "c"] ]
                StructuredOutputShape.Row => ConvertRowToArray2d(JsonSerializer.Deserialize<Array1d>(message, _jsonOptions)?.Data ?? throw new JsonException($"Failed to deserialize row: {message}")),
                // ["a", "b", "c"] becomes [ ["a"], ["b"], ["c"] ]
                StructuredOutputShape.Column => ConvertColumnToArray2d(JsonSerializer.Deserialize<Array1d>(message, _jsonOptions)?.Data ?? throw new JsonException($"Failed to deserialize column: {message}")),
                // [ ["a", "b"], ["c"] ] becomes [ ["a", "b"], ["c", ""] ]
                StructuredOutputShape.Range => ConvertJaggedToArray2d(JsonSerializer.Deserialize<Array2d>(message, _jsonOptions)?.Data ?? throw new JsonException($"Failed to deserialize 2d array: {message}")),
                _ => null
            };

            return structuredOutput is not null;
        }
        catch (JsonException ex)
        {
            // Do nothing, response will be returned in a single cell
            var loggerFactory = CellmAddIn.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(StructuredOutput));

            loggerFactory.LogWarning("{message}", ex.Message);

            return false;
        }
    }

    private record Array2d(string[][] Data);

    private record Array1d(string[] Data);

    private static string[,] ConvertRowToArray2d(string[] data)
    {
        var result = new string[1, data.Length];

        for (var i = 0; i < data.Length; i++)
        {
            result[0, i] = data[i];
        }

        return result;
    }

    private static string[,] ConvertColumnToArray2d(string[] data)
    {
        var result = new string[data.Length, 1];

        for (var i = 0; i < data.Length; i++)
        {
            result[i, 0] = data[i];
        }

        return result;
    }

    private static string[,] ConvertJaggedToArray2d(string[][] data)
    {

        var numberOfRows = data.Length;
        var numberOfColumns = data.Max(row => row?.Length ?? 0);
        var array2d = new string[numberOfRows, numberOfColumns];

        for (var i = 0; i < numberOfRows; i++)
        {
            for (var j = 0; j < data[i].Length; j++)
            {
                array2d[i, j] = data[i][j];
            }
        }

        return array2d;
    }
}
