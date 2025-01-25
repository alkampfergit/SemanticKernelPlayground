using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace SemanticKernel.Orchestration.Tests.Helpers;

public class MockTextGenerationService : ITextGenerationService
{
    private Func<string, Task<IReadOnlyList<TextContent>>>? _textResponseGenerator;
    private Func<string, IAsyncEnumerable<StreamingTextContent>>? _streamingResponseGenerator;
    private IReadOnlyDictionary<string, object?> _attributes = new Dictionary<string, object?>();

    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    public void SetResponseGenerator(Func<string, Task<IReadOnlyList<TextContent>>> generator)
    {
        _textResponseGenerator = generator;
    }

    public void SetStreamingResponseGenerator(Func<string, IAsyncEnumerable<StreamingTextContent>> generator)
    {
        _streamingResponseGenerator = generator;
    }

    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (_textResponseGenerator == null)
        {
            throw new InvalidOperationException("Response generator not set");
        }

        return _textResponseGenerator(prompt);
    }

    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (_streamingResponseGenerator == null)
        {
            throw new InvalidOperationException("Streaming response generator not set");
        }

        return _streamingResponseGenerator(prompt);
    }
}
