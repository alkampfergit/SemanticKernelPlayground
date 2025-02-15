using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Helpers;

public interface IUserQuestionManager
{
    Task<string> AskQuestionAsync(string question);
    Task<string> AskForSelectionAsync(string prompt, IEnumerable<string> options);
}


