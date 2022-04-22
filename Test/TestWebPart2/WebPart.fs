namespace TestWebPart2

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open MathNet.Numerics

module WebPart =

    let app =
        let two = BigRational.FromInt 2
        GET >=>
            choose [
                path "/hello" >=> OK $"Hello {two}"
                path "/goodbye" >=> OK $"Goodbye {two}"
            ]
