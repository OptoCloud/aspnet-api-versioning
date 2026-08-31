// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning;

/// <content>
/// Provides the implementation for ASP.NET Web API.
/// </content>
public partial class MediaTypeApiVersionReader
{
    /// <inheritdoc />
    public virtual IReadOnlyList<string> Read( HttpRequestMessage request )
    {
        ArgumentNullException.ThrowIfNull( request );

        var contentType = request.Content?.Headers.ContentType;
        var version = contentType is null ? default : ReadContentTypeHeader( contentType );
        var accept = request.Headers.Accept;

        if ( accept is null || accept.Count == 0 )
        {
            return version is null ? [] : [version];
        }

        var otherVersions = ReadAcceptHeader( version is null ? accept : MediaTypeQuality.MaxRanked( accept ) );

        return Collate( version, otherVersions );
    }
}