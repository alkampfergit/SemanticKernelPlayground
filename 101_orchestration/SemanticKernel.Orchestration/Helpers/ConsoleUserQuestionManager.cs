using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;

namespace SemanticKernel.Orchestration.Helpers;

public class ConsoleUserQuestionManager : IUserQuestionManager
{
    public Task<string> AskQuestionAsync(string question)
    {
        Console.WriteLine(question);
        return Task.FromResult(Console.ReadLine() ?? string.Empty);
    }

    public Task<string> AskForSelectionAsync(string prompt, IEnumerable<string> options)
    {
        var sample = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to select the example)[/]")
                .AddChoices(options.ToArray()));

        return Task.FromResult(sample);
    }
}
