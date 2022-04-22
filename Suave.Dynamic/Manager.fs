namespace Suave.Dynamic

open System.IO
open System.Reflection

open Suave
open Suave.Filters
open Suave.Operators

open Tommy

module Manager =

    let create tomlPath =
        use reader = new StreamReader(tomlPath : string)
        let table = TOML.Parse(reader)
        let partsNode = table["web_part"]
        choose [
            for key in partsNode.Keys do
                let subtable = partsNode[key].AsTable
                let filePath = subtable["file_path"].AsString.Value
                let webPath = subtable["web_path"].AsString.Value
                let assembly =
                    filePath
                        |> Path.GetFullPath
                        |> Assembly.LoadFile
                let prop =
                    seq {
                        for typ in assembly.GetTypes() do
                            for prop in typ.GetProperties(BindingFlags.Static ||| BindingFlags.Public) do
                                if prop.PropertyType = typeof<WebPart> then
                                    yield prop
                    } |> Seq.exactlyOne
                let part = prop.GetMethod.Invoke(null, Array.empty) :?> WebPart
                yield path webPath >=> part
            yield RequestErrors.NOT_FOUND "Found no handlers."
        ]
