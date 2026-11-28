namespace VS.Toolset.UI;

/// <summary>
/// Tracks the cancellation source for the active naming analysis or code review.
/// </summary>
internal sealed class AnalysisOperationCoordinator
{
    private CancellationTokenSource? _active;

    /// <summary>
    /// Gets whether a cancellable operation is active.
    /// </summary>
    public bool IsActive => _active is not null;

    /// <summary>
    /// Gets whether the active operation can be canceled.
    /// </summary>
    public bool CanCancel => _active is not null && !_active.IsCancellationRequested;

    /// <summary>
    /// Tracks a newly started operation.
    /// </summary>
    /// <param name="source">
    /// The cancellation source owned by the operation.
    /// </param>
    public void Begin(CancellationTokenSource source)
    {
        if (_active is not null)
            throw new InvalidOperationException("An analysis operation is already active.");
        _active = source;
    }

    /// <summary>
    /// Determines whether the source belongs to the active operation.
    /// </summary>
    /// <param name="source">
    /// The source to compare.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the source is active; otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsCurrent(CancellationTokenSource source) => ReferenceEquals(_active, source);

    /// <summary>
    /// Stops tracking the specified operation.
    /// </summary>
    /// <param name="source">
    /// The source owned by the operation that has completed.
    /// </param>
    public void End(CancellationTokenSource source)
    {
        if (!IsCurrent(source))
            throw new InvalidOperationException("The operation is not active.");
        _active = null;
    }

    /// <summary>
    /// Requests cancellation of the active operation, if any.
    /// </summary>
    public void Cancel() => _active?.Cancel();
}
