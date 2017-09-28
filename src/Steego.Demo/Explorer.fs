module Steego.Demo.Explorer

open System // 
open Suave //
open Steego
open Steego.Reflection.Navigators

open Printer

let explore (level:int) (value:'a) = 
  
    let o = value :> obj
    let navigatedObj = o |> toContext |> NavigateContext "/"
    let tag = navigatedObj|> print level

    let server = SocketServer.getServer(8080)

    tag.ToString() |> server.SendHtml


type System.Object with
    member this.Explore(level:int) = explore level this

