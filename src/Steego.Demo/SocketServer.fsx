/////////////////////////////////////////////////////////////////////
//                             IMPORTS                             //
/////////////////////////////////////////////////////////////////////

#if INTERACTIVE
#r "../Steego.Demo/bin/Release/Fasterflect.dll"
#r "../Steego.Demo/bin/Release/Suave.dll"
#r "../../packages/FSharp.Control.Reactive/lib/net45/FSharp.Control.Reactive.dll"

#load "TypeInfo.fs"
#load "TypePatterns.fs"
#load "Navigators.fs"
#load "HTML.fs"
#load "Printer.fs"
#load "Reflection.fs"
#load "SocketServer.fs"
#endif

/////////////////////////////////////////////////////////////////////
//                             IMPORTS                             //
/////////////////////////////////////////////////////////////////////

open System
open Steego.Demo
open FSharp.Control.Reactive
open Steego.Demo.SocketServer.Common
open FSharp.Control.Reactive.Builders

let rec getServer() = 
    SocketServer.startServer(8080, handler)
and handler(events) = 
    observe {
        let! e = events
        let output = sprintf "%A" (e)
        yield MessageSent(e.Connection, output)
    }

open Suave
open Suave.Operators
open Suave.Filters
open Suave.RequestErrors

let app = 
    choose [ GET >=> Files.file "src/Steego.Demo/index.html"
             NOT_FOUND "Found no handlers." ]

let server = getServer().SetHandler(app).Start()

server.ClientEvents
    :> IObservable<_>
    |> Observable.subscribe(printfn "On Msg: %A")