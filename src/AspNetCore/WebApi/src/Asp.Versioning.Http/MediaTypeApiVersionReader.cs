// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning;

using Microsoft.AspNetCore.Http;

/// <content>
/// Provides the implementation for ASP.NET Core.
/// </content>
[CLSCompliant( false )]
public partial class MediaTypeApiVersionReader
{
    /// <inheritdoc />
    public virtual IReadOnlyList<string> Read( HttpRequest request )
    {
        ArgumentNullException.ThrowIfNull( request );

        var headers = request.GetTypedHeaders();
        var contentType = headers.ContentType;
        var version = contentType is null ? default : ReadContentTypeHeader( contentType );
        var accept = headers.Accept;

        if ( accept is null || accept.Count == 0 )
        {
            return version is null ? [] : [version];
        }

        var otherVersions = ReadAcceptHeader( version is null ? accept : MediaTypeQuality.MaxRanked( accept ) );

        return Collate( version, otherVersions );
    }
}