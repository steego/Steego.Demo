module Steego.Demo.SocketServer

open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Files
open Suave.RequestErrors
open Suave.Logging
open Suave.Utils
open System
open System.Net
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open System.Collections.Concurrent

type Id = string

module Common = 
    /// Sends text to a websocket
    let sendText (webSocket : WebSocket) (response : string) = 
        (let byteResponse = 
             response
             |> System.Text.Encoding.ASCII.GetBytes
             |> ByteSegment
         // the `send` function sends a message back to the client
         webSocket.send Text byteResponse true)
    
    let (|Message|_|) msg = 
        match msg with
        | (Text, data, true) -> Some(System.Text.Encoding.UTF8.GetString(data))
        | _ -> None

    type Connection(context : HttpContext, ws : WebSocket) = 
        let id = Guid.NewGuid().ToString()
        let onReceive = new Event<Message>()
        member this.Id = id
        member this.TellReceived(msg) = onReceive.Trigger(msg)
        member this.OnReceive = onReceive.Publish
        member this.SendAsync(msg : string) = async { let! _ = msg |> sendText ws
                                                      return true }
    and Message = { Connection: Connection; Message: string }

    type Server(defaultConfig : SuaveConfig) = 
        let mutable started = false
        let connections = ConcurrentDictionary<string, Connection>()
        let onReceive = new Event<Message>()
        let getConn(id:string) = 
            let (exists, conn) = connections.TryGetValue(id)
            conn
        
        let socketHandler (ws : WebSocket) (context : HttpContext) = 
            socket { 
                let mutable loop = true
                let conn = Connection(context, ws)
                let id = conn.Id
                connections.AddOrUpdate(id, conn, fun x y -> conn) |> ignore
                while loop do
                    let! msg = ws.read()
                    match msg with
                    | Message(str) -> 
                        onReceive.Trigger( { Connection = getConn(id); Message = str })
                    | (Close, _, _) -> 
                        let _ = connections.TryRemove(id)
                        loop <- false
                    | _ -> ()
            }
        
        let mutable app = 
            choose [ path "/websocket" >=> handShake socketHandler
                     GET >=> Files.file "index.html"
                     NOT_FOUND "Found no handlers." ]
        
        let start() = 
            if not started then 
                lock connections (fun () -> started <- true)
                let config = { defaultConfig with logger = Targets.create Verbose [||] }
                let app (ctx : HttpContext) = async { return! (lock connections (fun () -> app)) ctx }
                Async.Start(async { startWebServer config app })
                printfn "Web server started"
        
        member this.OnReceived = onReceive.Publish
        member this.SendHtml(html : string) = 
            start()
            async { 
                for c in connections do
                    let conn = c.Value
                    let! _ = conn.SendAsync(html)
                    ()
            }
            |> Async.Start

let private servers = System.Collections.Concurrent.ConcurrentDictionary<int, Common.Server>()

let private createServer (port : int) = 
    let config = { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" port ] }
    let server = Common.Server(config)
    server

///**Creates or fetches and instances of a web server at the specified port**
///
///**Parameters**
///  * `port` - parameter of type `int`
///
///**Output Type**
///  * `Common.Server`
///
///**Exceptions**
///
let getServer (port) = servers.GetOrAdd(port, fun port -> createServer (port))