# Cellm
Cellm is an Excel extension that lets you use Large Language Models (LLMs) like ChatGPT in cell formulas.

## What is Cellm?
Similar to Excel's `=SUM()` function that outputs the sum of a range of numbers, Cellm's `=PROMPT()` function outputs the AI response to a range of text. 

For example, you can write `=PROMPT(A1:A10, "Extract keywords")` in a cell's formula to extract keywords from the range of cells. You can then drag the cell to apply the prompt to many other rows like `A2:B2` and `A3:B3`. This is useful when you want to use AI for repetitive tasks that would normally require copy-pasting data in and out of a chat window many times.

## Key features
This extension does one thing and one thing well.

- Calls LLMs in formulas and returns short answers suitable for cells.
- Supports models from Anthropic, OpenAI, and Google as well as other providers that mirrors one of these APIs, e.g. local Ollama, llama.cpp or vLLM servers.

## Example
Imagine you want to compare many scientific papers against inclusion and exclusion criteria for a systematic review. Here's how you could use Cellm to help with this task:

https://github.com/user-attachments/assets/94671655-09c5-42fa-9197-d8dffc439c27

In this example, we copy the papers title and abstract into excel and write the prompt once. We then use AutoFill to apply the prompt to many papers. Simple and powerful.

A single paper is misclassified because the inclusion and exclusion criteria were simplified into one sentence. This is a good example, however, because it shows that these models rely entirely on your input and can make mistakes.

## Why?
My girlfriend was writing a systematic review paper. She had to compare 7.500 papers against inclusion and exclusion criterias. I told her this was a perfect use-case for LLMs and it sparked the idea to make an AI tool to automate repetitive tasks for people like her who would rather avoid programming. I think Cellm is really cool because it enables normal people to automate repetitive tasks with AI to a level that was previously available only to programmers. 

She still did her analysis manually, of couse, because she cares about scientific integrity.

## Function
Cellm provides the following function:

```excel
PROMPT(cells: range, [instruction: range | instruction: string | temperature: double], [temperature: double]): string
```

- **cells:** A cell or a range of cells to send to the AI model
- **instructions:** A cell, a range of cells or a string: (Optional) Instructions for the AI model.
  - Default: Empty. The model will follow instructions in cells in the first argument as long as they present _somewhere_ and the model can identify them.
- **temperature:** double: (Optional) A value between 0 and 1 that controls the balance between deterministic outputs and creative exploration.
  - Default: 0. The model will almost always give you the same result.
- **Returns:** string: The AI model's response

Example usage:
- `=Prompt(A1:D10, "Extract keywords")` will use the selected range of cells as context and follow the instruction to extract keywords.
- `=Prompt(A1:D10, "Extract keywords", 0.7)` will use the selected range of cells as context, follow the instruction to extract keywords, and use a temperature of 0.7.
- `=Prompt(A1:D10)` will use the range of cells as context and follow instructions as long as they present _somewhere_ in the cells.
- `=Prompt(A1:D10, 0.7)` will use the selected range of cells as context, follow any instruction within the cells, and use a temperature of 0.7.

## Use-cases
Copy your data into Excel and take advantage of the fact that you can create one prompt and then drag the cell to automatically adjust cell range.

- **Classification**
    ```excel
    =Prompt(B2, "Analyse the survery response. Categorize as 'Product', 'Service', or 'Pricing' related or 'Other'."
    ```

- **Prompt engineering and RAG tuning**
    ```excel
    =Prompt(A1:F1, "Score relevancy of the retrieved documents to the user's question on a scale from 1-10 where 10 is most relevant.")
    ```
    where rows will contain contains different RAG results.

- **Sentiment analysis**
    ```excel
    =Prompt(A1, "Categorize each customer email as 'Positive', 'Negative', or 'Other'")
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
4. Install dependencies:
   ```
   dotnet restore
   ```
5. Build the project:
   ```
   dotnet build
   ```

## Configuration

Edit the `Cellm/appsettings.Local.json` file and add you API key to the model provider of your choice or set the base address of the OpenAI client to the url of your local Llama.cpp, Ollama, or vLLM server. Remember to update the "DefaultModelProvider" parameter if you switch away from Anthropic which is the default model provider.

## License

Fair Core License, Version 1.0, Apache 2.0 Future License
