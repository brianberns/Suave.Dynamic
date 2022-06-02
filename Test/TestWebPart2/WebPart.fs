namespace TestWebPart2

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open Tommy

open MathNet.Numerics

module WebPart =

    let createApp (tomlTable : TomlTable) =
        let two = BigRational.FromInt 2
        let extra = tomlTable["extra"].AsString
        GET >=>
            choose [
                path "/" >=> OK $"Root {two}"
                path "/hello" >=> OK $"Hello {two} {extra}"
                path "/goodbye" >=> OK $"Goodbye {two} {extra}"
            ]
