// Copyright (c) .NET Foundation and contributors. All rights reserved.

#pragma warning disable SA1649

using Asp.Versioning;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer( typeof( FluentAssertionsLicense ), nameof( FluentAssertionsLicense.Accept ) )]

namespace Asp.Versioning;

public static class FluentAssertionsLicense
{
    public static void Accept() => FluentAssertions.License.Accepted = true;
}