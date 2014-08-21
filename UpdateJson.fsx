﻿// This script uses Google's Analysis Service docs to generate C# classes
// that are used for serialising/deserialising JSON.

#r "System.Xml.Linq"
open System
open System.IO
open System.Text
open System.Xml.Linq
open System.Xml.XPath

Directory.SetCurrentDirectory(__SOURCE_DIRECTORY__)
let root = Directory.GetCurrentDirectory()
let apiDocFilename = """M:\Coding\Applications\Dart\dart\pkg\analysis_server\tool\spec\spec_input.html"""
let outputJsonFilename = """DanTup.DartAnalysis\Json.cs"""
let outputRequestFilename = """DanTup.DartAnalysis\Requests.cs"""

let doc = XDocument.Load(apiDocFilename)
let ( !! ) : string -> XName = XName.op_Implicit;;

let collect (f : XElement -> string) (x : seq<XElement>) : string =
    x |> Seq.map f |> String.concat ""

let extractDoc (fieldNode : XElement) =
    match fieldNode.Element(!!"p") with
        | null -> ""
        | _ ->
            fieldNode.Element(!!"p").Value.Trim().Split('\n')
                |> Seq.map (fun s -> s.Trim())
                |> Seq.map (sprintf "\t\t/// %s")
                |> String.concat "\r\n"
                |> sprintf "\t\t/// <summary>\r\n%s\r\n\t\t/// </summary>\r\n"

let getCSharpType = function
    | "String" -> "string"
    | x -> x

let formatName (x : string) = x.[0].ToString().ToUpper() + x.[1..]

let extractCSharpType (fieldNode : XElement) =
    let cSharpType = fieldNode.XPathSelectElement(".//ref").Value |> getCSharpType
    match fieldNode.Element(!!"list") with
        | null -> sprintf "%s" cSharpType
        | _ -> sprintf "%s[]" cSharpType

let getField (fieldNode : XElement) =
    sprintf "%s\t\tpublic %s %s;\r\n"
        (fieldNode |> extractDoc)
        (fieldNode |> extractCSharpType)
        (fieldNode.Attribute(!!"name").Value |> formatName)
            

let getType (typeNode : XElement) =
    sprintf "\tpublic class %s\r\n\t{\r\n%s\t}\r\n\r\n"
        (typeNode.Attribute(!!"name").Value)
        (typeNode.Descendants(!!"field") |> collect getField)

let getRequest (typeNode : XElement) =
    match typeNode.Element(!!"params") with
        | null -> ""
        | _ ->
            sprintf "\t[AnalysisMethod(\"%s.%s\")]\r\n\tpublic class %s%s%s\r\n\t{\r\n%s\t}\r\n\r\n"
                (typeNode.Parent.Attribute(!!"name").Value)
                (typeNode.Attribute(!!"method").Value)
                (typeNode.Parent.Attribute(!!"name").Value |> formatName)
                (typeNode.Attribute(!!"method").Value |> formatName)
                "Request"
                (typeNode.Element(!!"params").Descendants(!!"field") |> collect getField)

let getResponse (typeNode : XElement) =
    match typeNode.Element(!!"result") with
        | null -> ""
        | _ ->
            sprintf "\tpublic class %s%s%s\r\n\t{\r\n%s\t}\r\n\r\n"
                (typeNode.Parent.Attribute(!!"name").Value |> formatName)
                (typeNode.Attribute(!!"method").Value |> formatName)
                "Response"
                (typeNode.Element(!!"result").Descendants(!!"field") |> collect getField)

let getAllJsonTypes () =
    sprintf """// Code generated by UpdateJson.fsx. Do not hand-edit!

namespace DanTup.DartAnalysis.Json
{
%s
%s
%s
}"""
        (doc.Document.XPathSelectElements("//types/type") |> collect getType)
        (doc.Document.XPathSelectElements("//domain/request") |> collect getRequest)
        (doc.Document.XPathSelectElements("//domain/request") |> collect getResponse)



let getRequestWrapper (typeNode : XElement) =
    let className =
        (typeNode.Parent.Attribute(!!"name").Value |> formatName)
        + (typeNode.Attribute(!!"method").Value |> formatName)

    let requestClassName =
        className
        + "Request"

    let responseClassName =
        match typeNode.Element(!!"result") with
            | null -> ""
            | _ ->
                sprintf "%s%sResponse"
                    (typeNode.Parent.Attribute(!!"name").Value |> formatName)
                    (typeNode.Attribute(!!"method").Value |> formatName)

    let baseClass =
        match typeNode.Element(!!"params"), typeNode.Element(!!"result") with
            | null, null -> "Request<Response>"
            | null, _ ->
                sprintf "Request<Response<%s>>"
                    responseClassName
            | _, null ->
                sprintf "Request<%s, Response>"
                    requestClassName
            | _, _ ->
                sprintf "Request<%s, Response<%s>>"
                    requestClassName
                    responseClassName

    let ctor =
        match typeNode.Element(!!"params") with
            | null -> ""
            | _ ->
                sprintf "\t\tpublic %s(%s @params)\r\n\t\t\t: base(@params)\r\n\t\t{\r\n\t\t}"
                    className
                    requestClassName

    sprintf "\tpublic class %s : %s\r\n\t{\r\n%s\r\n\t}\r\n\r\n"
        className
        baseClass
        ctor
                


let getAllRequestTypes () =
    sprintf """// Code generated by UpdateJson.fsx. Do not hand-edit!

using DanTup.DartAnalysis.Json;

namespace DanTup.DartAnalysis
{
%s
}"""
        (doc.Document.XPathSelectElements("//domain/request") |> collect getRequestWrapper)




// Do the stuff!
File.WriteAllText(outputJsonFilename, getAllJsonTypes())
File.WriteAllText(outputRequestFilename, getAllRequestTypes())