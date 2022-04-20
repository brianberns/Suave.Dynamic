namespace Suave.Dynamic

open System.IO
open Suave
open Tommy

module Manager =

    let create path =
        use reader = new StreamReader(path : string)
        let table = TOML.Parse(reader)
        let parts = table["web_part"]
        for key in parts.Keys do
            printfn "%O" (key, parts[key])
        
        Successful.OK "Hello World!"
