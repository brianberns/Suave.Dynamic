open Suave
open Suave.Filters
open Suave.Logging
open Suave.Operators

let app =
    let logger = Targets.create LogLevel.Info [||]
    choose [
        Dynamic.WebPart.create "WebParts.toml"
        RequestErrors.NOT_FOUND "Found no handlers."
    ] >=> logWithLevelStructured
        LogLevel.Info
        logger
        logFormatStructured

startWebServer defaultConfig app
