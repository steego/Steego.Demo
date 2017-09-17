module Steego.Demo.ReflectionTests

open System
open Steego.Demo.Reflection
open NUnit.Framework

type Person(name:string, dob: DateTime) = 
    member this.Name = name
    member this.Age = int((DateTime.Now - dob).TotalDays / 365.0)
    member private this.Test = 123

[<Test>]
let ``getPublicProperties ignores private`` () =
  let props = 
    typeof<Person> |> TypeInfo.getPublicProperties |> Array.map (fun p -> p.Name) |> Set.ofArray
  
  Assert.AreEqual(Set.ofList(["Name"; "Age"]),props)
