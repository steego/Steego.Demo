module Steego.Demo.SocketServer

open System.Threading.Tasks
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
open System.Windows.Interop


type Id = string

module Log =
    let getDate() = 
        let now = DateTime.Now
        sprintf "%02i:%02i:%02i" (now.Hour) (now.Minute) (now.Second)
    let debug(msg) = printfn "[%s DBG] %s" (getDate()) msg

module Common = 
    open Microsoft.FSharp.Control
    open FSharp.Control.Reactive
    open FSharp.Control.Reactive.Builders
    
    /// Sends text to a websocket
    let private sendText (webSocket : WebSocket) (response : string) = 
        (let byteResponse = 
             response
             |> System.Text.Encoding.ASCII.GetBytes
             |> ByteSegment
         // the `send` function sends a message back to the client
         webSocket.send Text byteResponse true)
    
    let private (|Message|_|) msg = 
        match msg with
        | (Text, data, true) -> Some(System.Text.Encoding.UTF8.GetString(data))
        | _ -> None
    
    type IConnection = 
        abstract Id : string
    
    and Message = 
        { Connection : IConnection
          Message : string }
    
    type private SuaveConnection(context : HttpContext, ws : WebSocket) = 
        let id = Guid.NewGuid().ToString()
        member _this.SendAsync(msg : string) = 
            async { let! _ = msg |> sendText ws
                    () }
        override _this.ToString() = id
        interface IConnection with
            member _this.Id = id
    
    type MessagePayload = string
    
    type ClientEvent = 
        | Connected of IConnection
        | ReceivedMessage of IConnection * MessagePayload
        | Disconnected of IConnection

    type ServerEvent = 
        | MessageSent of IConnection * MessagePayload

    type ClientMessage = {
        Connections: Map<Id,IConnection>
        Connection: IConnection
        Message: string
    }

    let foldEvent(state)(evt:ClientEvent) = 
        let (conns, conn, msg) = state
        match evt with
        | Connected(conn) ->  (conns |> Map.add conn.Id conn, conn, msg)
        | ReceivedMessage(conn, msg) -> (conns |> Map.add conn.Id conn, conn, msg)
        | Disconnected(conn) -> (conns |> Map.remove conn.Id, conn, msg)

    let trackConnections(clientEvents:IEvent<ClientEvent>) = 
        clientEvents
        |> Event.scan(foldEvent) (Map.empty, null)


        // connectionEvents.Publish.Subscribe(fun _ -> ())

        // let rec trackConnections(connections) =
        //     observe {
        //          let! c = clientEvents
        //          match c with
        //          | Connected(conn) -> 
        //             yield! trackConnections(connections |> Map.add conn.Id conn)
        //          | Disconnected(conn) -> 
        //             yield! trackConnections(connections |> Map.remove conn.Id)
        //          | ReceivedMessage(conn, msg) -> 
        //             // yield (connections, conn, msg)
        //             yield { 
        //                 Connections = connections
        //                 Connection = conn
        //                 Message = msg
        //             }
        //     }
        // trackConnections(Map.empty)

    let dispatchServerEvent = function
        | MessageSent(conn, message) -> 
            let myConn = conn :?> SuaveConnection
            myConn.SendAsync(message) |> Async.Start

    type WebHandler = IObservable<ClientMessage> -> IObservable<ServerEvent>

    type Server(defaultConfig : SuaveConfig, stream: WebHandler) = 
        let mutable started = false
        let clientEvent = new Event<ClientEvent>()
        let clientEventPublished = clientEvent.Publish |> trackConnections

        let serverEvent = clientEventPublished |> stream

        do serverEvent.Add(dispatchServerEvent)

        let socketHandler (ws : WebSocket) (context : HttpContext) = 
            socket { 
                let mutable loop = true
                let conn = SuaveConnection(context, ws)

                clientEvent.Trigger(Connected(conn))
                
                while loop do
                    let! msg = ws.read()
                    match msg with
                    | Message(str) -> 
                        clientEvent.Trigger(ReceivedMessage(conn, str))
                    | (Close, _, _) -> 
                        clientEvent.Trigger(Disconnected(conn))
                        loop <- false
                    | _ -> ()
            }

        let getApp(app, socketHandler) = 
            choose [ path "/websocket" >=> handShake socketHandler
                     GET >=> app
                     NOT_FOUND "Found no handlers." ]

        let mutable app = getApp((GET >=> file "index.html"), socketHandler)
        
        let start() = 
            if not started then
                lock clientEvent (fun () -> started <- true)
                let config = { defaultConfig with
                                    logger = Targets.create Verbose [||]
                             }
                let app (ctx : HttpContext) = async { 
                    return! (lock clientEvent (fun () -> app)) ctx
                }
                Async.Start(async { startWebServer config app })
                Log.debug "Web server started"
        
        member this.Start() = 
            start()
            this
        member _this.ClientEvents = clientEventPublished
        member this.SetHandler(newApp) = 
            app <- getApp(newApp, socketHandler)
            this

let private servers = ConcurrentDictionary<int, Common.Server>()

let private createServer (port : int, handler: Common.WebHandler) = 
    let config = { defaultConfig with 
                    bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" port ]
                 }
    let server = Common.Server(config, handler)
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
let getServer (port, handler) = 
    servers.GetOrAdd(port, fun port -> createServer (port, handler))

let startServer(port, handler) = 
    let server = getServer(port, handler)
    server.Start()
    server
        