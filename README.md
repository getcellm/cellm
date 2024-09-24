# Cellm
Cellm is an Excel extension that lets you use Large Language Models (LLMs) like ChatGPT in cell formulas.

- [Example](#example)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Use Cases](#use-cases)
- [Run Models Locally](#run-models-locally)
- [Why did you make Cellm?](#why-did-you-make-cellm)

## What is Cellm?
Similar to Excel's `=SUM()` function that outputs the sum of a range of numbers, Cellm's `=PROMPT()` function outputs the AI response to a range of text. 

For example, you can write `=PROMPT(A1:A10, "Extract all person names mentioned in the text.")` in a cell's formula and drag the cell to apply the prompt to many rows. Cellm is useful when you want to use AI for repetitive tasks that would normally require copy-pasting data in and out of a chat window many times.

## Key features
This extension does one thing and one thing well.

- Calls LLMs in formulas and returns short answers suitable for cells.
- Supports models from Anthropic, Mistral, OpenAI, and Google as well as locally hosted models via Llamafiles, Ollama, or vLLM.

## Example
Say you're reviewing medical studies and need to quickly identify papers relevant to your research. Here's how Cellm can help with this task:

Here's how Cellm can help with this task:

https://github.com/user-attachments/assets/c93f7da9-aabd-4c13-a4f5-3e12332c5794

In this example, we copy the papers' title and abstract into Excel and write this prompt: 

> "If the paper studies diabetic neuropathy and stroke, return "Include", otherwise, return "Exclude"."  

We then use AutoFill to apply the prompt to many papers. Simple and powerful.

A single paper is misclassified because the original inclusion and exclusion criteria were summarized in one sentence. This is a good example, however, because it shows that these models rely entirely on your input and can make mistakes.

## Getting Started

Cellm must be built from source and installed via Excel. Follow the steps below.

### Requirements

#### Cellm

- Windows
- [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [Excel 2010 or higher (desktop app)](https://www.microsoft.com/en-us/microsoft-365/excel)

#### Local LLMs

- [Docker](https://www.docker.com/products/docker-desktop/) (optional)
- A GPU and [NVIDIA CUDA Toolkit 12.4](https://developer.nvidia.com/cuda-downloads) or higher (optional)

To get started, you can run small models with Llamafile on your CPU. Cellm can automatically download and run these models for you. For Ollama and vLLM you will need docker, and for higher quality models you will need a GPU.

### Build

1. Clone this repository:
   ```cmd
   git clone https://github.com/getcellm/cellm.git
   ```

2. In your terminal, go into the root of the project directory:
   ```cmd
   cd cellm
   ```

3. Add your Anthropic API key. Rename `src/Cellm/appsettings.Anthropic.json` to `src/Cellm/appsettings.Local.json` and insert it. Example:
    ```json
    {
      "AnthropicConfiguration": {
        "ApiKey": "YOUR_ANTHROPIC_APIKEY"
      }
    }
    ```

   Cellm uses Anthropic as the default model provider. You can also use models from OpenAI, Mistral, Google, or run models locally. See the `appsettings.Local.*.json` files for examples.

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
  - Example: anthropic/claude-3-5-sonnet-20240620

Example usage:

- `=PROMPTWITH("openai/gpt-4o-mini", A1:D10, "Extract keywords")` will extract keywords using OpenAI's GPT-4o mini model instead of the default model.

## Use Cases
Cellm is useful for repetitive tasks on both structured and unstructured data. Here are some practical applications:

1. **Text Classification**
    ```excel
    =PROMPT(B2, "Analyze the survey response. Categorize as 'Product', 'Service', 'Pricing', or 'Other'.")
    ```
    Use classification prompts to quickly categorize large volumes of e.g. open-ended survey responses.

2. **Model Comparison**
    
    Make a sheet with user queries in the first column and provider/model pairs in the first row. Write this prompt in the cell B2:
    ```excell
    =PROMPTWITH(B$1,$A2,"Answer the question in column A")
    ```
    Drag the cell across the entire table to apply all models to all queries.

3. **Data Cleaning**
    ```excel
    =PROMPT(E2, "Standardize the company name by removing any legal entity identifiers (e.g., Inc., LLC) and correcting common misspellings.")
    ```
    Useful for cleaning and standardizing messy datasets.

4. **Content Summarization**
    ```excel
    =PROMPT(F2, "Provide a 2-sentence summary of the article in the context.")
    ```
    Great for quickly digesting large amounts of text data, such as news articles or research papers.

5. **Entity Extraction**
    ```excel
    =PROMPT(G2, "Extract all person names mentioned in the text.")
    ```
    Useful for analyzing unstructured text data in fields like journalism, research, or customer relationship management.

6. **When Built-in Excel Functions Are Insufficient**
    ```
    =PROMPT(A1, "Fix email formatting")
    ```
    Useful when an "auditor" inserts random spaces in a column with thousands of email adresses. Use a local model if you are worried about sending sensitive data to hosted models.

These use cases are starting points. Experiment with different instructions to find what works best for your data. It works best when combined with human judgment and expertise in your specific domain.

## Run Models Locally

Cellm can run LLM models locally on your computer via Llamafiles, Ollama, or vLLM. This ensures none of your data ever leaves your machine. And its free.

By default Cellm uses Gemma 2 2B model with 4-bit quantization. This clever little model runs fine on a CPU.

### LLamafile

Llamafile is a stand-alone executable that is very easy to setup. Cellm will automatically download a Llamafile and run it for you the first time you use a Llamafile model. 

To get started:

1. Rename `appsettings.Llamafile.json` to `appsettings.Local.json`.
2. Build and install Cellm.
3. Run e.g. `=PROMPT(A1, "Extract keywords")` in a formula.
4. Wait 5-10 min depending on your internet connection. The model will reply once it is ready. 

Use `appsettings.Llamafile.GPU.json` to offload inference to your NVIDIA or AMD GPU.

### Ollama and vLLM

Ollama and vLLM are LLM inference servers. Ollama is designed for easy of use and vLLM is designed to run models efficiently with high-throughput. Both Ollama and vLLM are packaged up as docker compose files that can run models locally on your computer.

To get started, use Ollama with the Gemma 2 2B model with 4-bit quantization. This clever little model runs fine on a CPU.

1. Rename `appsettings.Ollama.json` to `appsettings.Local.json`, 
2. Build and install Cellm.
3. Run the following command in the docker directory:   
   ```cmd
   docker compose -f docker-compose.Ollama.yml up --detach
   docker compose -f docker-compose.Ollama.yml exec backend ollama pull gemma2:2b
   docker compose -f docker-compose.Ollama.yml down  // When you want to shut it down
   ```

Open WebUI in included in both docker compose files so you test the local model outside of Cellm. It is available at `http://localhost:3000`.

If you want to speed up inference, you can use your GPU as well:

```cmd
docker compose -f docker-compose.Ollama.yml -f docker-compose.Ollama.GPU.yml up --detach
```

A GPU is practically required if you want to use larger models than Gemma 2 2b.

Iff you want to further speed up running many requests in parallel, you can use vLLM instead of Ollama. You must supply the docker compose file with a Huggingface token API key either via an environment variable or editing the docker compose file directy. If you don't know what an API key is just use a Llamafile model or Ollama. To start vLLM:

```cmd
docker compose -f docker-compose.vLLM.GPU.yml up --detach
```

## Dos and Don'ts

Do:

- Experiment with different prompts to find the most effective instructions for your data.
- Use cell references to dynamically change your prompts based on other data in your spreadsheet.
- Use local models for sensitive data. Always consider the privacy implications of the data you're sending cloud-based LLM providers.
- Refer to the cell data as "context" in your instructions.
- Verify responses, especially for critical decisions or analyses. These models will make errors and rely entirely on your input, which may also contain errors.

Don't:

- Don't use Cellm to compute sums, averages, and other numerical calculations. The current generation of LLMs are not designed for mathematical operations. Use Excel's existing functions instead.
- Don't use cloud model providers to process sensitive or confidential information unless you've carefully reviewed your data and privacy policies of the LLM provider.
- Don't use extremely long prompts or give Cellm complex tasks. A normal chat UI lets you have a back and forth conversation which is better for exploring complex topics.
- Don't use Cellm for tasks that require up-to-date information beyond the AI model's knowledge cutoff date _unless_ you provide the information as context.

## Why did you make Cellm?
My girlfriend was writing a systematic review paper. She had to compare 7.500 papers against inclusion and exclusion criterias. I told her this was a great use case for LLMs but quickly realized that individually copying 7.500 papers in and out of chat windows was a total pain. This sparked the idea to make an AI tool to automate repetitive tasks for people like her who would rather avoid programming. 

I think Cellm is really cool because it enables everyone to automate repetitive tasks with AI to a level that was previously available only to programmers. My girlfriend still did her analysis manually, of couse, because she cares about scientific integrity.

## License

Fair Core License, Version 1.0, Apache 2.0 Future License
