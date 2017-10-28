open System

let doEvent() =
    let event = Event<_>()

    Async.Start <| async {
        for x in 1..100 do
            do! Async.Sleep(100)
            event.Trigger(x)
    }

    event.Publish

let evt = doEvent()
evt |> Event.add(printfn "%A")