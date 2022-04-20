open Suave
open Suave.Dynamic

let app = Manager.create "WebParts.toml"
startWebServer defaultConfig app
