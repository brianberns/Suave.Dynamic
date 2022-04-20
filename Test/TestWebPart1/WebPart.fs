namespace TestWebPart1

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

module WebPart =

    let app =
        GET >=>
            choose [
                path "/hello" >=> OK "Hello 1"
                path "/goodbye" >=> OK "Goodbye 1"
            ]
