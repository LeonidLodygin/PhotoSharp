namespace PhotoSharp

open Avalonia
open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open Avalonia.Platform.Storage
open Brahma.FSharp
open ImageProcessing.MyImage
open ImageProcessing.Types
open System.IO
open Avalonia.Media.Imaging
open Microsoft.FSharp.Control
open ImageProcessing
open ImageProcessing.Arguments
open ImageProcessing.ImageArrayProcessing


type MainWindow () as this = 
    inherit Window ()
    
    let mutable tmpPath = ""
    let mutable directoryPath = ""
    let tmp = Path.Combine([|__SOURCE_DIRECTORY__; "tmp"|])
    let mutable tmpExtension = ""
    let mutable tmpImage = MyImage([||], 0, 0, "")
    
    let cleanDirectory path =
        let files = Directory.GetFiles(path)
        for file in files do
            File.Delete(file)
     
    let modParser name =
        match name with
        | "Gauss5x5" -> Gauss5x5
        | "Gauss7x7" -> Gauss7x7
        | "Edges" -> Edges
        | "Sharpen" -> Sharpen
        | "Emboss" -> Emboss
        | "ClockwiseRotation" -> ClockwiseRotation
        | "CounterClockwiseRotation" -> CounterClockwiseRotation
        | "MirrorVertical" -> MirrorVertical
        | "MirrorHorizontal" -> MirrorHorizontal
        | "FishEye" -> FishEye
        
    let modComposition (control:ListBox) =
        let listOfFunc = List.ofSeq (control.Items |> Seq.cast<string>) |> List.map (fun x -> x |> modParser)
        let gpuCheck = this.FindControl<ToggleSwitch>("GPGPU").IsChecked
        let filters =
            if (gpuCheck.Value) then
                let clContext = ClContext(ClDevice.GetFirstAppropriateDevice())
                let queue = clContext.QueueProvider.CreateQueue()
                let filterKernel = GpuKernels.applyFilterKernel clContext
                let rotateKernel = GpuKernels.rotateKernel clContext
                let mirrorKernel = GpuKernels.mirrorKernel clContext
                let fishKernel = GpuKernels.fishEyeKernel clContext
                let kernelsCortege = (filterKernel, rotateKernel, mirrorKernel, fishKernel)
                List.map (fun n -> modificationGpuParser n kernelsCortege clContext 64 queue) listOfFunc
            else
                listOfFunc |> List.map modificationParser
        List.reduce (>>) filters  
    
    do this.InitializeComponent()
       
    member this.openImage(sender: obj, args: RoutedEventArgs) =
        let topLevel = TopLevel.GetTopLevel(this)
        let file = topLevel.StorageProvider.OpenFilePickerAsync(FilePickerOpenOptions(Title = "Open the image", AllowMultiple = false))
        async {
            let! result = Async.AwaitTask file
            if result.Count >= 1 then
                let imageControl = this.FindControl<Image>("Image")
                let path = result[0].TryGetLocalPath()
                tmpExtension <- Path.GetExtension(path)
                if (Array.contains tmpExtension extensions) then
                    cleanDirectory tmp
                    tmpImage <- loadAsImage path
                    let name = Path.GetFileName(path)
                    tmpPath <- Path.Combine([|__SOURCE_DIRECTORY__; "tmp"; name|])
                    saveImage tmpImage tmpPath 
                    imageControl.Source <- new Bitmap(path)
                    
                    this.FindControl<Button>("SaveImage").IsEnabled <- true
                    this.FindControl<ToggleSwitch>("Agents").IsEnabled <- false
                    this.FindControl<Button>("ApplyModification").IsEnabled <- true
                    this.FindControl<Panel>("ImagePanel").IsVisible <- true
                    this.FindControl<ListBox>("ListOfFiles").IsVisible <- false
                    this.FindControl<Button>("SaveDirectory").IsEnabled <- false
                ()
        } |> Async.StartImmediate
        
    member this.openDirectory (sender: obj, args: RoutedEventArgs) =
        let topLevel = TopLevel.GetTopLevel(this)
        let file = topLevel.StorageProvider.OpenFolderPickerAsync(FolderPickerOpenOptions(Title = "Open the directory", AllowMultiple = false))
        async {
            let! result = Async.AwaitTask file
            if result.Count >= 1 then
                let path = result[0].TryGetLocalPath()
                let files = listAllFiles path
                if files.Length > 0 then
                    directoryPath <- path
                    cleanDirectory tmp
                    let textControl = this.FindControl<ListBox>("ListOfFiles")
                    for file in files do
                        textControl.Items.Add(Path.GetFileName file) |> ignore
                        
                    this.FindControl<Button>("SaveImage").IsEnabled <- false
                    this.FindControl<ToggleSwitch>("Agents").IsEnabled <- true
                    this.FindControl<Button>("ApplyModification").IsEnabled <- false
                    this.FindControl<Panel>("ImagePanel").IsVisible <- false
                    this.FindControl<ListBox>("ListOfFiles").IsVisible <- true
                    this.FindControl<Button>("SaveDirectory").IsEnabled <- true    
                ()
        } |> Async.StartImmediate
        
    member this.saveImage (sender: obj, args: RoutedEventArgs) =    
        let topLevel = TopLevel.GetTopLevel(this)
        let file = topLevel.StorageProvider.SaveFilePickerAsync(FilePickerSaveOptions(Title = "Save the image"))
        async {
            let! result = Async.AwaitTask file
            if result <> null then
                saveImage tmpImage (result.TryGetLocalPath() + tmpExtension)
                this.FindControl<Button>("SaveImage").IsEnabled <- false
                cleanDirectory tmp
                ()
        } |> Async.StartImmediate
              
    member this.addModification (sender: obj, args: RoutedEventArgs) =
        let textControl = this.FindControl<ListBox>("ListOfModifications")
        match sender with
        | :? MenuItem as menuItem -> 
            let itemName = menuItem.Header
            textControl.Items.Add(itemName) |> ignore
        | _ -> ()
        
    member this.deleteModification (sender: obj, args: RoutedEventArgs) =
        let textControl = this.FindControl<ListBox>("ListOfModifications")
        let count = textControl.Items.Count
        if (count > 0) then
            textControl.Items.RemoveAt(count - 1)
        
    member this.clearModifications (sender: obj, args: RoutedEventArgs) =
        let textControl = this.FindControl<ListBox>("ListOfModifications")
        textControl.Items.Clear()
        
    member this.applyModifications (sender: obj, args: RoutedEventArgs) =
        let textControl = this.FindControl<ListBox>("ListOfModifications")
        let composition = modComposition textControl        
        let filtered = composition tmpImage
        tmpImage <- filtered
        saveImage filtered tmpPath
        let imageControl = this.FindControl<Image>("Image")
        imageControl.Source <- new Bitmap(tmpPath)
        
    member this.applyAndSave (sender: obj, args: RoutedEventArgs) =
        let textControl = this.FindControl<ListBox>("ListOfModifications")
        let composition = modComposition textControl
        let agentCheck = this.FindControl<ToggleSwitch>("Agents").IsChecked
        let topLevel = TopLevel.GetTopLevel(this)
        let file = topLevel.StorageProvider.OpenFolderPickerAsync(FolderPickerOpenOptions(Title = "Save images", AllowMultiple = false))
        async {
            let! result = Async.AwaitTask file
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(fun () ->
                this.FindControl<ListBox>("ListOfFiles").IsVisible <- false
                this.FindControl<ProgressBar>("ProgressBar").IsVisible <- true
                this.FindControl<Button>("SaveDirectory").IsEnabled <- false
            ) |> ignore
            if result.Count >= 1 then
                let savePath = result[0].TryGetLocalPath()
                let status =
                    match agentCheck.Value with
                    | true -> On
                    | false -> Off
                do! arrayOfImagesProcessing directoryPath savePath composition status |> Async.SwitchToNewThread
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(fun () ->
                    this.FindControl<ProgressBar>("ProgressBar").IsVisible <- false
                    this.FindControl<ListBox>("ListOfFiles").IsVisible <- true
                    this.FindControl<Button>("SaveDirectory").IsEnabled <- true
                 ) |> ignore
                ()
        } |> Async.Start

             

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)
        