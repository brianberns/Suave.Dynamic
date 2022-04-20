namespace TestWebPart2

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

module WebPart =

    let app =
        GET >=>
            choose [
                path "/hello" >=> OK "Hello 2"
                path "/goodbye" >=> OK "Goodbye 2"
            ]
