namespace Wrabber
open System
open System.Collections.Concurrent
open System.Drawing
open System.Drawing.Imaging
open System.IO
open System.Linq
open System.Threading
open System.Windows.Forms
open Microsoft.FSharp.Control


module CaptureItem = 
    type CaptureSize = {
        width: int
        height: int
    }

    type CaptureItem = {
        siteAddress: Uri
        imageName  : string
        captureInterval : TimeSpan
        captureSize : CaptureSize
        mutable lastCaptureTime : DateTime
    }

    let create siteAddress imageName captureSize captureInterval= 
        {
            siteAddress     = siteAddress
            imageName       = imageName
            captureInterval = captureInterval
            captureSize     = captureSize
            lastCaptureTime = DateTime.MinValue
        }

    type WrabHandler = Bitmap -> unit

    let saveToPngFile fullFilename (bitmap: Bitmap) =
        let fullPath = Path.ChangeExtension(fullFilename, ".png")
        bitmap.Save(fullPath, ImageFormat.Png)
        
module CaptureThread = 
    open CaptureItem

    type CaptureThreadItem = {
        siteAddress : Uri
        captureSize : CaptureSize
        wrabHandler : WrabHandler
    }

    type CaptureThread() =
        let synchronizer = new Object()

        [<DefaultValue>]
        val mutable siteAddress : Uri

        [<DefaultValue>]
        val mutable wrabHandler : WrabHandler

        [<DefaultValue>]
        val mutable captureSize : CaptureSize

        member this.CaptureSite(siteAddress: Uri, captureSize: CaptureSize, wrabHandler : WrabHandler) =
            lock synchronizer (fun () ->
                this.siteAddress <- siteAddress
                this.wrabHandler <- wrabHandler
                this.captureSize <- captureSize

                let captureThread = new Thread(this.ExecuteCapture)
                captureThread.SetApartmentState ApartmentState.STA
                captureThread.Start()
                captureThread.Join())

        member this.ExecuteCapture() =
            try
                use browser = new WebBrowser()
                browser.ScrollBarsEnabled <- false
                browser.AllowNavigation <- true
                browser.Width <- this.captureSize.width
                browser.Height <- this.captureSize.height
                browser.ScriptErrorsSuppressed <- true
                browser.DocumentCompleted.Add(fun e -> this.DocumentCompleted(browser, e))
                browser.Navigate this.siteAddress
                while browser.ReadyState <> WebBrowserReadyState.Complete do
                    Application.DoEvents()
            with
                | e -> ()

        member this.DocumentCompleted(browser: WebBrowser, e: WebBrowserDocumentCompletedEventArgs) =
            if browser = null then
                ()
            try
                use bitmap = new Bitmap(browser.Width, browser.Height)
//                let fullPath = Path.ChangeExtension(this.fullFileName, ".png")
                browser.DrawToBitmap(bitmap, new Rectangle(0, 0, browser.Width, browser.Height))
                this.wrabHandler bitmap
//                bitmap.Save(fullPath, ImageFormat.Png)
            finally
                ()

module Wrabber = 
    open CaptureItem

    type Wrabber(imageCachePath: string) as this = 
        let imageCachePath = imageCachePath
        let captureItems = new ConcurrentDictionary<Uri, CaptureItem>()
        let captureThread = new CaptureThread.CaptureThread()
        do this.RunCapturing()

        member this.AddToCapture(siteAddress: Uri, imageName: string, captureSize: CaptureSize, captureInterval: TimeSpan) =
            let newCaptureItem = CaptureItem.create siteAddress imageName captureSize captureInterval
            captureItems.[newCaptureItem.siteAddress] <- newCaptureItem
            captureItems.Count
            
        member private this.NextCapture() : CaptureItem option = 
            let relevantCaptureItems = 
                captureItems.Values
                    .OrderBy(fun item -> item.lastCaptureTime + item.captureInterval)
                    |> List.ofSeq
            match relevantCaptureItems with
            | first :: tail -> Some(first)
            | [] -> None

        member private this.RunCapturing() =
            async {
                while true do
                    try
                        let now = DateTime.UtcNow
                        let nextCapture = this.NextCapture()
                        let interval = 
                            match nextCapture with
                            | None -> TimeSpan.FromSeconds(30.0)
                            | Some(nextCapture) ->                        
                                let captureTime = nextCapture.lastCaptureTime + nextCapture.captureInterval
                                if captureTime <= now then
                                    this.Capture nextCapture
                                    TimeSpan.FromSeconds(1.0)
                                else
                                    captureTime - now

                        Thread.Sleep(interval)
                    with
                    | e -> ()

                    Thread.Sleep 100
                    } |> Async.StartAsTask |> ignore 

        
        member private this.Capture (captureItem: CaptureItem) =
            try
                let fullPath = Path.ChangeExtension(Path.Combine(imageCachePath, captureItem.imageName), ".png")
                captureThread.CaptureSite(captureItem.siteAddress, captureItem.captureSize, saveToPngFile fullPath)
                captureItem.lastCaptureTime <- DateTime.UtcNow
            with
            | e -> ()

