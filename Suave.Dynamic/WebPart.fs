namespace Suave.Dynamic

open System.IO
open System.Net
open System.Reflection

open Suave
open Suave.Filters
open Suave.Operators

open Tommy

/// Definition of a dynamic web part.
type WebPartDefinition =
    {
        /// Web path prefix routed to be routed to the web part.
        WebPath : string

        /// Path to assembly that contains the web part.
        AssemblyPath : string
    }

module WebPartDefinition =

    /// Extracts a dynamic web part definition from the given table.
    let fromTable (table : TomlTable) =
        {
            WebPath = table["web_path"].AsString.Value
            AssemblyPath = table["assembly_path"].AsString.Value
        }

module WebPart =

    /// Creates a dynamic web part by invoking the given assembly.
    let private createWebPart assemblyPath =

            // load assembly
        let assembly =
            assemblyPath
                |> Path.GetFullPath
                |> Assembly.LoadFile

            // create web part
        let prop =
            seq {
                for typ in assembly.GetTypes() do
                    let properties =
                        typ.GetProperties(BindingFlags.Static ||| BindingFlags.Public)
                    for prop in properties do
                        if prop.PropertyType = typeof<WebPart> then
                            yield prop
            } |> Seq.exactlyOne
        prop.GetMethod.Invoke(null, Array.empty) :?> WebPart

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

    /// Creates a dynamic web part.
    let create tomlPath =

            // find dynamic web part configs
        let partsNode =
            use reader = new StreamReader(tomlPath : string)
            let table = TOML.Parse(reader)
            table["web_part"]

        choose [
            for key in partsNode.Keys do

                    // create dynamic web part
                let webPartDef =
                    partsNode[key].AsTable
                        |> WebPartDefinition.fromTable
                let webPart = createWebPart webPartDef.AssemblyPath

                    // wrap inner part
                let webPath = webPartDef.WebPath
                yield pathStarts webPath
                    >=> wrapWebPart webPath webPart
        ]
