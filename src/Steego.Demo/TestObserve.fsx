#r "../../packages/FSharp.Control.Reactive/lib/net45/FSharp.Control.Reactive.dll"


open Microsoft.FSharp.Control
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders

let watch() = 
    observe {
        try
            for x in 1..10 do
                do! Async.Sleep(1000) |> Observable.ofAsync
                yield x
                printfn "Printing: %i" x
            yield 0
            printfn "Done"
        with exn ->
            printfn "Exception! %A" exn
    }



let d = watch() |> Observable.subscribe(printfn "On Next: %A")

d.Dispose()