using E2E.Reports;

namespace Dariten.E2E;

/// <summary>
/// Watches the run as it happens through <see cref="Reporter.OnEvent"/>. Events arrive in <c>Seq</c> order from any thread,
/// so this only counts and prints one line per test, and never throws.
/// </summary>
public sealed class LiveEventReporter : Reporter
{
    private int _started;
    private int _finished;

    public override string Name => "live-events";

    public override void OnEvent(RunEvent runEvent)
    {
        switch (runEvent.Fact)
        {
            case TestStartedEvent started:
                Interlocked.Increment(ref _started);
                Console.Error.WriteLine($"[e2e #{runEvent.Seq}] start  {started.Title}");
                break;
            case TestFinishedEvent:
                Interlocked.Increment(ref _finished);
                break;
            case NoticeEvent notice:
                Console.Error.WriteLine($"[e2e #{runEvent.Seq}] notice {notice.Message}");
                break;
        }
    }

    public override Task<IReadOnlyList<ReporterSummaryRow>?> OnRunFinishedAsync(FinishedRun run, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReporterSummaryRow>?>([new ReporterSummaryRow("Events", $"{_started} started, {_finished} finished")]);
}
