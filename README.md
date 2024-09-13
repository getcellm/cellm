# Cellm
Cellm is an Excel extension that lets you use Large Language Models (LLMs) like ChatGPT in cell formulas.

## What is Cellm?
Similar to Excel's `=SUM()` function that outputs the sum of a range of numbers, Cellm's `=PROMPT()` function outputs the AI response to a range of text. 

For example, you can write `=PROMPT(A1:A10, "Extract all person names mentioned in the text.")` in a cell's formula and drag the cell to apply the prompt to many rows. Cellm is useful when you want to use AI for repetitive tasks that would normally require copy-pasting data in and out of a chat window many times.

## Key features
This extension does one thing and one thing well.

- Calls LLMs in formulas and returns short answers suitable for cells.
- Supports models from Anthropic, OpenAI, and Google as well as other providers that mirrors one of these APIs, e.g. local Ollama, llama.cpp or vLLM servers.

## Example
Say you're reviewing medical studies and need to quickly identify papers relevant to your research. Here's how Cellm can help with this task:

https://github.com/user-attachments/assets/94671655-09c5-42fa-9197-d8dffc439c27

In this example, we copy the papers' title and abstract into Excel and write this prompt: 

> "If the paper studies diabetic neuropathy and stroke, return "Include", otherwise, return "Exclude"."  

We then use AutoFill to apply the prompt to many papers. Simple and powerful.

A single paper is misclassified because the original inclusion and exclusion criteria were summarized in one sentence. This is a good example, however, because it shows that these models rely entirely on your input and can make mistakes.

## Why?
My girlfriend was writing a systematic review paper. She had to compare 7.500 papers against inclusion and exclusion criterias. I told her this was a great use case for LLMs but I quickly realized that individually copying 7.500 papers in and out of chat windows was a total pain. This sparked the idea to make an AI tool to automate repetitive tasks for people like her who would rather avoid programming. I think Cellm is really cool because it enables everyone to automate repetitive tasks with AI to a level that was previously available only to programmers. 

She still did her analysis manually, of couse, because she cares about scientific integrity.


## Usage
Cellm provides the following functions:

### PROMPT

```excel
PROMPT(cells: range, [instruction: range | instruction: string | temperature: double], [temperature: double]): string
```

- **cells (Required):** A cell or a range of cells.
  - Context and (optionally) instructions. The model will use the cells as context and follow any instructions as long as they present _somewhere_ in the cells.
- **instructions (Optional):** A cell, a range of cells, or a string.
  - The model will follow these instructions and ignore instructions in the cells of the first argument.
  - Default: Empty. 
- **temperature (Optional):** double. 
    - A value between 0 and 1 that controls the balance between deterministic outputs and creative exploration. Lower values make the output more deterministic, higher values make it more random.
  - Default: 0. The model will almost always give you the same result.
- **Returns:** string: The AI model's response.

Example usage:

- `=PROMPT(A1:D10, "Extract keywords")` will use the selected range of cells as context and follow the instruction to extract keywords.
- `=PROMPT(A1:D10, "Extract keywords", 0.7)` will use the selected range of cells as context, follow the instruction to extract keywords, and use a temperature of 0.7.
- `=PROMPT(A1:D10)` will use the range of cells as context and follow instructions as long as they present _somewhere_ in the cells.
- `=PROMPT(A1:D10, 0.7)` will use the selected range of cells as context, follow any instruction within the cells, and use a temperature of 0.7.

### PROMPTWITH

```excel
PROMPTWITH(providerAndModel: string or cell, cells: range, [instruction: range | instruction: string | temperature: double], [temperature: double]): string
```

Allows you to specify the model as the first argument.

- **providerAndModel (Required)**: A string on the form "provider/model".
  - Default: anthropic/claude-3-5-sonnet-20240620

Example usage:

- `=PROMPTWITH("openai/gpt-4o-mini", A1:D10, "Extract keywords")` will extract keywords using OpenAI's GPT-4o mini model instead of the default model from app settings.

## Use Cases
Cellm is useful for repetitive tasks on structured data. Here are some practical applications:

1. **Text Classification**
   ```excel
   =PROMPT(B2, "Analyze the survey response. Categorize as 'Product', 'Service', 'Pricing', or 'Other'.")
   ```
   Use classification prompts to quickly categorize large volumes of e.g. open-ended survey responses.

2. **Sentiment Analysis**
   ```excel
   =PROMPT(A1, "Score the customer email sentiment on a scale from 1 to 5 where 5 is very positive.")
   ```
   Useful for analyzing customer feedback, social media comments, or product reviews at scale.

3. **Test LLM apps**

   Implement `Cellm/Models/IClient.cs` for your own app and quickly evaluate your own LLM app on large datasets. Manually score responses or use an LLM to evaluate performance. For example, imagine you have a test set of user queries in column A. You can use column B to send queries to your app and column C to get an automated score.
   ```excel
   =PROMPTWITH("CLIENTNAME/MODELNAME", A1) [Column B]
   =PROMPT("Score the relevance of the answer in column B to the query in column A on a scale from 1 to 5 where 5 is most relevant.") [Column C]
   ```

4. **Model Comparison**
   
   Make a sheet with user queries in column A and different models in row 1. Write this prompt in the cell B2:
   
   ```
   =PROMPTWITH(B$1,$A2,"Answer the question in column A")
   ```

   Drag the cell across the entire table to apply all models to all queries.

5. **Language Translation**
   ```excel
   =PROMPT(D2, "Translate the text in the context from English to Spanish.")
   ```
   Enables quick translation of product names, short descriptions, or customer communications.

6. **Data Cleaning**
   ```excel
   =PROMPT(E2, "Standardize the company name by removing any legal entity identifiers (e.g., Inc., LLC) and correcting common misspellings.")
   ```
   Useful for cleaning and standardizing messy datasets, especially with company names or addresses.

7. **Content Summarization**
   ```excel
   =PROMPT(F2, "Provide a 2-sentence summary of the article in the context.")
   ```
   Great for quickly digesting large amounts of text data, such as news articles or research papers.

8. **RAG Evaluation**
   ```excel
   =PROMPT(A1:F1, "Score the relevancy of the retrieved documents to the user's question on a scale from 1 to 5 where 5 is most relevant.")
   ```
   Helpful for fine-tuning prompts and evaluating Retrieval-Augmented Generation (RAG) systems.

9. **Entity Extraction**
   ```excel
   =PROMPT(G2, "Extract all person names mentioned in the text.")
   ```
   Useful for analyzing unstructured text data in fields like journalism, research, or customer relationship management.

10. **Keyword Extraction**
   ```excel
   =PROMPT(C2, "Extract the top 3 keywords from the product description.")
   ```
   Helpful for SEO optimization, content tagging, or quickly summarizing lengthy texts.

11. **Fix mistakes**
   ```
   =PROMPT(A1, "Fix email formatting")
   ```
   Useful when an "auditor" inserts random spaces in a column with thousands of email adresses. Use a local model if you are worried about sending sensitive data to hosted models.

These use cases are starting points. Experiment with different instructions to find what works best for your data. It works best when combined with human judgment and expertise in your specific domain.

## Getting Started

Cellm must be built from source and installed via Excel. Follow the steps below.

### Requirements

#### Cellm

- Windows
- [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
- Excel 2010 or higher (desktop app).

#### Local LLMs

- Docker

We recommend you use Ollama with the Gemma 2 2b model by default. This runs fine on a CPU. If you want to use larger models you will also need:

- A GPU
- NVIDIA container runtime

### Build

1. Clone this repository:
   ```cmd
   git clone https://github.com/kaspermarstal/cellm.git
   ```

2. In your terminal, go into the root of the project directory:
   ```cmd
   cd cellm
   ```

3. Cellm uses Anthropic as the default model provider. You only need to add your API key. Rename `Cellm/appsettings.Anthropic.json` to `Cellm/appsettings.Local.json` and insert your API key. Example:
   ```json
   {
     "AnthropicConfiguration": {
         "ApiKey": "YOUR_OPENAI_APIKEY"
     }
   ```

   You can also use OpenAI or Google as model provider. See the `appsettings.Local.*.json*` files for examples.

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

### Run Local LLMs

Cellm can use models running on your computer with the OpenAI provider and Ollama or vLLM inference servers. This ensures none of your data ever leaves your machine. And its free.

To get started, use Ollama with the Gemma 2 2B model with 4-bit quantization. This runs fine without a GPU.

1. Rename `appsettings.Ollama.json` to `appsettings.Local.json`, 
2. Build and install Cellm.
3. Run the following command in the root of the repository   
   ```cmd
   docker compose -f docker-compose.Ollama.yml up --detach
   docker compose -f docker-compose.Ollama.yml exec backend ollama pull gemma2:2b
   docker compose -f docker-compose.Ollama.yml stop  // When you want to shut it down
   ```

This runs fine on a CPU. Open WebUI in included in the docker compose file so you test the local model outside of Cellm. It is available at `http://localhost:3000`.

If you want to further speed up inference, you can use your GPU and/or use vLLM. A GPU is practically required if you want to use larger models than Gemma 2 2b.

## Dos and Don'ts

Do:

- Experiment with different prompts to find the most effective instructions for your data.
- Use cell references to dynamically change your prompts based on other data in your spreadsheet.
- Use local models for sensitive data. Always consider the privacy implications of the data you're sending cloud-based LLM providers.
- Refer to the cell data as "context" in your instructions.
- Verify responses, especially for critical decisions or analyses. These models will make errors and rely entirely on your input, which may also contain errors.
- Be aware that you WILL spend API credits at incredible pace.

Don't:

- Don't use Cellm to compute sums, averages, and other numerical calculations. The current generation of LLMs are not designed for mathematical operations. Use Excel's existing functions instead.
- Don't use cloud model providers to process sensitive or confidential information unless you've carefully reviewed your data and privacy policies of the LLM provider.
- Don't use extremely long prompts or give Cellm complex tasks. A normal chat UI lets you have a back and forth conversation which is better for exploring complex topics.
- Don't use Cellm for tasks that require up-to-date information beyond the AI model's knowledge cutoff date _unless_ you provide the information as context.

## License

Fair Core License, Version 1.0, Apache 2.0 Future License
