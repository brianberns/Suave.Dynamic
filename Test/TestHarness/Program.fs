open Suave
open Suave.Dynamic
open Suave.Filters
open Suave.Logging
open Suave.Operators

let app =
    let logger = Targets.create LogLevel.Info [||]
    Manager.create "WebParts.toml"
        >=> logWithLevelStructured
            LogLevel.Info
            logger
            logFormatStructured

startWebServer defaultConfig app
