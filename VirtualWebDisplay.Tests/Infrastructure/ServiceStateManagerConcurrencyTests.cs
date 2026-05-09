using VirtualWebDisplay.Infrastructure.Runtime;

namespace VirtualWebDisplay.Tests.Infrastructure;

public sealed class ServiceStateManagerConcurrencyTests
{
    [Fact]
    public async Task WaitForStartRequestAsync_CompletesWithTrue_WhenSignalStartRequest()
    {
        var manager = new ServiceStateManager(ServiceState.Stopped);

        var waitTask = manager.WaitForStartRequestAsync();
        manager.SignalStartRequest();

        var completed = await waitTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(completed);
    }

    [Fact]
    public async Task WaitForStartRequestAsync_CompletesWithFalse_WhenSignalNoRestart()
    {
        var manager = new ServiceStateManager(ServiceState.Stopped);

        var waitTask = manager.WaitForStartRequestAsync();
        manager.SignalNoRestart();

        var completed = await waitTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(completed);
    }

    [Fact]
    public void ConcurrentRequestStart_IsSafeAndEndsInStartingOrStarted()
    {
        var manager = new ServiceStateManager(ServiceState.Stopped);

        Parallel.For(0, 200, _ => manager.RequestStart());

        Assert.True(
            manager.CurrentState is ServiceState.Starting or ServiceState.Started,
            $"Unexpected state {manager.CurrentState}");
    }

    [Fact]
    public void ConcurrentRequestStop_FromStarted_IsSafeAndEndsInStoppingOrStopped()
    {
        var manager = new ServiceStateManager(ServiceState.Started);

        Parallel.For(0, 200, _ => manager.RequestStop());

        Assert.True(
            manager.CurrentState is ServiceState.Stopping or ServiceState.Stopped,
            $"Unexpected state {manager.CurrentState}");
    }

    [Fact]
    public async Task StartStopCycle_RepeatedConcurrently_DoesNotDeadlockAndCanSignalRestart()
    {
        var manager = new ServiceStateManager(ServiceState.Stopped);

        using var runtime = Web.Handlers.WebHandlerTestHelper.CreateRuntime();

        for (var i = 0; i < 20; i++)
        {
            manager.RequestStart();
            manager.CompleteStart([runtime]);
            manager.RequestStop();
            manager.CompleteStop();

            var waitTask = manager.WaitForStartRequestAsync();
            manager.SignalStartRequest();
            var restart = await waitTask.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.True(restart);
        }

        Assert.Equal(ServiceState.Stopped, manager.CurrentState);
    }
}
