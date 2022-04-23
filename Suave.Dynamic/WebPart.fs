namespace Suave.Dynamic

open System
open System.Net
open System.Reflection

open Suave
open Suave.Filters
open Suave.Operators

open Tommy

module Option =

    /// Converts an option to a sequence of length 0 or 1.
    let toSeq option =
        option
            |> Option.map Seq.singleton
            |> Option.defaultValue Seq.empty

module String =

    /// Concatenates the given strings.
    let join (separator : string) (strings : seq<string>) =
        String.Join(separator, strings)

module WebPart =

    /// Extracts candidate types.
    let private getCandidateTypes webPartDef =
        let assembly =
            PluginLoadContext.load webPartDef.AssemblyPath
        match webPartDef.TypeFullNameOpt with
            | Some fullName ->
                [| assembly.GetType(fullName, true) |]
            | None -> assembly.GetTypes()

    /// Binding flags for candidate members.
    let private bindingFlags =
        BindingFlags.Static ||| BindingFlags.Public

    /// Extracts candidate properites from the given types.
    let private getCandidateProperties webPartDef types =
        let mapping =
            match webPartDef.MemberNameOpt with
                | Some name ->
                    fun (typ : Type) ->
                        typ.GetProperty(name, typeof<WebPart>)
                            |> Option.ofObj
                            |> Option.toSeq
                | None ->
                    fun (typ : Type) ->
                        typ.GetProperties(bindingFlags)
                            |> Seq.where (fun prop ->
                                prop.PropertyType = typeof<WebPart>)
        types
            |> Seq.collect mapping
            |> Seq.toArray

    /// Extracts candidate methods from the given types.
    let private getCandidateMethods webPartDef types =
        let mapping =
            match webPartDef.MemberNameOpt with
                | Some name ->
                    fun (typ : Type) ->
                        typ.GetMethod(name, bindingFlags, [| typeof<TomlTable> |])
                            |> Option.ofObj
                            |> Option.toSeq
                | None ->
                    fun (typ : Type) ->
                        typ.GetMethods(bindingFlags)
                            |> Seq.where (fun meth ->
                                meth.GetParameters()
                                    |> Seq.tryExactlyOne
                                    |> Option.map (fun param ->
                                        param.ParameterType = typeof<TomlTable>)
                                    |> Option.defaultValue false
                                    && meth.ReturnType = typeof<WebPart>)
        types
            |> Seq.collect mapping
            |> Seq.toArray

    /// Creates a dynamic web part by invoking the given assembly.
    let private createWebPart webPartDef tomlTableOpt =

            // extract candidate members
        let properties, methods =
            let types = getCandidateTypes webPartDef
            let properties = getCandidateProperties webPartDef types
            let methods =
                if tomlTableOpt |> Option.isSome then
                    getCandidateMethods webPartDef types
                else Array.empty
            properties, methods

            // choose and invoke one member
        match properties.Length, methods.Length, tomlTableOpt with
            | 1, 0, _ ->
                let property =
                    properties |> Array.exactlyOne
                property.GetMethod.Invoke(null, Array.empty)
                    :?> WebPart
            | 0, 1, Some tomlTable ->
                let method =
                    methods |> Array.exactlyOne
                method.Invoke(null, [| tomlTable |])
                    :?> WebPart
            | 0, 1, None -> failwith "Unexpected"
            | 0, 0, _ ->
                failwith $"No candidate members found in {webPartDef.AssemblyPath}"
            | _ ->
                let memberNames =
                    seq {
                        for property in properties do
                            yield $"Property {property.Name}"
                        for method in methods do
                            yield $"Method {method.Name}"
                    } |> String.join ", "
                failwith $"Multiple candidate members found in {webPartDef.AssemblyPath}: {memberNames}"

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
    let private fromDefinitionPairs webPartDefPairs =
        choose [
            for webPartDef, tomlTableOpt in webPartDefPairs do

                    // create dynamic web part
                let webPart = createWebPart webPartDef tomlTableOpt

                    // wrap dynamic part
                let webPath = webPartDef.WebPath
                yield pathStarts webPath
                    >=> wrapWebPart webPath webPart
        ]

    /// Creates a dynamic web part from the given definitions.
    let fromDefinitions webPartDefs =
        webPartDefs
            |> Array.map (fun webPartDef ->
                webPartDef, None)
            |> fromDefinitionPairs

    /// Creates a dynamic web part from the given TOML file.
    let fromToml tomlPath =
        let tables =
            WebPartDefinition.getWebPartDefTables tomlPath
        fromDefinitionPairs [|
            for table in tables do
                let webPartDef = WebPartDefinition.fromTable table
                yield webPartDef, Some table
        |]
