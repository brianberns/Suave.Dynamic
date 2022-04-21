namespace Suave.Dynamic

open System.IO
open System.Reflection

open Suave
open Tommy

module Manager =

    let create path =
        use reader = new StreamReader(path : string)
        let table = TOML.Parse(reader)
        let parts = table["web_part"]
        for key in parts.Keys do
            let subtable = parts[key].AsTable
            let filePath = subtable["file_path"].AsString.Value
            let webPath = subtable["web_path"].AsString.Value
            let assembly =
                filePath
                    |> Path.GetFullPath
                    |> Assembly.LoadFile
            for typ in assembly.GetTypes() do
                printfn "%O" typ
                for prop in typ.GetProperties(BindingFlags.Static ||| BindingFlags.Public) do
                    printfn "   %s: %O" prop.Name (prop.PropertyType = typeof<WebPart>)
        
        Successful.OK "Hello World!"
