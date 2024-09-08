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
Imagine you want to compare many scientific papers against inclusion and exclusion criteria for a systematic review. Here's how you could use Cellm for this task:

https://github.com/user-attachments/assets/94671655-09c5-42fa-9197-d8dffc439c27

In this example, we copy the papers title and abstract into Excel and write the prompt once. We then use AutoFill to apply the prompt to many papers. Simple and powerful.

A single paper is misclassified because the original inclusion and exclusion criteria were summarized in one sentence. This is a good example, however, because it shows that these models rely entirely on your input and can make mistakes.

## Why?
My girlfriend was writing a systematic review paper. She had to compare 7.500 papers against inclusion and exclusion criterias. I told her this was a great use case for LLMs and it sparked the idea to make an AI tool to automate repetitive tasks for people like her who would rather avoid programming. I think Cellm is really cool because it enables normal people to automate repetitive tasks with AI to a level that was previously available only to programmers. 

She still did her analysis manually, of couse, because she cares about scientific integrity.


## Usage
Cellm provides the following function:

```excel
PROMPT(cells: range, [instruction: range | instruction: string | temperature: double], [temperature: double]): string
```

- **cells (Required):** A cell or a range of cells.
  - Context and (optionally) instructions. The model will follow instructions as long as they present _somewhere_ in the cells and the model can identify them.
- **instructions (Optional):** A cell, a range of cells, or a string.
  - Overrides any instructions in the cells of the first argument.
  - Default: Empty. 
- **temperature (Optional):** double. 
    - A value between 0 and 1 that controls the balance between deterministic outputs and creative exploration.
  - Default: 0. The model will almost always give you the same result.
- **Returns:** string: The AI model's response.

Example usage:
- `=Prompt(A1:D10, "Extract keywords")` will use the selected range of cells as context and follow the instruction to extract keywords.
- `=Prompt(A1:D10, "Extract keywords", 0.7)` will use the selected range of cells as context, follow the instruction to extract keywords, and use a temperature of 0.7.
- `=Prompt(A1:D10)` will use the range of cells as context and follow instructions as long as they present _somewhere_ in the cells.
- `=Prompt(A1:D10, 0.7)` will use the selected range of cells as context, follow any instruction within the cells, and use a temperature of 0.7.


## Use Cases
Cellm is useful for repetitive tasks on structured data. Here are some practical applications:

1. **Text Classification**
   ```excel
   =PROMPT(B2, "Analyze the survey response. Categorize as 'Product', 'Service', 'Pricing', or 'Other'.")
   ```
   Use classificatin prompts to quickly categorize large volumes of open-ended survey responses or customer feedback.

2. **Sentiment Analysis**
   ```excel
   =PROMPT(A1, "Categorize the customer email sentiment as 'Positive', 'Negative', or 'Neutral'.")
   ```
   Useful for analyzing customer feedback, social media comments, or product reviews at scale.

3. **Keyword Extraction**
   ```excel
   =PROMPT(C2, "Extract the top 3 keywords from the product description.")
   ```
   Helpful for SEO optimization, content tagging, or quickly summarizing lengthy texts.

4. **Language Translation**
   ```excel
   =PROMPT(D2, "Translate the text in the context from English to Spanish:")
   ```
   Enables quick translation of product names, short descriptions, or customer communications.

5. **Data Cleaning**
   ```excel
   =PROMPT(E2, "Standardize the company name by removing any legal entity identifiers (e.g., Inc., LLC) and correcting common misspellings:")
   ```
   Useful for cleaning and standardizing messy datasets, especially with company names or addresses.

6. **Content Summarization**
   ```excel
   =PROMPT(F2, "Provide a 2-sentence summary of the article in the context.")
   ```
   Great for quickly digesting large amounts of text data, such as news articles or research papers.

7. **Prompt Engineering and RAG Tuning**
   ```excel
   =PROMPT(A1:F1, "Score the relevancy of the retrieved documents to the user's question on a scale from 1-10, where 10 is most relevant.")
   ```
   Helpful for fine-tuning prompts and evaluating Retrieval-Augmented Generation (RAG) systems.

8. **Entity Extraction**
   ```excel
   =PROMPT(G2, "Extract all person names mentioned in the text.")
   ```
   Useful for analyzing unstructured text data in fields like journalism, research, or customer relationship management.

These use cases are starting points. Experiment with different instructions to find what works best for your data. It works best when combined with human judgment and expertise in your specific domain.

## Getting Started

Cellm must be built from source and installed via Excel. Follow the steps below.

### Build

1. Clone this repository:
   ```cmd
   git clone https://github.com/kaspermarstal/cellm.git
   ```
2. Rename `Cellm/appsettings.Local.Example.json` to `Cellm/appsettings.Local.json` and configure your model provider:

    - For Anthropic, Google, or OpenAI APIs, add your API key to `Cellm/appsettings.Local.json`. Example:
      ```json
      {
        "AnthropicConfiguration": {
            "ApiKey": "YOUR_ANTHROPIC_APIKEY"
        },
        "CellmConfiguration": {
            "DefaultModelProvider":  "AnthropicClient"
        }
      }
      ```
      For Google or OpenAI, replace `AnthropicConfiguration` and `AnthropicClient` with `GoogleConfiguration` and `GoogleClient` or `OpenAiConfiguration` and `OpenAiClient`.
    - For local inference with Llama.cpp, Ollama, vLLM, or other OpenAI compatible inference servers, use the OpenAI provider and set the base address to localhost:
      ```json
      {
        "OpenAiConfiguration": {
            "BaseAddress": "https://localhost:8000"
        },
        "CellmConfiguration": {
            "DefaultModelProvider":  "OpenAiClient"
        }
      }
      ``` 
      Llama.cpp and vLLM use port `8000` by default while Ollama uses `11434`. 
3. In your terminal, go into the root of the project directory:
   ```cmd
   cd cellm
   ```
4. Install dependencies:
   ```cmd
   dotnet restore
   ```
5. Build the project:
   ```cmd
   dotnet build --configuration Release
   ```

### Install

1. In Excel, go to File > Options > Add-Ins.
2. In the `Manage` drop-down menu, select `Excel Add-ins` and click `Go...`.
3. Click `Browse...` and select the `Cellm-AddIn64.xll` file in the bin/Release/net6.0-windows folder.
4. Check the box next to Cellm and click `OK`.

### Requirements

- Windows.
- .NET 6.0 SDK.
- Excel desktop app.

## Dos and Don'ts

Do:

- Use Cellm for tasks that require natural language processing, such as text classification, synthetic data generation, or sentiment analysis.
- Be aware that you WILL spend API credits at incredible pace.
- Combine Cellm with Excel's built-in functions when appropriate.
- Experiment with different prompts to find the most effective instructions for your data.
- Use cell references to dynamically change your prompts based on other data in your spreadsheet.
- In your instructions (second argument), refer to the context (first argument) as "context"
- Use Cellm for batch processing of text data, leveraging Excel's ability to apply formulas across multiple rows.
- Always verify the AI's outputs, especially for critical decisions or analyses. AI can make mistakes or have biases.
- Always consider the privacy implications of the data you're processing with Cellm when using cloud-based LLM providers.

Don't:
 
- Don't use Cellm to compute sums, averages, and other numerical calculations. The current generation of LLMs are not designed for mathematical operations. Use Excel's existing functions instead.
- Don't use Cellm to process sensitive or confidential information unless you've carefully reviewed the privacy policies of the LLM provider you're using.
- Don't use extremely long prompts or give Cellm complex tasks. A normal chat UI lets you have a back and forth conversation which is better for exploring complex topics.
- Don't use Cellm for tasks that require up-to-date information beyond the AI model's knowledge cutoff date _unless_ you provide the information as context.

## License

Fair Core License, Version 1.0, Apache 2.0 Future License
