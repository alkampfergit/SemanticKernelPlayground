using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticMemory.Extensions;

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
