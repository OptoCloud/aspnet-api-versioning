// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning.Analyzers.Rules;

public class VersionedOpenApiAnalyzerTest
{
    private const string AV0029 = nameof( AV0029 );

    [Theory]
    [InlineData( "AddApiExplorer" )]
    [InlineData( "AddODataApiExplorer" )]
    [InlineData( "AddGrpcApiExplorer" )]
    [InlineData( "AddOpenApi" )]
    public async Task analyzer_should_report_openapi_services_for_each_explorer( string explorer )
    {
        // arrange
        var source = Configured( $"services.AddApiVersioning().{explorer}();", "services.AddOpenApi();" );

        // act
        var diagnostics = await AnalyzeAsync( source );

        // assert
        diagnostics.Should().ContainSingle().Which.Id.Should().Be( AV0029 );
    }

    [Theory]
    [InlineData( "services.AddOpenApi();" )]
    [InlineData( """services.AddOpenApi( "v1" );""" )]
    [InlineData( "services.AddOpenApi( options => { } );" )]
    public async Task analyzer_should_report_any_form_of_openapi_services( string added )
    {
        // arrange
        var source = Configured( "services.AddApiVersioning().AddOpenApi();", added );

        // act
        var diagnostics = await AnalyzeAsync( source );

        // assert
        diagnostics.Should().ContainSingle().Which.Id.Should().Be( AV0029 );
    }

    [Fact]
    public async Task analyzer_should_not_report_openapi_services_without_versioning()
    {
        // arrange
        // an application that does not version its APIs is described by a single document
        var source = Configured( "services.AddApiVersioning();", "services.AddOpenApi();" );

        // act
        var diagnostics = await AnalyzeAsync( source );

        // assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task analyzer_should_report_the_services_as_unnecessary_code()
    {
        // arrange
        var source = Configured( "services.AddApiVersioning().AddOpenApi();", "services.AddOpenApi();" );

        // act
        var diagnostics = await AnalyzeAsync( source );

        // assert
        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        var span = diagnostic.Location.SourceSpan;

        source.Substring( span.Start, span.Length ).Should().Be( "services.AddOpenApi();" );
        diagnostic.Severity.Should().Be( DiagnosticSeverity.Warning );
        diagnostic.Descriptor.CustomTags.Should().Contain( WellKnownDiagnosticTags.Unnecessary );
    }

    [Fact]
    public async Task analyzer_should_report_each_call_site()
    {
        // arrange
        var openApi = """
            services.AddOpenApi();
            services.AddOpenApi();
            """;
        var source = Configured( "services.AddApiVersioning().AddOpenApi();", openApi );

        // act
        var diagnostics = await AnalyzeAsync( source );

        // assert
        diagnostics.Should().HaveCount( 2 ).And.OnlyContain( diagnostic => diagnostic.Id == AV0029 );
    }

    [Fact]
    public async Task analyzer_should_report_across_files()
    {
        // arrange
        var versioning = Configured( "services.AddApiVersioning().AddOpenApi();", "", "Versioning" );
        var services = Configured( "", "services.AddOpenApi();", "Services" );

        // act
        var diagnostics = await AnalyzeAsync( versioning, services );

        // assert
        diagnostics.Should().ContainSingle().Which.Id.Should().Be( AV0029 );
    }

    // other rules can legitimately apply to the same configuration, so each test is scoped to its own
    private static async Task<IReadOnlyList<Diagnostic>> AnalyzeAsync( params string[] sources ) =>
        [.. ( await AnalyzerVerifier.AnalyzeAsync( sources ) )
            .Where( diagnostic => diagnostic.Id == AV0029 )];

    private static string Configured( string versioning, string openApi, string name = "Startup" ) =>
        $$"""
        using Asp.Versioning;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.Extensions.DependencyInjection;

        public static class {{name}}
        {
            public static void Configure( IServiceCollection services )
            {
                {{versioning}}
                {{openApi}}
            }
        }
        """;
}