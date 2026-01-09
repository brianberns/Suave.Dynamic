open Suave
open Suave.Operators

open Serilog

let logRequest (logger : ILogger) : WebPart =
    fun ctx ->
        async {
            logger.Information(
                "{Method} {Path}", ctx.request.method, ctx.request.path)
            return Some ctx
        }

try

    use logger =
        LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger()

    let app =
        logRequest logger
            >=> choose [
                Dynamic.WebPart.fromToml "WebParts.toml"
                RequestErrors.NOT_FOUND "Found no handlers."
            ]

    startWebServer defaultConfig app

with ex ->
    printfn $"{ex.Message}"
