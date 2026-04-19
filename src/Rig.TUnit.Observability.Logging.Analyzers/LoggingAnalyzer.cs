using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rig.TUnit.Observability.Logging.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LoggingAnalyzer : DiagnosticAnalyzer
{
    private static readonly Regex TemplatePlaceholder = new(@"\{\@?[A-Za-z_][A-Za-z0-9_]*(\:[^}]+)?\}", RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            LoggingDiagnostics.InterpolatedTemplate,
            LoggingDiagnostics.ConsoleWrite,
            LoggingDiagnostics.PiiPropertyName);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
    {
        var invocation = (InvocationExpressionSyntax)ctx.Node;

        if (invocation.Expression is MemberAccessExpressionSyntax ma)
        {
            var methodName = ma.Name.Identifier.ValueText;
            var receiverType = ctx.SemanticModel.GetTypeInfo(ma.Expression).Type
                               ?? ctx.SemanticModel.GetSymbolInfo(ma.Expression).Symbol switch
                               {
                                   INamedTypeSymbol t => t,
                                   _ => null,
                               };

            // RTU002 — Console.Write* in non-test assemblies.
            if (IsConsoleType(ma.Expression, ctx.SemanticModel)
                && (methodName == "Write" || methodName == "WriteLine"))
            {
                if (!IsTestAssembly(ctx.Compilation))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        LoggingDiagnostics.ConsoleWrite,
                        invocation.GetLocation(),
                        methodName));
                }
            }

            // RTU001 / RTU003 — ILogger.Log* family.
            if (IsILoggerMethod(methodName) && IsILoggerReceiver(receiverType))
            {
                AnalyzeLoggerCall(ctx, invocation);
            }
        }
    }

    private static bool IsILoggerMethod(string name) =>
        name == "LogTrace" || name == "LogDebug" || name == "LogInformation" ||
        name == "LogWarning" || name == "LogError" || name == "LogCritical" ||
        name == "Log";

    private static bool IsILoggerReceiver(ITypeSymbol? type)
    {
        if (type is null) return false;
        if (IsOrImplementsILogger(type)) return true;
        return false;
    }

    private static bool IsOrImplementsILogger(ITypeSymbol type)
    {
        if (type.Name == "ILogger" && (type.ContainingNamespace?.ToDisplayString() ?? "").StartsWith("Microsoft.Extensions.Logging"))
            return true;
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name == "ILogger" && (iface.ContainingNamespace?.ToDisplayString() ?? "").StartsWith("Microsoft.Extensions.Logging"))
                return true;
        }
        return false;
    }

    private static bool IsConsoleType(ExpressionSyntax expr, SemanticModel model)
    {
        var info = model.GetSymbolInfo(expr).Symbol ?? model.GetTypeInfo(expr).Type;
        return info switch
        {
            INamedTypeSymbol t => t.Name == "Console" && (t.ContainingNamespace?.ToDisplayString() == "System"),
            _ => false,
        };
    }

    private static bool IsTestAssembly(Compilation compilation)
    {
        var name = compilation.AssemblyName ?? string.Empty;
        return name.Contains(".Tests") || name.Contains(".Test.") || name.EndsWith(".Test");
    }

    private static void AnalyzeLoggerCall(SyntaxNodeAnalysisContext ctx, InvocationExpressionSyntax invocation)
    {
        // Find the message-template argument. Signature shapes:
        //   Log(LogLevel, [EventId,] [Exception,] string msg, params object[] args)
        //   LogInformation([EventId,] [Exception,] string msg, params object[] args)
        ArgumentSyntax? messageArg = null;
        foreach (var arg in invocation.ArgumentList.Arguments)
        {
            var t = ctx.SemanticModel.GetTypeInfo(arg.Expression).Type;
            if (t?.SpecialType == SpecialType.System_String)
            {
                messageArg = arg;
                break;
            }
        }
        if (messageArg is null) return;

        // RTU001 — interpolated string literal as template.
        if (messageArg.Expression is InterpolatedStringExpressionSyntax)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                LoggingDiagnostics.InterpolatedTemplate,
                messageArg.GetLocation()));
        }

        // RTU003 — PII-shaped placeholder names in the template.
        if (messageArg.Expression is LiteralExpressionSyntax lit && lit.Token.ValueText is { } template)
        {
            foreach (Match m in TemplatePlaceholder.Matches(template))
            {
                var raw = m.Value.Trim('{', '}').TrimStart('@');
                var name = raw.Split(':')[0];
                if (PiiTokens.IsPii(name))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        LoggingDiagnostics.PiiPropertyName,
                        messageArg.GetLocation(),
                        name));
                }
            }
        }
    }
}
