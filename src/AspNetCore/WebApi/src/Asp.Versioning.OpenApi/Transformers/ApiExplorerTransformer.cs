// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning.OpenApi.Transformers;

using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a <see cref="IOpenApiDocumentTransformer">transformer</see> used to apply API Explorer metadata to an
/// OpenAPI document.
/// </summary>
[CLSCompliant( false )]
public class ApiExplorerTransformer :
    IOpenApiSchemaTransformer,
    IOpenApiDocumentTransformer,
    IOpenApiOperationTransformer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiExplorerTransformer"/> class.
    /// </summary>
    /// <param name="options">The <see cref="VersionedOpenApiOptions">options</see> applied
    /// to OpenAPI document descriptions.</param>
    public ApiExplorerTransformer( VersionedOpenApiOptions options ) => Options = options;

    /// <summary>
    /// Gets the associated, versioned OpenAPI options.
    /// </summary>
    /// <value>The associated <see cref="VersionedOpenApiOptions">options</see>.</value>
    protected VersionedOpenApiOptions Options { get; }

    /// <summary>
    /// Gets or sets the OpenApi extension name.
    /// </summary>
    /// <value>The OpenAPI extension name. The default value is <c>x-api-versioning</c>.</value>
    protected string ExtensionName { get; set; } = "x-api-versioning";

    /// <inheritdoc />
    public virtual Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken )
    {
        ArgumentNullException.ThrowIfNull( schema );
        ArgumentNullException.ThrowIfNull( context );

        if ( context.ParameterDescription?.DefaultValue is string value )
        {
            schema.Enum ??= new List<JsonNode>( 1 );
            schema.Enum.Add( JsonNode.Parse( $"\"{value}\"" )! );
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken )
    {
        ArgumentNullException.ThrowIfNull( document );
        ArgumentNullException.ThrowIfNull( context );

        var services = context.ApplicationServices;
        var info = new DocInfo( services.GetService<IConfiguration>()?.GetSection( "OpenApi" ) );

        UpdateInfo( document.Info, info, Options, DefaultTitle( services, context.DocumentName ) );

        document.Info.Version = Options.Description.ApiVersion.ToString();

        UpdateDescriptionToMarkdown( document, Options.Description, Options.DocumentDescription );
        AddLinkExtensions( document, Options.Description );

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken )
    {
        ArgumentNullException.ThrowIfNull( operation );
        ArgumentNullException.ThrowIfNull( context );

        operation.Deprecated |= context.Description.IsDeprecated;

        if ( operation.Parameters is not { } parameters )
        {
            return Task.CompletedTask;
        }

        var descriptions = context.Description.ParameterDescriptions;

        for ( var i = 0; i < descriptions.Count; i++ )
        {
            var description = descriptions[i];

            if ( description.ModelMetadata is not { } metadata
                || string.IsNullOrEmpty( metadata.Description ) )
            {
                continue;
            }

            for ( var j = 0; j < parameters.Count; j++ )
            {
                var parameter = parameters[j];

                if ( parameter.Name == description.Name
                     && string.IsNullOrEmpty( parameter.Description ) )
                {
                    parameter.Description = metadata.Description;
                }
            }
        }

        return Task.CompletedTask;
    }

    // OpenApiDocumentService always seeds the title as "{ApplicationName} | {DocumentName}". that is
    // indistinguishable from an explicitly configured title unless the same value is recomputed here.
    // REF: https://github.com/dotnet/aspnetcore/blob/main/src/OpenApi/src/Services/OpenApiDocumentService.cs
    private static string? DefaultTitle( IServiceProvider services, string documentName ) =>
        services.GetService<IHostEnvironment>() is { } environment
        ? $"{environment.ApplicationName} | {documentName}"
        : default;

    private static void UpdateInfo( OpenApiInfo info, DocInfo doc, VersionedOpenApiOptions options, string? defaultTitle )
    {
        if ( !string.IsNullOrEmpty( doc.Title )
             && ( string.IsNullOrEmpty( info.Title ) || info.Title == defaultTitle ) )
        {
            info.Title = options.DocumentTitle.Format( doc.Title, options.Description );
        }

        if ( string.IsNullOrEmpty( info.Description ) )
        {
            info.Description = doc.Description;
        }
    }

    private void UpdateDescriptionToMarkdown(
        OpenApiDocument document,
        ApiVersionDescription api,
        OpenApiDocumentDescriptionOptions options )
    {
        var description = new StringBuilder( document.Info.Description );
        var links = new StringBuilder();

        if ( api.DeprecationPolicy is { } deprecation )
        {
            var notice = options.DeprecationNotice( deprecation );

            if ( !string.IsNullOrEmpty( notice ) )
            {
                AddSentence( description, notice );
            }

            if ( !options.HidePolicyLinks && deprecation.HasLinks )
            {
                AddMarkdownLinks( links, deprecation.Links );
            }
        }
        else if ( api.IsDeprecated )
        {
            var notice = options.DeprecationNotice( new() );

            if ( !string.IsNullOrEmpty( notice ) )
            {
                AddSentence( description, notice );
            }
        }

        if ( api.SunsetPolicy is { } sunset )
        {
            var notice = options.SunsetNotice( sunset );

            if ( !string.IsNullOrEmpty( notice ) )
            {
                AddSentence( description, notice );
            }

            if ( !options.HidePolicyLinks && sunset.HasLinks )
            {
                AddMarkdownLinks( links, sunset.Links );
            }
        }

        if ( links.Length > 0 )
        {
            description.AppendLine()
                       .AppendLine()
                       .AppendLine( "### Links" )
                       .AppendLine()
                       .Append( links );
        }

        document.Info.Description = description.ToString();
    }

    /// <summary>
    /// Determines if the specified link should be rendered.
    /// </summary>
    /// <param name="link">The <see cref="LinkHeaderValue">link</see> to evaluate.</param>
    /// <returns>True if the link should be rendered; otherwise, false.</returns>
    /// <remarks>The default implementation only renders <c>text/html</c> links.</remarks>
    protected virtual bool ShouldRenderLink( LinkHeaderValue link )
    {
        ArgumentNullException.ThrowIfNull( link );
        return StringSegmentComparer.OrdinalIgnoreCase.Equals( link.Type, "text/html" );
    }

    /// <summary>
    /// Renders the specified link as markdown.
    /// </summary>
    /// <param name="markdown">The <see cref="StringBuilder">builder</see> to render the Markdown into.</param>
    /// <param name="link">The <see cref="LinkHeaderValue">link</see> to render.</param>
    protected virtual void RenderLink( StringBuilder markdown, LinkHeaderValue link )
    {
        ArgumentNullException.ThrowIfNull( markdown );
        ArgumentNullException.ThrowIfNull( link );

        if ( StringSegment.IsNullOrEmpty( link.Title ) )
        {
            if ( link.LinkTarget.IsAbsoluteUri )
            {
                markdown.Append( "- " ).AppendLine( link.LinkTarget.OriginalString );
            }
            else
            {
                markdown.Append( "- <a href=\"" )
                        .Append( link.LinkTarget.OriginalString )
                        .Append( "\">" )
                        .Append( link.LinkTarget.OriginalString )
                        .AppendLine( "</a>" );
            }
        }
        else
        {
            markdown.Append( "- [" )
                    .Append( link.Title.ToString() )
                    .Append( "](" )
                    .Append( link.LinkTarget.OriginalString )
                    .Append( ')' );
        }
    }

    private static void AddSentence( StringBuilder text, string sentence )
    {
        if ( text.Length > 0 )
        {
            if ( text[^1] != '.' )
            {
                text.Append( '.' );
            }

            text.Append( ' ' );
        }

        text.Append( sentence );
    }

    private void AddMarkdownLinks( StringBuilder markdown, IList<LinkHeaderValue> links )
    {
        var appendLine = markdown.Length > 0;

        for ( var i = 0; i < links.Count; i++ )
        {
            var link = links[i];

            if ( ShouldRenderLink( link ) )
            {
                if ( appendLine )
                {
                    markdown.AppendLine();
                }

                RenderLink( markdown, link );
                appendLine = true;
            }
        }
    }

    private void AddLinkExtensions( OpenApiDocument document, ApiVersionDescription api )
    {
        var array = new JsonArray();

        if ( api.DeprecationPolicy is { } deprecation && deprecation.HasLinks )
        {
            AddLinks( array, deprecation.Links );
        }

        if ( api.SunsetPolicy is { } sunset && sunset.HasLinks )
        {
            AddLinks( array, sunset.Links );
        }

        if ( array.Count > 0 )
        {
            var obj = new JsonObject();
            var extensions = document.Extensions ??= new Dictionary<string, IOpenApiExtension>();

            obj["links"] = array;
            extensions[ExtensionName] = new JsonNodeExtension( obj );
        }
    }

    [UnconditionalSuppressMessage( "ILLink", "IL2026" )]
    [UnconditionalSuppressMessage( "ILLink", "IL3050" )]
    private void AddLinks( JsonArray array, IList<LinkHeaderValue> links )
    {
        for ( var i = 0; i < links.Count; i++ )
        {
            array.Add( ToJson( links[i] ) );
        }
    }

    /// <summary>
    /// Converts the specified link into JSON as an OpenAPI extension.
    /// </summary>
    /// <param name="link">The <see cref="LinkHeaderValue">link</see> to convert.</param>
    /// <returns>The OpenAPI extension <see cref="JsonObject">JSON</see> node.</returns>
    protected virtual JsonObject ToJson( LinkHeaderValue link )
    {
        ArgumentNullException.ThrowIfNull( link );

        var obj = new JsonObject();

        if ( link.Title.HasValue )
        {
            obj["title"] = link.Title.ToString();
        }

        if ( link.Type.HasValue )
        {
            obj["type"] = link.Type.ToString();
        }

        obj["rel"] = link.RelationType.ToString();
        obj["url"] = link.LinkTarget.ToString();

        if ( link.Media.HasValue )
        {
            obj["media"] = link.Media.ToString();
        }

        if ( link.Languages.Count > 0 )
        {
            obj["lang"] = new JsonArray( [.. link.Languages.Select( l => JsonNode.Parse( l.ToString() ) )] );
        }

        foreach ( var (key, value) in link.Extensions )
        {
            obj[key.ToString()] = value.ToString();
        }

        return obj;
    }

    private sealed class DocInfo( IConfigurationSection? config )
    {
        public string Title
        {
            get
            {
                if ( field is null )
                {
                    if ( config?.Exists() == true )
                    {
                        field = config?["Document:Title"] ?? config?[nameof( Title )];
                    }

                    if ( string.IsNullOrEmpty( field ) && Assembly.GetEntryAssembly() is { } assembly )
                    {
                        field = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
                    }

                    field ??= string.Empty;
                }

                return field;
            }
        }

        public string Description
        {
            get
            {
                if ( field is null )
                {
                    if ( config?.Exists() == true )
                    {
                        field = config?["Document:Description"] ?? config?[nameof( Description )];
                    }

                    if ( string.IsNullOrEmpty( field ) && Assembly.GetEntryAssembly() is { } assembly )
                    {
                        field = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
                    }

                    field ??= string.Empty;
                }

                return field;
            }
        }
    }
}