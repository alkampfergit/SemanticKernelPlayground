using System;

namespace SemanticKernel.Orchestration.Helpers;

public class DisposableAction : IDisposable
{
    private readonly Action _action;
    private bool _disposed;

    public DisposableAction(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _action();
            }

            _disposed = true;
        }
    }
}
