﻿module Steego.Type.Patterns

open System
open System.Collections
open System.Collections.Generic
open System.Linq
open Fasterflect
open Steego.TypeInfo

/////////////////////////////////////////////////////////////////////
//                           TYPE PATTERNS                         //
/////////////////////////////////////////////////////////////////////

module TypePatterns = begin

    let primitiveTypes = [
        typeof<int>; typeof<string>; typeof<DateTime>; typeof<bigint>; typeof<uint32>; typeof<char>;
        typeof<double>; typeof<decimal>; typeof<uint32>; typeof<byte>; typeof<float>; typeof<sbyte>; 
        typeof<int16>; typeof<uint64>; typeof<uint16>; typeof<bool>  
    ]

    let (|Primitive|)(t:Type) = 
        if primitiveTypes.Contains(t) then Some(t) else None

    let (|GenericType|_|) (t:Type) = 
        if t.IsGenericType then Some(t.GetGenericArguments() |> List.ofArray)
        else None

    let (|Implements|_|) (find:Type) (t:Type) = 
        if t = find || t.Implements(find) then Some() else None

    let (|GenericInterface|_|) (find:Type) (t:Type) = 
      if isNull t then None
      elif not t.IsGenericType then None
      elif t.IsInterface && t.GetGenericTypeDefinition() = find then 
        Some([ for a in t.GetGenericArguments() -> a ])
      else
        Seq.head <| seq { 
          for i in t.GetInterfaces() do
            if i.IsGenericType && i.GetGenericTypeDefinition() = find then 
              yield Some([ for a in i.GetGenericArguments() -> a ])
          yield None 
        }

    module GenericTypes = 
        let listType = typedefof<List<_>>
        let seqType = typedefof<seq<_>>
        let dictType = typedefof<IDictionary<_,_>>
        let arrayType = typedefof<_[]>
        let nullableType = typedefof<Nullable<_>>
        let optionType = typedefof<Option<_>>

    module NonGenericTypes = 
        let dictType = typedefof<IDictionary>
        let enumerableType = typedefof<IEnumerable>

    let (|SimpleEnumerable|_|) (t:Type) = 
        match t with
        | Implements NonGenericTypes.enumerableType as t -> Some(t)
        | _ -> None

    let (|IEnumerable|_|) = function
        | GenericInterface GenericTypes.seqType [t] -> Some(t)
        | GenericInterface GenericTypes.listType [t] -> Some(t)
        | _ -> None

    let (|UntypedDictionary|_|) (t:Type) = 
        match t with
        | Implements NonGenericTypes.dictType as t -> Some(t)
        | _ -> None

    let (|TypedDictionary|_|) = function
        | GenericInterface GenericTypes.dictType [t] -> Some(t)
        | _ -> None



    let (|SimpleObject|_|) = function
        | null -> None
        | Primitive(_) -> None
        | IEnumerable(_) -> None
        | SimpleEnumerable(_) -> None
        | t -> 
            let members = TypeInfo(t).Members
            let primitiveMembers = members |> List.filter(fun m -> isPrimitiveType m.Type)
            let objectMembers = members |> List.filter(fun m -> (not (isPrimitiveType m.Type)) && (not (isSeq m.Type)))
            let enumerableMembers = members |> List.filter(fun m -> isSeq m.Type)
            Some(members, primitiveMembers, objectMembers, enumerableMembers)

end

/////////////////////////////////////////////////////////////////////
//                         OBJECT PATTERNS                         //
/////////////////////////////////////////////////////////////////////

module ObjectPatterns = begin

    open TypePatterns

    let (|IsSeq|_|) (candidate : obj) =
        if isNull candidate then None
        else begin
            let t = candidate.GetType()
            if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<seq<_>>
            then Some (candidate :?> System.Collections.IEnumerable)
            else None
        end

    ///////////////////////////////////////////////////////

    let (|IsNullable|_|) (candidate : obj) =
        let t = candidate.GetType()
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Nullable<_>>
        then Some (candidate)
        else None

    let (|IsPrimitive|_|) (candidate : obj) =
        if isPrimitiveObject(candidate) then Some(candidate)
        else None

    let (|GenericList|_|)(o:obj) =
        if isNull o then None
        else
            let t = o.GetType()
            let ti = TypeInfo(t)
            if ti.IsGenericSeq then
                if not ti.IsPrimitive then
                    let getters = ti.ElementType.Members
                    let list = o :?> IEnumerable
                    Some(t, getters, list)
                else 
                    None
            elif t.IsArray && t.HasElementType then
                let ti = TypeInfo(t.GetElementType())
                if not ti.IsPrimitive then
                    let getters = ti.ElementType.Members
                    let list = o :?> IEnumerable
                    Some(t, getters, list)
                else 
                    None                
            else None

    let (|Object|_|)(o:obj) = 
        if isNull o then None
        elif isEnumerable(o) then None
        elif isPrimitiveObject(o) then None
        else
            let members = TypeInfo(o.GetType()).Members
            let primitiveMembers = members |> List.filter(fun m -> isPrimitiveType m.Type)
            let objectMembers = members |> List.filter(fun m -> (not (isPrimitiveType m.Type)) && (not (isSeq m.Type)))
            let enumerableMembers = members |> List.filter(fun m -> isSeq m.Type)
            Some(members, primitiveMembers, objectMembers, enumerableMembers, obj)
end
