namespace Suave.Dynamic

open System
open System.IO
open System.Net
open System.Reflection

open Suave
open Suave.Filters
open Suave.Operators

module WebPart =

    /// Creates a dynamic web part by invoking the given assembly.
    let private createWebPart webPartDef =

            // load assembly
        let assembly =
            webPartDef.AssemblyPath
                |> Path.GetFullPath
                |> Assembly.LoadFile

            // extract candidate types from assembly
        let types =
            match webPartDef.TypeFullNameOpt with
                | Some fullName ->
                    [| assembly.GetType(fullName, true) |]
                | None -> assembly.GetTypes()

            // extract candidate property(ies) from types
        let properties =
            let mapping =
                match webPartDef.PropertyNameOpt with
                    | Some name ->
                        fun (typ : Type) ->
                            typ.GetProperty(name, typeof<WebPart>)
                                |> Seq.singleton
                    | None ->
                        fun (typ : Type) ->
                            typ.GetProperties(
                                BindingFlags.Static ||| BindingFlags.Public)
                                |> Seq.where (fun prop ->
                                    prop.PropertyType = typeof<WebPart>)
            types
                |> Seq.collect mapping
                |> Seq.toArray

            // choose final property
        let property =
            match properties.Length with
                | 1 -> properties |> Array.exactlyOne
                | 0 -> failwith $"No candidate properties found in {webPartDef.AssemblyPath}"
                | _ -> failwith $"Multiple candidate properties found in {webPartDef.AssemblyPath}: {properties}"

            // create web part
        property.GetMethod.Invoke(null, Array.empty)
            :?> WebPart

    /// Removes the given web path prefix from the start of the given
    /// context's path.
    let private trimPath (pathPrefix : string) (ctx : HttpContext) =

            // trim path
        let trimmedPath = ctx.request.path.Substring(pathPrefix.Length)

            // re-encode trimmed path
        let rawPath =
            trimmedPath
                |> String.split('/')
                |> Seq.map WebUtility.UrlEncode
                |> String.concat "/"

            // construct modified context
        let trimmedCtx =
            { ctx with
                request =
                    { ctx.request with
                        rawPath = rawPath } }
        assert(ctx.request.path = pathPrefix + trimmedCtx.request.path)
        trimmedCtx

    /// Wraps the given web part for invocation with a trimmed path.
    let private wrapWebPart pathPrefix (webPart : WebPart) : WebPart =
        fun ctx ->
            async {
                    // invoke inner part with trimmed path
                let! ctxOpt =
                    ctx
                        |> trimPath pathPrefix
                        |> webPart

                    // restore original request
                return ctxOpt
                    |> Option.map (fun ctx' ->
                        { ctx' with request = ctx.request })
            }

    /// Creates a dynamic web part from the given definitions.
    let fromDefinitions webPartDefs =
        choose [
            for webPartDef in webPartDefs do

                    // create dynamic web part
                let webPart = createWebPart webPartDef

                    // wrap dynamic part
                let webPath = webPartDef.WebPath
                yield pathStarts webPath
                    >=> wrapWebPart webPath webPart
        ]

    /// Creates a dynamic web part from the given TOML file.
    let fromToml tomlPath =
        tomlPath
            |> WebPartDefinition.read
            |> fromDefinitions
