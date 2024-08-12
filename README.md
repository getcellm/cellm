# Cellm
Cellm is an Excel extension that lets you use Large Language Models (LLMs) like ChatGPT in cell formulas.

## What is Cellm?
Cellm adds a new function `=PROMPT()` that can send a range of cells to an LLM and output the result in a cell.

[GIF WITH EXAMPLE]

Similar to the `=SUM()` function that calculates the sum of a cell range of numbers, Cellm calculates a response to a cell range of text. This is useful for text analysis and for repetitive tasks that would normally require copying and pasting in and out of a chat window.

## Key features
This extension does one thing and one thing well.

- Calls LLMs in formulas.
- Accepts strings, a single cell, or a range of cells.
- Returns short answers suitable for cells.

## Function

Cellm provides the following function:

```excel
PROMPT(cells: range, [instruction: string | temperature: double], [temperature: double]): string
```

- cells: A cell or a range of cells to send to the AI model
- instructions: string: (Optional) Instructions for the AI model. 
  - Default: Empty. The model will follow instructions as long as they present _somewhere_ in the cells.
- temperature: double: (Optional) A value between 0 and 1 that controls the balance between deterministic outputs and creative exploration.
  - Default: 0. The model will almost always give you the same result.
- Returns: string: The AI-generated response

Example usage:
- `=Prompt(A1:D10, "Extract keywords")` uses the selected range of cells as context and will follow the instruction to extract keywords.
- `=Prompt(A1:D10, "Extract keywords", 0.7)` uses the selected range of cells as context, follows the instruction to extract keywords, and uses a temperature of 0.7.
- `=Prompt(A1:D10)` uses the range of cells as context and will follow instructions as long as they present _somewhere_ in the cells.
- `=Prompt(A1:D10, 0.7)` uses the selected range of cells as context, follows any instruction within the cells, and sets the temperature to 0. 

## Why?
My girlfriend was writing a systematic review paper. She had to compare 7.500 papers against inclusion and exclusion criterias. She did this manually because obviously she cares about scientific integrity, but it inspired me to make an AI tool to automate repetitive tasks for people who would rather avoid programming. The name is a combination of "Cell" and "LLM" and is pronounced _Sell 'em!_.

## Use-cases
- **Qualitative interview analysis**
    ```excel
    =Prompt(B2:B100, "Analyze the sentiment of each interview answer. Categorize as POSITIVE, NEGATIVE, NEUTRAL, or MIXED."
    ```

- **Prompt engineering and RAG tuning**
    ```excel
    =Prompt(A1:F1), "Summarize this product review in one sentence.")
    =Prompt(B1:F1), "Summarize this product review in one sentence.")
    =Prompt(C1:F1), "Summarize this product review in one sentence.")
    ```
    where the cell range contains different RAG results.

- **Catogorize text**
    ```excel
    =Prompt(A2:A100, "Categorize each customer comment as 'Product', 'Service', or 'Pricing' related.")
    ```

## Dos and dont's 
- Don't use Cellm to compute sums, averages, and other calculations. LLMs are not good at that. Use existing tools instead.

## Getting Started

To use Cellm:

1. Download the Cellm Excel add-in (`.xll` file) from the Releases page.
2. In Excel, go to `File > Options > Add-Ins`.
3. Manage `Excel Add-ins` and click `Go...`.
4. Click `Browse...` and select the downloaded `.xll` file.
5. Check the box next to Cellm and click `OK`.

## Development

1. Install the .NET SDK.
2. Clone this repository:
   ```
   git clone https://github.com/yourusername/cellm.git
   ```
3. Go to the project directory:
   ```
   cd cellm
   ```
4. Install ExcelDna.Integration:
   ```
   dotnet add package ExcelDna.Integration
   ```
5. Build the project:
   ```
   dotnet build
   ```

## Configuration

Edit the `Secrets.cs` file in the project root with your API key:

```csharp
namespace Cellm;

internal class Secrets
{
    public string ApiKey { get; } = "your-api-key-here";
}

```

## License

Apache 2.0 License
