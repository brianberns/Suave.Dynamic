# Suave.Dynamic

Suave.Dynamic is a [Suave](https://suave.io) web part that can load other web parts dynamically and then route requests to them. This allows a single Suave web server to host multiple independent apps, each of which acts as the root of its own virtual directory.

## Example

Let's build a simple web part that can say "hello" and "goodbye":

```fsharp
module WebPart1

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

let app =
    GET >=>
        choose [
            path "/hello" >=> OK $"Hello 1"
            path "/goodbye" >=> OK $"Goodbye 1"
        ]
```

In another project, we can have a second, independent web part that behaves slightly differently:


```fsharp
module WebPart2

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

let app =
    GET >=>
        choose [
            path "/hello" >=> OK $"Hello 2"
            path "/goodbye" >=> OK $"Goodbye 2"
        ]
```

Now we need a basic Suave web server that hosts the web parts:


```fsharp
open Suave
open Suave.Filters
open Suave.Logging
open Suave.Operators

let app =
    let logger = Targets.create LogLevel.Info [||]
    choose [
        Dynamic.WebPart.fromToml "WebParts.toml"
        RequestErrors.NOT_FOUND "Found no handlers."
    ] >=> logWithLevelStructured
        LogLevel.Info
        logger
        logFormatStructured

startWebServer defaultConfig app
```

The key line is:

```fsharp
Dynamic.WebPart.fromToml "WebParts.toml"
```

This loads the dynamic web parts using the information in a [TOML](https://toml.io/en/) configuration file:

```toml
[web_part.one]
assembly_path = '..\..\..\..\TestWebPart1\bin\Debug\net6.0\TestWebPart1.dll'
web_path = "/one"

[web_part.two]
assembly_path = '..\..\..\..\TestWebPart2\bin\Debug\net6.0\TestWebPart2.dll'
web_path = "/two"
type_full_name = "WebPart2"
property_name = "app"
```

The configuration file tells Suave.Dynamic where to find the dynamic web parts:

* `assembly_path`: File path of assembly that contains the dynamic web part
* `web_path`: Name of the virtual directory that will route to the dynamic web part
* `type_full_name` (optional): Name of the type (or F# module) that contains the dynamic web part
* `property_name` (optional): Name of the type's static property that contains the dynamic web part

If `type_full_name` or `property_name` are omitted, Suave.Dynamic will search the assembly for a type that contains a static property of type `WebPart`.

We can then start the web server and browse to a URL such as http://localhost:8080/one/hello. The response is "Hello 1", as expected. Note, however, that the request that `WebPart1` sees is just `/hello`, rather than `/one/hello`. This allows `WebPart2` to be loaded as well, and respond to requests at `/two/hello`, without any conflict between the two web parts.

# Building a dynamic web part

Dynamic web parts must be built carefully, so they can be successfully loaded at runtime. This requires the `.fsproj` file to contain the following settings:

* Set `EnableDynamicLoading` to `true`. This copies all of the project's dependencies to its build directory.
* Set `ExcludeAssets` to `runtime` for both Suave and FSharp.Core. This prevents those particular assemblies from being copied to the build directory, and being loaded incompatibly by `Suave.Dynamic`.

A typical `.fsproj` file for a dynamic web part will then look something like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <SatelliteResourceLanguages>en-US</SatelliteResourceLanguages>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="WebPart.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="MathNet.Numerics.FSharp" Version="5.0.0" />
    <PackageReference Include="Suave" Version="2.6.2">
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
    <PackageReference Update="FSharp.Core">
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

See the article [Create a .NET Core application with plugins](https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support) for details.