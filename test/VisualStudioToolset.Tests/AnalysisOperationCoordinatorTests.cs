using VS.Toolset.UI;

namespace VisualStudioToolset.Tests;

[TestClass]
public sealed class AnalysisOperationCoordinatorTests
{
    [TestMethod]
    public void BeginCancelAndEndTrackOperationLifecycle()
    {
        AnalysisOperationCoordinator coordinator = new();
        using CancellationTokenSource source = new();

        Assert.IsFalse(coordinator.IsActive);
        Assert.IsFalse(coordinator.CanCancel);
        coordinator.Begin(source);
        Assert.IsTrue(coordinator.IsActive);
        Assert.IsTrue(coordinator.CanCancel);
        Assert.IsTrue(coordinator.IsCurrent(source));

        coordinator.Cancel();
        Assert.IsTrue(source.IsCancellationRequested);
        Assert.IsFalse(coordinator.CanCancel);
        coordinator.End(source);
        Assert.IsFalse(coordinator.IsActive);
        Assert.IsFalse(coordinator.IsCurrent(source));
    }

    [TestMethod]
    public void BeginRejectsOverlappingOperations()
    {
        AnalysisOperationCoordinator coordinator = new();
        using CancellationTokenSource first = new();
        using CancellationTokenSource second = new();
        coordinator.Begin(first);

        Assert.ThrowsExactly<InvalidOperationException>(() => coordinator.Begin(second));
        Assert.IsTrue(coordinator.IsCurrent(first));
        coordinator.End(first);
    }

    [TestMethod]
    public void EndRejectsSourceFromAnotherOperation()
    {
        AnalysisOperationCoordinator coordinator = new();
        using CancellationTokenSource current = new();
        using CancellationTokenSource other = new();
        coordinator.Begin(current);

        Assert.ThrowsExactly<InvalidOperationException>(() => coordinator.End(other));
        Assert.IsTrue(coordinator.IsCurrent(current));
        coordinator.End(current);
    }
}
