# LiteHTMLSharp Development Guide

## Local Development Setup

When developing with LiteHTMLSharp using `ProjectReference` (rather than consuming published NuGet packages), you need to manually copy native libraries to your output directory.

### Quick Setup

Add this to your application's `.csproj` file:

```xml
<!-- Copy native libraries for local development -->
<ItemGroup>
  <Content Include="path/to/LiteHTMLSharp/runtimes/**/*.dll;path/to/LiteHTMLSharp/runtimes/**/*.dylib">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>runtimes/%(RecursiveDir)%(Filename)%(Extension)</Link>
    <TargetPath>runtimes/%(RecursiveDir)%(Filename)%(Extension)</TargetPath>
  </Content>
</ItemGroup>
```

Replace `path/to/LiteHTMLSharp` with the actual path to your LiteHTMLSharp clone.

### Example

If your solution structure is:
```
MySolution/
  LiteHTMLSharp/          (cloned repo)
    runtimes/
      osx-arm64/
      osx-x64/
      win-x64/
      win-x86/
  MyApp/
    MyApp.csproj
```

Then in `MyApp.csproj`:
```xml
<ItemGroup>
  <Content Include="$(MSBuildThisFileDirectory)../LiteHTMLSharp/runtimes/**/*.dll;$(MSBuildThisFileDirectory)../LiteHTMLSharp/runtimes/**/*.dylib">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>runtimes/%(RecursiveDir)%(Filename)%(Extension)</Link>
    <TargetPath>runtimes/%(RecursiveDir)%(Filename)%(Extension)</TargetPath>
  </Content>
</ItemGroup>
```

### macOS App Bundle Exception

For macOS app bundles (projects with `<UseMacOSAppBundle>true</UseMacOSAppBundle>`), use `NativeReference` instead of `Content`:

```xml
<ItemGroup>
  <NativeReference Include="$(MSBuildThisFileDirectory)../runtimes/osx-arm64/native/liblitehtml.dylib" Condition="'$(RuntimeIdentifier)' == 'osx-arm64' OR '$(RuntimeIdentifier)' == ''">
    <Kind>Dynamic</Kind>
    <ForceLoad>False</ForceLoad>
  </NativeReference>
  <NativeReference Include="$(MSBuildThisFileDirectory)../runtimes/osx-x64/native/liblitehtml.dylib" Condition="'$(RuntimeIdentifier)' == 'osx-x64'">
    <Kind>Dynamic</Kind>
    <ForceLoad>False</ForceLoad>
  </NativeReference>
</ItemGroup>
```

This ensures native libraries are placed in the `MonoBundle/` folder where .NET can find them in app bundles.

## Why Is This Necessary?

Native libraries need to be in specific locations for .NET's runtime to find them. When using published NuGet packages, this happens automatically. For local development with `ProjectReference`, you need to manually configure the copy.

## Published Package Consumption

If you're consuming published NuGet packages from NuGet.org, no additional setup is required - native libraries are included automatically.

```xml
<PackageReference Include="LiteHtmlSharp.Avalonia" Version="2.0.x" />
```

## Native Library Locations

Native libraries are stored in:
```
runtimes/
  win-x64/native/LiteHtmlLib.dll
  win-x86/native/LiteHtmlLib.dll
  osx-x64/native/liblitehtml.dylib
  osx-arm64/native/liblitehtml.dylib
```

.NET automatically searches these locations at runtime based on your platform's Runtime Identifier (RID).
