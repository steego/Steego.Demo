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
#load "Explorer.fs"
#endif


open System.IO
open Steego.Demo.Explorer
open Steego.Printer

type Dir(path:string) = 
    let dirInfo = DirectoryInfo(path)
    member this.Name = dirInfo.FullName
    member this.CreationTime = dirInfo.CreationTime
    member this.Attributes = dirInfo.Attributes
    member this.SubFolders = 
        Directory.EnumerateDirectories(path) |> Seq.map Dir
    member this.Files = 
        Directory.EnumerateFiles(path)

type App(root:string) = 
    member this.Name = "My super app"
    member this.Folders = 
        System.IO.Directory.EnumerateDirectories(root)
        |> Seq.map (fun d -> Dir(d))
        |> Seq.toArray

let fsiPrinter(o) =
    explore 1 o
    sprintf "%A" o

#if __INTERACTIVE__
fsi.AddPrinter(fsiPrinter)
#endif

let app = App(@"C:\Projects")