using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Actors.Core.Local;

/// <summary>
/// Local implementation of an actor reference for in-process actors.
/// </summary>
public class LocalActorRef : IActorRef
{
    private readonly LocalActorCell _cell;
    private readonly ILogger<LocalActorRef>? _logger;

    public string Path { get; }
    public string Name { get; }
    public bool IsValid => !_cell.IsTerminated;

    public LocalActorRef(string path, string name, LocalActorCell cell, ILogger<LocalActorRef>? logger = null)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _cell = cell ?? throw new ArgumentNullException(nameof(cell));
        _logger = logger;
    }

    public async Task TellAsync(object message, IActorRef? sender = null, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            await _cell.SendAsync(message, sender, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send message {MessageType} to actor {ActorPath}", 
                message.GetType().Name, Path);
            throw;
        }
    }

    public async Task<TResponse> AskAsync<TResponse>(object message, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var responseTask = new TaskCompletionSource<TResponse>();

        // Create a temporary actor ref for response handling
        var responseRef = new ResponseActorRef<TResponse>(responseTask);

        try
        {
            // Send the message with the response ref as sender
            await _cell.SendAsync(message, responseRef, cancellationToken);

            // Wait for response with timeout
            using var timeoutCts = new CancellationTokenSource(actualTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                return await responseTask.Task.WaitAsync(combinedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException($"Ask operation timed out after {actualTimeout} waiting for response from {Path}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to ask message {MessageType} to actor {ActorPath}", 
                message.GetType().Name, Path);
            responseTask.TrySetException(ex);
            throw;
        }
    }

    public override string ToString() => $"LocalActorRef({Path})";
}

/// <summary>
/// Temporary actor ref used for handling ask responses.
/// </summary>
internal class ResponseActorRef<T> : IActorRef
{
    private readonly TaskCompletionSource<T> _responseTask;

    public string Path { get; } = "/temp/response";
    public string Name { get; } = "response";
    public bool IsValid => !_responseTask.Task.IsCompleted;

    public ResponseActorRef(TaskCompletionSource<T> responseTask)
    {
        _responseTask = responseTask;
    }

    public Task TellAsync(object message, IActorRef? sender = null, CancellationToken cancellationToken = default)
    {
        if (message is T response)
        {
            _responseTask.TrySetResult(response);
        }
        else
        {
            _responseTask.TrySetException(new InvalidOperationException(
                $"Expected response of type {typeof(T).Name} but received {message.GetType().Name}"));
        }
        return Task.CompletedTask;
    }

    public Task<TResponse> AskAsync<TResponse>(object message, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Response actors cannot be asked");
    }
}