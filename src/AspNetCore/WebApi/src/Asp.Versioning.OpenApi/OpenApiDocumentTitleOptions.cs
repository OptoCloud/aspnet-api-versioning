// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning.OpenApi;

using Asp.Versioning.ApiExplorer;

/// <summary>
/// Represents the options used to format the OpenAPI document title.
/// </summary>
public class OpenApiDocumentTitleOptions
{
    /// <summary>
    /// Gets or sets a function used to format the document title.
    /// </summary>
    /// <value>The <see cref="Func{T1, T2, TResult}">function</see> used to format the document title. The default
    /// format is <c>"{Title} | {GroupName}"</c>.</value>
    public Func<string, ApiVersionDescription, string> Format { get; set; } = DefaultFormat;

    private static string DefaultFormat( string title, ApiVersionDescription api ) => $"{title} | {api.GroupName}";
}