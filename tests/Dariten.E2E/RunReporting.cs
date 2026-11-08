using E2E.NUnit;
using NUnit.Framework;

// Outside any namespace on purpose: NUnit then runs this fixture once for the whole assembly.
// E2ERunFixture is what calls the reporters listed in e2e.config.json when the last test is done.
// Without it, no reporter runs (not the GitHub comment, not the markdown file, not the event log).

/// <summary>Ends the run for E2E: writes report.json, then runs every reporter in <c>e2e.config.json</c>.</summary>
[SetUpFixture]
public sealed class RunReporting : E2ERunFixture;
