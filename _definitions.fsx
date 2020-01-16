open System.Windows.Forms
#r "Microsoft.VisualBasic"
#r "System.IO.Compression"
#r "System.IO.Compression.FileSystem"

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Diagnostics

open Microsoft.VisualBasic.FileIO

// PRE-BUILT VARIABLES

type VarWrapper() =
    member val Rand = Random()

    member val Web = new WebClient()

    member val ComSpec = Environment.GetEnvironmentVariable("COMSPEC")

    member val Desktop = 
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop)

    member val Documents = 
        Environment.GetFolderPath(Environment.SpecialFolder.Personal)

    member __.Clipboard 
        with get() =
            Clipboard.GetText()
        and set value = 
            Clipboard.SetText(value)

    member __.CurrentDirectory = Directory.GetCurrentDirectory()

let vars = VarWrapper()

// HELPER EXTENSIONS
[<RequireQualifiedAccess>]
module Seq =
    open FSharp.Reflection

    let toTuple<'listItem, 'returnType> (items : 'listItem seq) =
        let types = FSharpType.GetTupleElements typeof<'returnType>

        if (Seq.length items <> Seq.length types) then
            failwith "Expected tuple length did not match sequence length!"

        FSharpValue.MakeTuple(
                items 
                |> Seq.mapi (fun i item -> 
                    Convert.ChangeType(item, types.[i]))
                |> Array.ofSeq, typeof<'returnType>)
        :?> 'returnType

// STRING MANIPULATION

[<RequireQualifiedAccess>]
module String =
    let inline enquotes str =
        "\"" + str + "\""

    let normalize str = 
        Regex.Replace(str, "(?:\r?\n){2,}", "\n")

    let replace (toReplace : string) (replaceWith : string) (str : string) =
        str.Replace(toReplace, replaceWith)

    let strip toRemove (str : string)  =
        str.Replace(toRemove, "")

    let split (split : char) (str : string) =
        str.Split(split)

    let contains (containsStr : string) (str : string)=
        str.Contains(containsStr)

    let count needle (str : string) =
        Regex.Matches(str, needle).Count

// FILES

let getTempFile() =
    Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())

let getFilesIn dir =
    Directory.GetFiles(dir)

let getFiles() =
    getFilesIn vars.CurrentDirectory

// EXECUTION

let exec (args : string) (path : string) = 
    let startInfo = ProcessStartInfo()
    startInfo.FileName <- path
    startInfo.Arguments <- args
    Process.Start(startInfo)

let execSilent (args : string) (path : string) =
    let startInfo = ProcessStartInfo()
    startInfo.FileName <- path
    startInfo.Arguments <- args
    startInfo.CreateNoWindow <- true
    startInfo.UseShellExecute <- false
    Process.Start(startInfo)

let execRead (args : string) (path : string) =
    if not <| File.Exists(path) then
        Error (sprintf "Could not find file \"%s\"" path)
    else
        let startInfo = ProcessStartInfo(path)
        startInfo.Arguments <- args
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true

        let runner = Process.Start(startInfo)
        let str = runner.StandardOutput.ReadToEnd()
        runner.WaitForExit()
        Ok str

// FILE I/O

let inline write text =
    let path = Path.Combine(vars.Desktop, vars.Rand.Next().ToString() + ".txt")
    File.WriteAllText(path, text)
    path
let writeOpen text = 
    text
    |> write 
    |> exec ""

let inline writeLines lines =
    let path = Path.Combine(vars.Desktop, vars.Rand.Next().ToString() + ".txt")
    File.WriteAllLines(path, lines)
    path
let writeLinesOpen lines = 
    lines
    |> writeLines
    |> exec ""

let inline writeLinesTo path lines =
    File.WriteAllLines(path, lines)
    path
let writeLinesToOpen path lines = writeLinesTo path lines |> exec ""

let inline writeTo path text =
    File.WriteAllText(path, text)
    path
let writeToOpen path text = writeTo path text |> exec ""

let csv str =
    use reader = new StringReader(str)
    use parser = new TextFieldParser(reader)
    parser.CommentTokens <- [|"#"|]
    parser.SetDelimiters([| ","; ";"; "|"; "\t" |])
    parser.HasFieldsEnclosedInQuotes <- true
    [| while not parser.EndOfData do yield parser.ReadFields() |]

let inline read (path : string) =
    File.ReadAllText(path)

let readCsv = read >> csv

let inline reader (path : string) = 
    new StreamReader(path)

/// Downloads a file from the specified address and runs it with the provided arguments
/// If the file already exists, it will not be downloaded but will be run.
let downloadAndRun (address : string) (executeWith : string ) (filename : string) (arguments : string) =
    Console.WriteLine(executeWith + " Run Result: ")
    if not <| File.Exists(filename) then
        Console.WriteLine(filename + " does not exist. Downloading from: " + address)
        vars.Web.DownloadFile(address, filename)
    execRead arguments executeWith

// CONSOLE MANIPULATION
let cmd command =
    if String.IsNullOrEmpty(vars.ComSpec) then
        Error "Could not locate COMSPEC, please add 'cmd.exe' path to your COMSPEC environment variable"
    else
        execRead (sprintf "/Q /C %s" command) vars.ComSpec

let ps command =
    execRead (sprintf "-Command %s" command) @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"

let notify (str : string) =
    Console.ForegroundColor <- ConsoleColor.Green
    Console.WriteLine(str)
    Console.ForegroundColor <- ConsoleColor.White

let cls() = Console.Clear()

// DIRECTORY STUFF

let rec findFilesInDir regex dir =
    [| 
        for file in getFilesIn dir do
            if Regex.IsMatch(file, regex) then
                yield file
        yield!
            Directory.GetDirectories(dir)
            |> Array.collect (findFilesInDir regex)
    |]

let cd str = 
    Directory.SetCurrentDirectory(str)
    Directory.GetCurrentDirectory()
    |> notify

let up = ".."
