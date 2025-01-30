# Cellm
Cellm is an Excel extension that lets you use Large Language Models (LLMs) like ChatGPT in cell formulas.

- [Example](#example)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Models](#models)
- [Use Cases](#use-cases)
- [Development](#development)
- [Why did you make Cellm?](#why-did-you-make-cellm)

## What is Cellm?
Cellm's `=PROMPT()` function outputs the AI response to a range of text, similar to how Excel's `=SUM()` function that outputs the sum of a range of numbers.  

For example, you can write `=PROMPT(A1, "Extract all person names mentioned in the text.")` in a cell's formula and drag the cell to apply the prompt to many rows. Cellm is useful when you want to use AI for repetitive tasks that would normally require copy-pasting data in and out of a chat window many times.

## Key features
This extension does one thing and one thing well.

- Calls LLMs in formulas and returns short answers suitable for cells.
- Supports models from Anthropic, Mistral, OpenAI, and Google as well as locally hosted models via Llamafiles, Ollama, or vLLM.

## Example
Say you're reviewing medical studies and need to quickly identify papers relevant to your research. Here's how Cellm can help with this task:

https://github.com/user-attachments/assets/c93f7da9-aabd-4c13-a4f5-3e12332c5794

In this example, we copy the papers' titles and abstracts into Excel and write this prompt: 

> "If the paper studies diabetic neuropathy and stroke, return "Include", otherwise, return "Exclude"."  

We then use autofill to apply the prompt to many papers. Simple and powerful.

Green denotes a correct classification and read denotes and incorrect classification. The models _will_ make mistakes at times and it is your responsibility to cross-validate if a model is accurate enough for your use case and upgrade model or use another approach if not.

## Getting Started

### Requirements

- Windows
- [.NET 9.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Excel 2010 or higher (desktop app)](https://www.microsoft.com/en-us/microsoft-365/excel)

### Install

1. Go to the [Release page](https://github.com/getcellm/cellm/releases) and download `Cellm-AddIn64-packed.xll` and `appsettings.json`. Put them in the _same_ folder.

2. Double-click on `Cellm-AddIn64-packed.xll`. Excel will open and install Cellm.

3. Download and install [Ollama](https://ollama.com/). Cellm uses Ollama and the Gemma 2 2B model by default. Gemma 2 2B will be downloaded automatically the first time you call `=PROMPT()`. To call other models, see the [Models](#models) section below. 

### Uninstall

1. In Excel, go to File > Options > Add-Ins.
2. In the `Manage` drop-down menu, select `Excel Add-ins` and click `Go...`.
3. Uncheck `Cellm-AddIn64.xll` and click `OK`.

## Usage

Just like Excel's built-in functions, you can start typing `=PROMPT(` in any cell to use AI in your spreadsheet. For example:

<img src="https://github.com/user-attachments/assets/4a044178-bc30-4253-9c97-9c9321800725" width=100%>

In this example we use openai/gpt-4o-mini to list PDF files in a folder.

```
=PROMPT(A1, "Which pdf files do I have in my downloads folder?")
```

To configure which AI model you call, use the Cellm tab in Excel's ribbon menu:

- **Model**: Select which AI model to use (e.g., "openai/gpt-4o-mini")
- **Address**: The API endpoint for your chosen provider (e.g., "https://api.openai.com/v1")
- **API Key**: Your authentication key for the selected provider

The other options in the Cellm tab are:
- **Cache**: Enable/disable local caching of model responses. Useful when Excel triggers recalculation of many cells.
- **Functions**: Enable/disable tools (not to be confused with Excel _formula_ functions below).

### Functions
Cellm provides the following functions that can be used in Excel formulas:

#### PROMPT

```excel
PROMPT(cells: range | instruction: string, [instruction: range | instruction: string | temperature: double], [temperature: double]): string
```

- **cells (Required):** A cell or a range of cells.
  - Context and (optionally) instructions. The model will use the cells as context and follow any instructions as long as they are present _somewhere_ in the cells.
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

#### PROMPTWITH

```excel
PROMPTWITH(providerAndModel: string or cell, cells: range | instruction: string, [instruction: range | instruction: string | temperature: double], [temperature: double]): string
```

Allows you to specify the model as the first argument.

- **providerAndModel (Required)**: A string on the form "provider/model".
  - Default: ollama/gemma2:2b

Example usage:

- `=PROMPTWITH("openai/gpt-4o-mini", A1:D10, "Extract keywords")` will extract keywords using OpenAI's GPT-4o mini model instead of the default model.

## Models

Cellm supports hosted models from Anthropic, DeepSeek, Google, OpenAI, Mistral, and any OpenAI-compatible provider.

You can use `appsettings.Local.OpenAiCompatible.json` as a starting point for configuring any model provider that is compatible with OpenAI's API. Just rename it to `appsettings.Local.json` and edit the values. In general, you should leave `appsettings.json` alone and add your own configuration to `appsettings.Local.json` only. Any settings in this file will override the default settings in `appsettings.json`.

The following sections shows you how to configure `appsettings.Local.json` for commonly used hosted and local models.

### Hosted LLMs

Cellm supports hosted models from Anthropic, DeepSeek, Google, OpenAI, Mistral, and any OpenAI-compatible provider. To use e.g. Claude 3.5 Sonnet from Anthropic:

1. Rename `src/Cellm/appsettings.Anthropic.json` to `src/Cellm/appsettings.Local.json`.
 
2. Add your Anthropic API key to `src/Cellm/appsettings.Local.json`:
    ```json
    {
        "AnthropicConfiguration": {
            "ApiKey": "ADD_YOUR_ANTHROPIC_API_KEY_HERE"
        },
        "ProviderConfiguration": {
            "DefaultProvider": "Anthropic"
        }
    }
    ```

See the `appsettings.Local.*.json` files for examples on other providers.

### Local LLMs

Cellm supports local models that run on your computer via Llamafiles, Ollama, or vLLM. This ensures none of your data ever leaves your machine. And it's free. 

#### Ollama 

Cellm uses Ollama Gemma 2 2B model by default. This clever little model runs fine on a CPU. For any model larger than 3B you will need a GPU. Ollama will automatically use your GPU if you have one. To get started:

1. Download and install Ollama from [https://ollama.com/](https://ollama.com/).
2. Download the model by running the following command in your Windows terminal:
   ```cmd
   ollama pull gemma2:2b
   ```

See [https://ollama.com/search](https://ollama.com/search) for a complete list of supported models.

#### LLamafile

Llamafile is a stand-alone executable that is very easy to setup. To get started:

1. Download a llamafile from https://github.com/Mozilla-Ocho/llamafile (e.g. [Gemma 2 2B](https://huggingface.co/Mozilla/gemma-2-2b-it-llamafile/blob/main/gemma-2-2b-it.Q6_K.llamafile?download=true)).
2. Run the following command in your Windows terminal:
    ```cmd
    .\gemma-2-2b-it.Q6_K.llamafile.exe --server --v2
    ```

    To offload inference to your NVIDIA or AMD GPU, run:

    ```cmd
    .\gemma-2-2b-it.Q6_K.llamafile.exe --server --v2 -ngl 999
    ```

3. Rename `appsettings.Llamafile.json` to `appsettings.Local.json`.
4. Build and install Cellm.

#### Dockerized Ollama and vLLM

If you prefer to run models via docker, both Ollama and vLLM are packaged up with docker compose files in the `docker/` folder. Ollama is designed for easy of use and vLLM is designed to run many requests in parallel. vLLM is particularly useful if you need to process a lot of data locally. Open WebUI in included in both Ollama and vLLM docker compose files so you can test the local model outside of Cellm. It is available at `http://localhost:3000`.

To get started, we recommend using Ollama with the Gemma 2 2B model:

1. Build and install Cellm.
2. Run the following command in the `docker/` directory:   
   ```cmd
   docker compose -f docker-compose.Ollama.yml up --detach
   docker compose -f docker-compose.Ollama.yml exec backend ollama pull gemma2:2b
   docker compose -f docker-compose.Ollama.yml down  // When you want to shut it down
   ```

To use other Ollama models, pull another of the [supported models](https://ollama.com/search). If you want to speed up inference, you can use your GPU as well:

```cmd
docker compose -f docker-compose.Ollama.yml -f docker-compose.Ollama.GPU.yml up --detach
```

If you want to further speed up running many requests in parallel, you can use vLLM instead of Ollama. You must supply the docker compose file with a Huggingface API key either via an environment variable or editing the docker compose file directy. Look at the vLLM docker compose file for details. If you don't know what a Huggingface API key is, just use Ollama. 

To start vLLM:

```cmd
docker compose -f docker-compose.vLLM.GPU.yml up --detach
```

To use other vLLM models, change the "--model" argument in the docker compose file to another Huggingface model.

## Dos and Don'ts

Do:

- Experiment with different prompts to find the most effective instructions for your data.
- Use cell references to dynamically change your prompts based on other data in your spreadsheet.
- Use local models for sensitive and confidential data.
- Refer to the cell data as "context" in your instructions.
- Verify at least a subset of a model's responses. Models will make errors and rely entirely on your input, which may also contain errors.

Don't:

- Don't use Cellm to compute sums, averages, and other numerical calculations. The current generation of LLMs are not designed for mathematical operations. Use Excel's existing functions instead.
- Don't use cloud model providers to process sensitive or confidential data.
- Don't use extremely long prompts or give Cellm complex tasks. A normal chat UI lets you have a back and forth conversation which is better for exploring complex topics.
- Don't use Cellm for tasks that require up-to-date information beyond the AI model's knowledge cutoff date _unless_ you provide the information as context.
- 
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

## Development

### Build with Visual Studio

1. In Visual Studio, go to File > Clone Repository.
 
2. Set the Repository Location to `https://github.com/getcellm/cellm`, the Path to a directory of your choice, and click Clone.

3. Run the "Excel" configuration. Visual Studio will build Cellm and open Excel with the output build installed.
 
### Build with command line

1. Clone this repository:
   ```cmd
   git clone https://github.com/getcellm/cellm.git
   ```

2. In your terminal, go into the root of the project directory:
   ```cmd
   cd cellm
   ```

3. Install dependencies:
   ```cmd
   dotnet restore
   ```

4. Build the project:
   ```cmd
   dotnet build --configuration Debug
   ```

## Why did you make Cellm?
My girlfriend was writing a systematic review paper. She had to compare 7.500 papers against inclusion and exclusion criterias. I told her this was a great use case for LLMs but quickly realized that individually copying 7.500 papers in and out of chat windows was a total pain. This sparked the idea to make an AI tool to automate repetitive tasks for people like her who would rather avoid programming. 

I think Cellm is really cool because it enables everyone to automate repetitive tasks with AI to a level that was previously available only to programmers.

## License

Fair Core License, Version 1.0, Apache 2.0 Future License
