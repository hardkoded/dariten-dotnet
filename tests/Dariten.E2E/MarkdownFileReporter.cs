using E2E.Reports;

namespace Dariten.E2E;

/// <summary>
/// Renders the finished run as one markdown page with <see cref="MarkdownReport.RenderMarkdownReport"/> and writes it to
/// <c>.e2e/report.md</c>. It is the same page E2E.GitHub posts as a pull request comment, so you can read a run locally.
/// </summary>
public sealed class MarkdownFileReporter : Reporter
{
    public override string Name => "markdown-file";

    public override async Task<IReadOnlyList<ReporterSummaryRow>?> OnRunFinishedAsync(FinishedRun run, CancellationToken cancellationToken)
    {
        var page = MarkdownReport.RenderMarkdownReport(
            run.Report,
            new MarkdownReportOptions
            {
                ArtifactsDir = run.ArtifactsRoot,
                Title = "Dariten",
            });

        var path = Path.Combine(run.ProjectRoot, ".e2e", "report.md");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, page, cancellationToken);

        return [new ReporterSummaryRow("Markdown", path)];
    }
}
