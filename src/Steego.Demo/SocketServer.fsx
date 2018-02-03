/////////////////////////////////////////////////////////////////////
//                             IMPORTS                             //
/////////////////////////////////////////////////////////////////////

#if INTERACTIVE
#r "../Steego.Demo/bin/Release/Fasterflect.dll"
#r "../Steego.Demo/bin/Release/Suave.dll"

#load "TypeInfo.fs"
#load "TypePatterns.fs"
#load "Navigators.fs"
#load "HTML.fs"
#load "Printer.fs"
#load "Reflection.fs"
#load "SocketServer.fs"
#endif

open Steego.Demo
open Steego.Demo.SocketServer

let server = SocketServer.startServer(8080)

// open Suave
// open Suave.Http
// open Suave.Operators
// open Suave.Filters
// open Suave.Successful
// open Suave.Files
// open Suave.RequestErrors
// open Suave.Logging
// open Suave.Utils

open Steego.Printer

/////////////////////////////////////////////////////////////////////
//                             IMPORTS                             //
/////////////////////////////////////////////////////////////////////

Log.debug "Press anything to end"

let printMsg(m:Common.Message) = 
    Log.debug <| sprintf "Sender: %s - %s" (m.Connection.Id) (m.Message)

server.OnReceived.Subscribe printMsg

// Async.RunSynchronously <| async {
//     for i in 1..10000 do
//         do! Async.Sleep(200)
//         let conns = server.Connections
//         for c in conns do
//             let msg = Info(c.Id, i, server) |> print 3
//             do! c.SendAsync(msg) |> Async.AwaitTask
// }

let fsiPrinter(o) =
    Async.RunSynchronously <| async {
        let conns = server.Connections
        for c in conns do
            let msg = o |> print 3
            do! c.SendAsync(msg) |> Async.AwaitTask
    }
    sprintf "%A" o

#if INTERACTIVE
fsi.AddPrinter(fsiPrinter)
#endif

// type Info(id:string, count:int, server:Common.Server) = 
//     member this.Id = id
//     member this.Count = server.Connections.Length
//     member this.Connections = server.Connections |> Array.map (fun c -> c.Id)

[ for c in server.Connections -> c.Id ]

Log.debug "Exited"