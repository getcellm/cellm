[![CI](https://github.com/getcellm/cellm/actions/workflows/ci.yml/badge.svg)](https://github.com/getcellm/cellm/actions/workflows/ci.yml)

# Cellm
Use AI in Excel formulas to run your prompt on thousands of rows of tasks in minutes.

## What is Cellm?
Cellm is an Excel extension that lets you use Large Language Models (LLMs) like ChatGPT in cell formulas. Cellm's `=PROMPT()` function outputs AI responses to a range of text, similar to how Excel's `=SUM()` function outputs the sum of a range of numbers.  

For example, you can write `=PROMPT(A1, "Extract all person names mentioned in the text.")` in a cell's formula and drag the cell to apply the prompt to many rows. Cellm is useful when you want to use AI for repetitive tasks that would normally require copy-pasting data in and out of a chat window many times.

Read more in our [documentation](https://docs.getcellm.com).

## Why use Cellm?
- Make quick work of data cleaning, classification, and extraction tasks.
- Enable marketing, finance, sales, operations and other teams to automate everyday tasks without depending on developers.
- Immediately free yourself and your team from repetitive manual work with the spreadsheet they already master.
- Bypass lengthy rollouts of specialized AI apps. Your team already have Excel on their computers.

> “I love feeding data to ChatGPT, one copy-paste at a time”
> — no one who’s run the same prompt 5 times

## Example
Say you need to track your international competitors, but their websites are in different languages. Visiting each one, finding the latest update, and plugging it into a translation tool totally sucks. Instead, let Cellm do the manual work for you:

https://github.com/user-attachments/assets/8967f557-50b8-4e39-80e8-86a1246c5a42

This example uses news websites. We give Cellm a list of URLs and write a simple prompt that asks Cellm to grab the top headline from each one. Then, in the next columns, we ask the model to translate the headline, identify its original language, and even sort it into a category like "Politics" or "Business". 

With a drag to autofill, Cellm visits every site, pulls your data and organizes it for you. What would have taken perhaps an hour of manual work is now done in seconds. Imagine what you could prepare every day before your daily 09:00 meeting.

Just remember that the models do make mistakes at times. They might misunderstand a headline or assign the wrong category. It is your responsibility to validate that the results are accurate enough for your use case.

## Quick start

### Requirements

- Windows 10 or higher
- [.NET 9.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Excel 2010 or higher (desktop app)](https://www.microsoft.com/en-us/microsoft-365/excel)

### Install

1. Go to the [Release page](https://github.com/getcellm/cellm/releases) and download `Cellm-AddIn64-packed.xll` and `appsettings.json`. Put them in the _same_ folder.

2. Double-click on `Cellm-AddIn64-packed.xll` and click on "Enable this add-in for this session only" when Excel opens.

3. Download and install [Ollama](https://ollama.com/).

4. Download a model, e.g. Gemma 2 2B: Open Windows Terminal (open start menu, type `Windows Terminal`, and click `OK`), type `ollama pull gemma2:2b`, and wait for the download to finish.

For permanent installation and more options, see our [installation guide](https://docs.getcellm.com/get-started/install).

## Basic usage

Select a cell and type `=PROMPT("What model are you and who made you?")`. For Gemma 3 4B, it will tell you that it's called "Gemma" and made by Google DeepMind.

You can also use cell references. For example, copy a news article into cell A1 and type in cell B1: `=PROMPT(A1, "Extract all person names mentioned in the text")`. You can reference many cells using standard Excel notation, e.g. `=PROMPT(A1:F10, "Extract all person names in the cells")`

For more advanced usage, including function calling and configuration, see our [documentation](https://docs.getcellm.com).

## Models

Cellm supports:
- Hosted models from Azure, AWS, Google, Anthropic, OpenAI, Mistral, and others
- Local models via Ollama, Llamafiles, or vLLM

For detailed information about configuring different models, see our documentation on [local models](https://docs.getcellm.com/models/local-models) and [hosted models](https://docs.getcellm.com/models/hosted-models).

## Use cases

Cellm is useful for repetitive tasks on both structured and unstructured data:

1. **Text classification:** Categorize survey responses, support tickets, etc.
2. **Model comparison:** Compare results from different LLMs side by side
3. **Data cleaning:** Standardize names, fix formatting issues
4. **Content summarization:** Condense articles, papers, or reports
5. **Entity recognition:** Pull out names, locations, dates from text

For more use cases and examples, see our [Prompting Guide](https://docs.getcellm.com/usage/prompting).

## Development

For build instructions with Visual Studio or command line, see our [development guide](https://docs.getcellm.com/get-started/development).

## Why did we make Cellm?
A friend was writing a systematic review paper and had to compare 7,500 papers against inclusion/exclusion criteria to identify papers relevant to her research. We thought this was a great use case for LLMs but quickly realized that individually copying papers in and out of chat windows was a total pain. This sparked the idea to make an AI tool to automate repetitive tasks for people who would rather avoid programming.

A quick prototype enabled her to quickly import a CSV file into Excel and classify all 7,500 papers with a prompt like "If the paper studies diabetic neuropathy and stroke, return INCLUDE otherwise return EXCLUDE". So we decided to develop it further.

We think Cellm is really cool because it enables everyone to automate tasks with AI to a level that was previously available only to programmers.

## Telemetry
To help us improve Cellm, we collect limited, anonymous telemetry data:

- **Crash reports:** To help us fix bugs.
- **Prompts:** To help us understand usage patterns. For example, if you use `=PROMPT(A1:B2, "Extract person names")`, we capture the text "Extract person names" and prompt options. The prompt options are things like the model you use and the temperature setting. We do not capture the data in cells A1:B2. 

We do not collect any data from your spreadsheet and we have no way of associating your prompts with you. You can see for yourself at [src/Cellm/Models/Behaviors/SentryBehavior.cs](src/Cellm/Models/Behaviors/SentryBehavior.cs).

You can disable telemetry at any time by adding the following contents to your `appsettings.json` file in the same folder as `Cellm-AddIn64-packed.xll`:

```json
{
    "SentryConfiguration": {
        "IsEnabled": false
    }
}
```

## License

Fair Core License, Version 1.0, Apache 2.0 Future License
