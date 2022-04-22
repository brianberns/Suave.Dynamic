namespace Suave.Dynamic

open System.IO
open System.Net
open System.Reflection

open Suave
open Suave.Filters
open Suave.Operators

open Tommy

module Manager =

    let private loadAssembly (table : TomlTable) =
        let filePath = table["file_path"].AsString.Value
        let webPath = table["web_path"].AsString.Value
        let assembly =
            filePath
                |> Path.GetFullPath
                |> Assembly.LoadFile
        webPath, assembly

    let private createWebPart (assembly : Assembly) =
        let prop =
            seq {
                for typ in assembly.GetTypes() do
                    for prop in typ.GetProperties(BindingFlags.Static ||| BindingFlags.Public) do
                        if prop.PropertyType = typeof<WebPart> then
                            yield prop
            } |> Seq.exactlyOne
        prop.GetMethod.Invoke(null, Array.empty) :?> WebPart

    let private trimPath (webPath : string) (ctx : HttpContext) =
        let rawPath =
            ctx.request.path.Substring(webPath.Length)
                |> String.split('/')
                |> Seq.map WebUtility.UrlEncode
                |> String.concat "/"
        let req =
            { ctx.request with rawPath = rawPath }
        { ctx with request = req }

    let private wrapPart webPath (webPart : WebPart) : WebPart =
        fun ctx ->
            async {
                    // invoke inner part with trimmed path
                let! ctxOpt =
                    ctx
                        |> trimPath webPath
                        |> webPart

                    // restore original request
                return ctxOpt
                    |> Option.map (fun ctx' ->
                        { ctx' with request = ctx.request })
            }

    let create tomlPath =

            // find dynamic web part configs
        use reader = new StreamReader(tomlPath : string)
        let table = TOML.Parse(reader)
        let partsNode = table["web_part"]

        choose [
            for key in partsNode.Keys do

                    // load assembly
                let webPath, assembly =
                    partsNode[key].AsTable
                        |> loadAssembly

                    // create inner part
                let innerPart = createWebPart assembly

                    // wrap inner part
                yield pathStarts webPath
                    >=> wrapPart webPath innerPart

            yield RequestErrors.NOT_FOUND "Found no handlers."
        ]
