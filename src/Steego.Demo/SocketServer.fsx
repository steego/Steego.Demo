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

let x() = 
    Suave.Sucessful.OK("Hi")


let myPart = 
    choose [
        GET >=> path "/" >=> OK("Howdy partner")
        //GET >=> request (fun r -> OK("Path: " + r.path))
        GET >=> browseHome
        GET >=> path "/admin" >=> 
            Authentication.authenticateBasic
                (fun (user,pwd) -> user = "foo" && pwd = "test") 
                (OK("Howdy admin"))
    ]

System.IO.Directory.GetCurrentDirectory()

server.UpdateWebPart(myPart)

// server.UpdateWebPart(Successful.OK("Hello"))

// server.SendHtml("Hello <b>World</b>!")

