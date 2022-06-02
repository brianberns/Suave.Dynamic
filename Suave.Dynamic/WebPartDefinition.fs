namespace Suave.Dynamic

open System.IO
open Tommy

module TomlTable =

    /// Tries to get the node with the given key from the given
    /// TOML table.
    let tryGetNode key (table : TomlTable) =
        match table.TryGetNode(key) with
            | true, node -> Some node
            | _ -> None

/// Definition of a dynamic web part.
type WebPartDefinition =
    {
        /// Web path prefix to be routed to the web part.
        /// E.g. "/MyWebPart".
        WebPath : string

        /// Path to assembly that contains the web part. If this is
        /// a relative path, it will be resolved using the current
        /// directory, which might not be the same as the directory
        /// that contains the web server's binaries.
        AssemblyPath : string

        /// Full name of type that contains the web part. If no
        /// name is specified, all types in the assembly will be
        /// considered.
        TypeFullNameOpt : Option<string>

        /// Name of static member that contains the web part. If
        /// no name is specified, all static members in the type
        /// will be considered.
        MemberNameOpt : Option<string>
    }

module WebPartDefinition =

    /// Extracts a dynamic web part definition from the given table.
    let fromTable (table : TomlTable) =
        {
            WebPath = table["web_path"].AsString.Value
            AssemblyPath = table["assembly_path"].AsString.Value
            TypeFullNameOpt =
                table
                    |> TomlTable.tryGetNode "type_full_name"
                    |> Option.map (fun node ->
                        node.AsString.Value)
            MemberNameOpt =
                table
                    |> TomlTable.tryGetNode "member_name"
                    |> Option.map (fun node ->
                        node.AsString.Value)
        }

    /// Reads dynamic web part definitions from the given TOML file.
    let getWebPartDefTables tomlPath =
        let node =
            use reader = new StreamReader(tomlPath : string)
            let table = TOML.Parse(reader)
            table["web_part"]
        [|
            for key in node.Keys do
                yield node[key].AsTable
        |]
