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

let server = SocketServer.getServer(8080)

open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Files
open Suave.RequestErrors
open Suave.Logging
open Suave.Utils


printfn "Press anything to end"

let printMsg(m:Common.Message) = 
    printfn "Sender: %s - %s" (m.Connection.Id) (m.Message)
    ()

server.OnReceived.Subscribe printMsg

async {
    for i in 1..10000 do
        do! Async.Sleep(200)
        let msg = sprintf "Counting %i" i
        //printfn "%s" msg
        server.SendHtml(msg)
} |> Async.RunSynchronously

printfn "Exited"