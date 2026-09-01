// Copyright (c) .NET Foundation and contributors. All rights reserved.

#pragma warning disable IDE0130

namespace Asp.Versioning.Analyzers;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using static Descriptor;
using static Microsoft.CodeAnalysis.Diagnostics.GeneratedCodeAnalysisFlags;

/// <summary>
/// Represents an analyzer that reports OpenAPI configured without regard to API versions.
/// </summary>
/// <remarks>
/// Versioned OpenAPI registers services of its own in place of the ones OpenAPI registers for itself,
/// which describe a single document that knows nothing about API versions.
/// </remarks>
[DiagnosticAnalyzer( LanguageNames.CSharp )]
public sealed class VersionedOpenApiAnalyzer : DiagnosticAnalyzer
{
    private const string AddApiExplorer = nameof( AddApiExplorer );
    private const string AddODataApiExplorer = nameof( AddODataApiExplorer );
    private const string AddGrpcApiExplorer = nameof( AddGrpcApiExplorer );
    private const string AddOpenApi = nameof( AddOpenApi );
    private const string ApiVersioningBuilderExtensions =
        "Microsoft.Extensions.DependencyInjection.IApiVersioningBuilderExtensions";
    private const string OpenApiServiceCollectionExtensions =
        "Microsoft.Extensions.DependencyInjection.OpenApiServiceCollectionExtensions";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create( AV0029_UnnecessaryOpenApiServices );

    public override void Initialize( AnalysisContext context )
    {
        context.ConfigureGeneratedCodeAnalysis( Analyze | ReportDiagnostics );
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction( OnCompilationStart );
    }

    private static void OnCompilationStart( CompilationStartAnalysisContext context )
    {
        // the services do not exist to be registered without the library declaring them
        if ( !Symbols.IsReferenced( context.Compilation, OpenApiServiceCollectionExtensions ) )
        {
            return;
        }

        var analysis = new Analysis();

        context.RegisterSyntaxNodeAction( analysis.OnInvocation, SyntaxKind.InvocationExpression );
        context.RegisterCompilationEndAction( analysis.OnCompilationEnd );
    }

    private sealed class Analysis
    {
        private readonly ConcurrentBag<Location> serviceCallSites = [];
        private volatile bool versioned;

        public void OnInvocation( SyntaxNodeAnalysisContext context )
        {
            var invocation = (InvocationExpressionSyntax) context.Node;

            if ( context.SemanticModel.GetSymbolInfo( invocation, context.CancellationToken ).Symbol
                 is not IMethodSymbol method ||
                 Symbols.ResolveDeclaringType( method ) is not { } type )
            {
                return;
            }

            // OpenAPI declares an AddOpenApi of its own, which is the one that knows nothing of versions
            var declaringType = type.ToDisplayString();

            switch ( method.Name )
            {
                case AddApiExplorer or AddODataApiExplorer or AddGrpcApiExplorer or AddOpenApi
                    when declaringType == ApiVersioningBuilderExtensions:
                    versioned = true;
                    break;
                case AddOpenApi when declaringType == OpenApiServiceCollectionExtensions:
                    serviceCallSites.Add(
                        invocation.Parent is ExpressionStatementSyntax statement
                        ? statement.GetLocation()
                        : invocation.GetLocation() );
                    break;
            }
        }

        public void OnCompilationEnd( CompilationAnalysisContext context )
        {
            if ( !versioned )
            {
                return;
            }

            foreach ( var callSite in serviceCallSites )
            {
                context.ReportDiagnostic( Diagnostic.Create( AV0029_UnnecessaryOpenApiServices, callSite ) );
            }
        }
    }
}