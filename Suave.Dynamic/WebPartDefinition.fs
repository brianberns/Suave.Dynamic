namespace Suave.Dynamic

open System.IO
open Tommy

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
    }

module WebPartDefinition =

    /// Extracts a dynamic web part definition from the given table.
    let fromTable (table : TomlTable) =
        {
            WebPath = table["web_path"].AsString.Value
            AssemblyPath = table["assembly_path"].AsString.Value
            TypeFullNameOpt =
                match table.TryGetNode("type_full_name") with
                    | true, node -> Some node.AsString.Value
                    | _ -> None
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
