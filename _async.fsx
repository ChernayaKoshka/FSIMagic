[<RequireQualifiedAccess>]
module Async =
    open System
    let runTaskSynchronously task =
        task
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let chunkAndProcess parallelCount (work:seq<Async<'a>>) =
        async {
            let chunkSize = Math.Ceiling((float (work |> Seq.length))/(float parallelCount)) |> int
            match chunkSize with
            | 0 -> 
                return List.empty //prevent exception in List.chunkBySize since chunkSize must be >= 1
            | _ ->
                return
                    work
                    |> Seq.chunkBySize chunkSize
                    |> Seq.collect (Async.Parallel >> Async.RunSynchronously)
                    |> List.ofSeq
        }

    let chunkAndDo parallelCount (work:seq<Async<'a>> ) =
        async {
            return!
                chunkAndProcess parallelCount work
                |> Async.Ignore
        }
