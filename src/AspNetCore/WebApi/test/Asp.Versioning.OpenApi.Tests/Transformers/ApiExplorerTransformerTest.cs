// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning.OpenApi.Transformers;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

public class ApiExplorerTransformerTest
{
    [Fact]
    public async Task transform_should_use_document_title_from_configuration()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer();
        var context = NewContext( new() { ["OpenApi:Document:Title"] = "Contoso API" } );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Title.Should().Be( "Contoso API | v1" );
    }

    [Fact]
    public async Task transform_should_use_title_from_configuration()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer();
        var context = NewContext( new() { ["OpenApi:Title"] = "Contoso API" } );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Title.Should().Be( "Contoso API | v1" );
    }

    [Fact]
    public async Task transform_should_prefer_document_title_over_title_from_configuration()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer();
        var context = NewContext( new()
        {
            ["OpenApi:Document:Title"] = "Contoso API",
            ["OpenApi:Title"] = "Fabrikam API",
        } );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Title.Should().Be( "Contoso API | v1" );
    }

    [Fact]
    public async Task transform_should_use_title_from_assembly_info()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer();
        var context = NewContext();

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Title.Should().Be( "Test API | v1" );
    }

    [Fact]
    public async Task transform_should_use_title_from_assembly_info_when_configuration_is_empty()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer();
        var context = NewContext( [] );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Title.Should().Be( "Test API | v1" );
    }

    [Fact]
    public async Task transform_should_format_title_with_custom_format()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer( title => title.Format = ( name, api ) => $"{name} v{api.ApiVersion}" );
        var context = NewContext( new() { ["OpenApi:Document:Title"] = "Contoso API" } );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Title.Should().Be( "Contoso API v1.0" );
    }

    [Fact]
    public async Task transform_should_not_overwrite_existing_title()
    {
        // arrange
        var document = NewDocument( title: "Explicit API" );
        var transformer = NewTransformer();
        var context = NewContext( new() { ["OpenApi:Document:Title"] = "Contoso API" } );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Title.Should().Be( "Explicit API" );
    }

    [Fact]
    public async Task transform_should_overwrite_default_title_from_host_environment()
    {
        // arrange
        var document = NewDocument( title: "Contoso.Api | v1" );
        var transformer = NewTransformer();
        var context = NewContext(
            new() { ["OpenApi:Document:Title"] = "Contoso API" },
            applicationName: "Contoso.Api" );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Title.Should().Be( "Contoso API | v1" );
    }

    [Fact]
    public async Task transform_should_not_overwrite_title_matching_a_different_application_name()
    {
        // arrange
        var document = NewDocument( title: "Fabrikam.Api | v1" );
        var transformer = NewTransformer();
        var context = NewContext(
            new() { ["OpenApi:Document:Title"] = "Contoso API" },
            applicationName: "Contoso.Api" );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Title.Should().Be( "Fabrikam.Api | v1" );
    }

    [Fact]
    public async Task transform_should_use_document_description_from_configuration()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer();
        var context = NewContext( new() { ["OpenApi:Document:Description"] = "The Contoso API." } );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Description.Should().Be( "The Contoso API." );
    }

    [Fact]
    public async Task transform_should_use_description_from_configuration()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer();
        var context = NewContext( new() { ["OpenApi:Description"] = "The Contoso API." } );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Description.Should().Be( "The Contoso API." );
    }

    [Fact]
    public async Task transform_should_prefer_document_description_over_description_from_configuration()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer();
        var context = NewContext( new()
        {
            ["OpenApi:Document:Description"] = "The Contoso API.",
            ["OpenApi:Description"] = "The Fabrikam API.",
        } );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Description.Should().Be( "The Contoso API." );
    }

    [Fact]
    public async Task transform_should_not_overwrite_existing_description()
    {
        // arrange
        var document = NewDocument( description: "The explicit API." );
        var transformer = NewTransformer();
        var context = NewContext( new() { ["OpenApi:Document:Description"] = "The Contoso API." } );

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Description.Should().Be( "The explicit API." );
    }

    [Fact]
    public async Task transform_should_set_version_from_description()
    {
        // arrange
        var document = NewDocument();
        var transformer = NewTransformer();
        var context = NewContext();

        // act
        await transformer.TransformAsync( document, context, TestContext.Current.CancellationToken );

        // assert
        document.Info.Version.Should().Be( "1.0" );
    }

    private static OpenApiDocument NewDocument( string title = default, string description = default ) =>
        new() { Info = new() { Title = title, Description = description } };

    private static ApiExplorerTransformer NewTransformer( Action<OpenApiDocumentTitleOptions> setupTitle = default )
    {
        var documentTitle = new OpenApiDocumentTitleOptions();

        setupTitle?.Invoke( documentTitle );

        return new(
            new()
            {
                Description = new( new ApiVersion( 1.0 ), "v1" ),
                Document = new(),
                DocumentTitle = documentTitle,
                DocumentDescription = new(),
            } );
    }

    private static OpenApiDocumentTransformerContext NewContext(
        Dictionary<string, string> settings = default,
        string applicationName = default )
    {
        var services = new ServiceCollection();

        if ( settings is not null )
        {
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection( settings ).Build();
            services.AddSingleton( configuration );
        }

        if ( applicationName is not null )
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet( e => e.ApplicationName ).Returns( applicationName );
            services.AddSingleton( environment.Object );
        }

        return new()
        {
            DocumentName = "v1",
            DescriptionGroups = [],
            ApplicationServices = services.BuildServiceProvider(),
        };
    }
}