#r "System.Data"
#r "System.Transactions"

open System
open System.Data
open System.Data.Common

module SQL =
    let inline fromDbValue<'a when 'a : equality> (value : string) : Option<'a> =
        if isNull value then
            None
        else
            match typeof<'a> with
            | z when z = typedefof<bool> ->
                value <> "0"
                |> box
                :?> 'a
                |> Some
            | _ ->
                Convert.ChangeType(value, typeof<'a>)
                :?> 'a
                |> Some

    let queryDbWithColumnNamesAsync (cmd : DbCommand) =
        async {
            let! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask
            let schema = [ for i = 0 to reader.FieldCount - 1 do yield reader.GetName(i) ]
            let mutable data = []
            while reader.Read() do
                let row = 
                    [for i in 0..reader.FieldCount-1 do 
                        if reader.IsDBNull(i) then
                            yield null
                        else
                            yield reader.GetValue(i).ToString()]
                data <- (row :: data)
            do reader.Close()
            return (schema, data |> List.rev)
        }

    let queryDbWithColumnNames : DbCommand -> string list * string list list = queryDbWithColumnNamesAsync >> Async.RunSynchronously

    let queryDbAsync (cmd : DbCommand) = 
        async {
            let! (_, result) = queryDbWithColumnNamesAsync cmd
            return result
        }

    let queryDb (cmd : DbCommand) =
        queryDbAsync cmd
        |> Async.RunSynchronously
        
    let executeDbAsync (cmd : DbCommand) = async {
        return! cmd.ExecuteNonQueryAsync() |> Async.AwaitTask
    }

    let executeDb (cmd : DbCommand) =
        cmd.ExecuteNonQuery()
    
    let queryScalarDbAsync<'ReturnType> (cmd : DbCommand) = async {
        let! scalar = cmd.ExecuteScalarAsync() |> Async.AwaitTask
        return scalar :?> 'ReturnType
    }

    let queryScalarDb<'ReturnType> (cmd : DbCommand) =
        cmd.ExecuteScalar() :?> 'ReturnType

    let refreshConnection<'SqlConnection when 'SqlConnection :> DbConnection and 'SqlConnection : (new : unit -> 'SqlConnection)> (connection : 'SqlConnection) =
        match connection.State with
        | ConnectionState.Broken | ConnectionState.Closed ->
            let newConnection = new 'SqlConnection()
            newConnection.ConnectionString <- connection.ConnectionString
            do newConnection.Open()

            connection.Dispose()

            newConnection
        | _ ->
            connection

    let makeLazyConnection (con : DbConnection) =
        lazy (
            con.Open()
            con)