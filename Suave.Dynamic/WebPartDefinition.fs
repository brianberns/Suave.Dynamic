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
        /// Web path prefix routed to be routed to the web part.
        WebPath : string

        /// Path to assembly that contains the web part.
        AssemblyPath : string

        /// Full name of type that contains the web part. If no
        /// name is specified, all types in the assembly will be
        /// considered.
        TypeFullNameOpt : Option<string>

        /// Name of static property that contains the web part. If
        /// no name is specified, all static properties in the type
        /// will be considered.
        PropertyNameOpt : Option<string>
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
            PropertyNameOpt =
                table
                    |> TomlTable.tryGetNode "property_name"
                    |> Option.map (fun node ->
                        node.AsString.Value)
        }

    /// Reads dynamic web part definitions from the given TOML file.
    let read tomlPath =
        let node =
            use reader = new StreamReader(tomlPath : string)
            let table = TOML.Parse(reader)
            table["web_part"]
        [|
            for key in node.Keys do
                yield node[key].AsTable |> fromTable
        |]
