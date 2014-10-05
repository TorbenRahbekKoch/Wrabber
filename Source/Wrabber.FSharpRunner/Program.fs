open System
open Wrabber

[<EntryPoint>]
let main argv = 
    let wrabber = new Wrabber.Wrabber("e:\\temp")
    wrabber.AddToCapture(new Uri("http://eb.dk"), "ebdk", {width=1024; height=768}, TimeSpan.FromSeconds(30.0)) |> ignore
    wrabber.AddToCapture(new Uri("http://bt.dk"), "btdk", {width=1024; height=768}, TimeSpan.FromSeconds(30.0)) |> ignore

    Console.ReadLine() |> ignore
    
    0 // return an integer exit code
