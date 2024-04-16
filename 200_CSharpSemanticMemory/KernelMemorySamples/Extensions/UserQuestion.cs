using Microsoft.KernelMemory;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticMemory.Extensions;

/// <summary>
/// This class represent user question and allows for more advanced scenario
/// like query-rewrite, query-expansion and other stuff.
/// </summary>
public class UserQuestion
{
    public UserQueryOptions UserQueryOptions { get; }

    public string Question { get; }

    /// <summary>
    /// Optional list of filter to perform the query.
    /// </summary>
    public ICollection<MemoryFilter>? Filters { get; }

    public List<Question> ExpandedQuestions { get; }

    /// <summary>
    /// List of citations that were used to answer the question
    /// </summary>
    public List<Citation> Citations { get; set; }

    /// <summary>
    /// This is the Answer to the question, when an handler popuplate this field
    /// it means that the question has been answered.
    /// </summary>
    public string? Answer { get; set; }

    public bool Answered => !string.IsNullOrEmpty(Answer);

    /// <summary>
    /// The <see cref="IQueryHandler"/> that answered the question
    /// </summary>
    public string? AnswerHandler { get; internal set; }

    public UserQuestion(
        UserQueryOptions userQueryOptions,
        string question,
        ICollection<MemoryFilter>? filters = null)
    {
        UserQueryOptions = userQueryOptions;
        Question = question;
        Filters = filters;
        ExpandedQuestions = [];
        Citations = [];
    }
}

public class UserQueryOptions
{
    public UserQueryOptions(string index)
    {
        Index = index;
    }

    public double MinRelevance { get; set; }

    public string Index { get; private set; }
    public int RetrievalQueryLimit { get; internal set; }
}

/// <summary>
/// Handlers are responsible for handling user questions to perform various operations
/// </summary>
public interface IQueryHandler
{
    /// <summary>
    /// Handle the question, usually it will modify the UserQuestion object until we reach
    /// the final step where we have all segments used to generate the answer.
    /// </summary>
    /// <param name="userQuestion"></param>
    /// <returns></returns>
    Task HandleAsync(UserQuestion userQuestion, CancellationToken cancellationToken);

    /// <summary>
    /// Each handler has a name to identify it.
    /// </summary>
    string Name { get; }
}

public interface IAsyncQueryHandler : IQueryHandler
{
    /// <summary>
    /// Same functions of HandleAsync but with streaming support, because it is capable of raising 
    /// events while the answer is being generated.
    /// </summary>
    /// <param name="userQuestion"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<UserQuestionProgress> HandleStreamingAsync(UserQuestion userQuestion, CancellationToken cancellationToken);
}

public abstract class BasicQueryHandler : IQueryHandler
{
    public abstract string Name { get; }

    public Task HandleAsync(UserQuestion userQuestion, CancellationToken cancellationToken) 
    {
        return OnHandleAsync(userQuestion, cancellationToken);
    }

    protected abstract Task OnHandleAsync(UserQuestion userQuestion, CancellationToken cancellationToken);
}

public abstract class BasicAsyncQueryHandler : IAsyncQueryHandler
{
    public abstract string Name { get; }

    public async Task HandleAsync(UserQuestion userQuestion, CancellationToken cancellationToken)
    {
        //we can delegate to the async enumerable
        var enumerable = HandleStreamingAsync(userQuestion, cancellationToken);
        await foreach (var progress in enumerable)
        {
            //Actually since the client is not interested in the streaming, we can simpli ignore
            //all progress messages.
        }
    }

    protected abstract IAsyncEnumerable<UserQuestionProgress> OnHandleStreamingAsync(UserQuestion userQuestion, CancellationToken cancellationToken);

    public IAsyncEnumerable<UserQuestionProgress> HandleStreamingAsync(UserQuestion userQuestion, CancellationToken cancellationToken)
    {
        return OnHandleStreamingAsync(userQuestion, cancellationToken);
    }
}

public record Question(string Text);