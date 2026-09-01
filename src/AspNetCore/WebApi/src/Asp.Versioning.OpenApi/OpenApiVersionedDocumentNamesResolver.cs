// Copyright (c) .NET Foundation and contributors. All rights reserved.

#pragma warning disable IDE0079
#pragma warning disable CA1812

namespace Asp.Versioning.OpenApi;

using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiVersionedDocumentNamesResolver( IApiVersionDescriptionProvider provider )
    : IAdditionalOpenApiDocumentNameResolver
{
    public IEnumerable<string> ResolveDocumentNames() => provider.ApiVersionDescriptions.Select( d => d.GroupName );
}