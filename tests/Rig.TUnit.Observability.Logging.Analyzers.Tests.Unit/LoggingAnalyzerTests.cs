namespace Rig.TUnit.Observability.Logging.Analyzers.Tests.Unit;

public sealed class LoggingAnalyzerTests
{
    // ─── RTU001 — Interpolated template ──────────────────────────────────────

    [Test]
    public async Task RTU001_Positive_InterpolatedStringPassedToILogger()
    {
        const string src = """
        using Microsoft.Extensions.Logging;
        class C {
            void M(ILogger log, int id) {
                log.LogInformation($"Order {id} created");
            }
        }
        """;
        var diags = await AnalyzerHarness.RunAsync(src);
        await Assert.That(diags.Any(d => d.Id == "RTU001")).IsTrue();
    }

    [Test]
    public async Task RTU001_Negative_StructuredTemplateIsAllowed()
    {
        const string src = """
        using Microsoft.Extensions.Logging;
        class C {
            void M(ILogger log, int id) {
                log.LogInformation("Order {OrderId} created", id);
            }
        }
        """;
        var diags = await AnalyzerHarness.RunAsync(src);
        await Assert.That(diags.Any(d => d.Id == "RTU001")).IsFalse();
    }

    [Test]
    public async Task RTU001_Boundary_DestructuringAtPrefixAllowed()
    {
        const string src = """
        using Microsoft.Extensions.Logging;
        class C {
            void M(ILogger log, object p) {
                log.LogInformation("Payload {@Body}", p);
            }
        }
        """;
        var diags = await AnalyzerHarness.RunAsync(src);
        await Assert.That(diags.Any(d => d.Id == "RTU001")).IsFalse();
    }

    // ─── RTU002 — Console.Write in non-test assembly ─────────────────────────

    [Test]
    public async Task RTU002_Positive_ConsoleWriteLineInSourceAssembly()
    {
        const string src = """
        class C {
            void M() { System.Console.WriteLine("hi"); }
        }
        """;
        var diags = await AnalyzerHarness.RunAsync(src, "MyLib");
        await Assert.That(diags.Any(d => d.Id == "RTU002")).IsTrue();
    }

    [Test]
    public async Task RTU002_Negative_ConsoleWriteLineInTestAssemblyIgnored()
    {
        const string src = """
        class C {
            void M() { System.Console.WriteLine("hi"); }
        }
        """;
        var diags = await AnalyzerHarness.RunAsync(src, "MyLib.Tests.Unit");
        await Assert.That(diags.Any(d => d.Id == "RTU002")).IsFalse();
    }

    [Test]
    public async Task RTU002_Boundary_ConsoleErrorIsNotFlagged()
    {
        // Only Write/WriteLine are flagged — Console.Error stream write is fine for CLIs.
        const string src = """
        class C {
            void M() { System.Console.Error.Write("x"); }
        }
        """;
        var diags = await AnalyzerHarness.RunAsync(src, "MyLib");
        // Error.Write is ConsoleTextWriter.Write, not Console.Write → not flagged.
        await Assert.That(diags.Any(d => d.Id == "RTU002")).IsFalse();
    }

    // ─── RTU003 — PII property name ──────────────────────────────────────────

    [Test]
    public async Task RTU003_Positive_PasswordPlaceholderFlagged()
    {
        const string src = """
        using Microsoft.Extensions.Logging;
        class C {
            void M(ILogger log, string pwd) {
                log.LogInformation("Login {Password}", pwd);
            }
        }
        """;
        var diags = await AnalyzerHarness.RunAsync(src);
        await Assert.That(diags.Any(d => d.Id == "RTU003")).IsTrue();
    }

    [Test]
    public async Task RTU003_Negative_NonPiiPlaceholderAllowed()
    {
        const string src = """
        using Microsoft.Extensions.Logging;
        class C {
            void M(ILogger log, int id) {
                log.LogInformation("Login {UserId}", id);
            }
        }
        """;
        var diags = await AnalyzerHarness.RunAsync(src);
        await Assert.That(diags.Any(d => d.Id == "RTU003")).IsFalse();
    }

    [Test]
    public async Task RTU003_Boundary_TokenSubstringInPlaceholderAlsoFlagged()
    {
        const string src = """
        using Microsoft.Extensions.Logging;
        class C {
            void M(ILogger log, string x) {
                log.LogInformation("Ctx {UserEmail}", x);
            }
        }
        """;
        var diags = await AnalyzerHarness.RunAsync(src);
        await Assert.That(diags.Any(d => d.Id == "RTU003")).IsTrue();
    }
}
