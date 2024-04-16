using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.Extensions.DependencyInjection;
using SemanticMemory.Helper.Pipeline;
using SemanticMemory.Samples;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogIntercepting;

public static class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddSingleton<SimpleBookIndexingWithTextCleaning>();
        services.AddSingleton<SBertSample>();
        services.AddSingleton<BasicSample>();
        services.AddSingleton<TextCleanerHandler>();
        services.AddSingleton<CustomSearchPipelineBase>();
        services.AddSingleton<AnthropicSample>();
        services.AddHttpClient();

        var serviceProvider = services.BuildServiceProvider();

        // Ask for the user's favorite fruits
        var choices = new Dictionary<string, Type?>
        {
            ["Basic Sample"] = typeof(SimpleBookIndexingWithTextCleaning),
            ["Custom Pipeline (text cleaner)"] = typeof(SimpleBookIndexingWithTextCleaning),
            ["SBert in action"] = typeof(SBertSample),
            ["Custom Search pipeline (Basic)"] = typeof(CustomSearchPipelineBase),
            ["Anthropic"] = typeof(AnthropicSample),
            ["Exit"] = null
        };

        Type? sampleType;
        do
        {
            var sample = AnsiConsole.Prompt(
              new SelectionPrompt<string>()
                  .Title("Choose the option to run?")
                  .PageSize(10)
                  .MoreChoicesText("[grey](Move up and down to select the example)[/]")
                  .AddChoices(choices.Keys.ToArray()));

            var book = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select the [green]book[/] to index")
                .AddChoices([@"c:\temp\advancedapisecurity.pdf", @"S:\OneDrive\B19553_11.pdf"]));

            sampleType = choices[sample];
            if (sampleType != null)
            {
                var sampleInstance = (ISample) serviceProvider.GetRequiredService(sampleType);  
                await sampleInstance.RunSample(book);
            }
        } while (sampleType != null);
    }
}