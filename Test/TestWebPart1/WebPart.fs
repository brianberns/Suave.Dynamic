namespace TestWebPart1

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open MathNet.Numerics

module WebPart =

    let app =
        let one = BigRational.FromInt 1
        GET >=>
            choose [
                path "/" >=> OK $"Root {one}"
                path "/hello" >=> OK $"Hello {one}"
                path "/goodbye" >=> OK $"Goodbye {one}"
            ]
