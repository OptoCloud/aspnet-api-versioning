// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning.OpenApi;

using Asp.Versioning.ApiExplorer;

public class OpenApiDocumentTitleOptionsTest
{
    [Fact]
    public void format_should_return_default_title()
    {
        // arrange
        var options = new OpenApiDocumentTitleOptions();
        var api = new ApiVersionDescription( new ApiVersion( 1.0 ), "v1" );

        // act
        var actual = options.Format( "Contoso API", api );

        // assert
        actual.Should().Be( "Contoso API | v1" );
    }

    [Fact]
    public void format_should_return_custom_title()
    {
        // arrange
        var options = new OpenApiDocumentTitleOptions()
        {
            Format = ( title, api ) => $"{title} ({api.ApiVersion})",
        };
        var api = new ApiVersionDescription( new ApiVersion( 1.0 ), "v1" );

        // act
        var actual = options.Format( "Contoso API", api );

        // assert
        actual.Should().Be( "Contoso API (1.0)" );
    }
}