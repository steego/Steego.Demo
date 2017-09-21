module Steego.Demo.Explorer

open System // 
open Suave //
open Steego
open Steego.Reflection.Navigators

[<Literal>]
let Scripts = @"
<script language=""javascript"" type=""text/javascript"">
    var wsUri = 'ws://localhost:8080/websocket';
    var output;

    function init() {
        output = document.getElementById('output');
        testWebSocket();
    }

    function testWebSocket() {
        websocket = new WebSocket(wsUri);
        websocket.onopen = function (evt) { onOpen(evt) };
        websocket.onclose = function (evt) { onClose(evt) };
        websocket.onmessage = function (evt) { onMessage(evt) };
        websocket.onerror = function (evt) { onError(evt) };

        let count = 0;
        window.setInterval(function(evt) {
            count += 1;
            doSend('Interval ' + count)
        }, 2000)
    }

    function onOpen(evt) {
        writeToScreen('CONNECTED');
        doSend('WebSocket rocks');
    }

    function onClose(evt) {
        writeToScreen('DISCONNECTED');
    }

    function onMessage(evt) {
        writeToScreen('<span style=""color: blue;"">RESPONSE: ' + evt.data + '</span>');
        //websocket.close();
    }

    function onError(evt) {
        writeToScreen('<span style=""color: red;"">ERROR:</span> ' + evt.data);
    }

    function doSend(message) {
        writeToScreen('SENT: ' + message);
        websocket.send(message);
    }

    function writeToScreen(message) {
        var pre = document.createElement('p');
        pre.style.wordWrap = 'break-word';
        pre.innerHTML = message;
        output.appendChild(pre);
    }

    window.addEventListener('load', init, false);

</script>
"

[<Literal>]
let HeadTemplate = @"
  <head>
    <!--<meta http-equiv='refresh' content='2' /> -->
    <link href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css"" rel=""stylesheet"" integrity=""sha384-BVYiiSIFeK1dGmJRAkycuHAHRg32OmUcww7on3RYdg4Va+PmSTsz/K68vbdEjh4u"" crossorigin=""anonymous"">
    <script src=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js"" integrity=""sha384-Tc5IQib027qvyjSMfHjOMaLkfuWVxZxUPnCJA7l2mCWNIpG9mGCD8wGNIcPD7Txa"" crossorigin=""anonymous""></script>
    
  </head>
"

open Printer

let explore (level:int) (value:'a) = 
  
    let o = value :> obj
    let navigatedObj = o |> toContext |> NavigateContext "/"
    let tag = navigatedObj|> print level


    tag.ToString() |> SocketServer.sendHtml

//   SocketServer.update(fun (ctx:HttpContext) -> async {
//         let path = ctx.request.path
//         let o = value :> obj
//         let navigatedObj = o |> toContext |> NavigateContext path
//         let tag = navigatedObj |> print level
//         let html = sprintf "<html>%s\n%s<body><div id='output'>%s</div></body></html>" HeadTemplate Scripts (tag.ToString())
//         return! Successful.OK html ctx
//     })

type System.Object with
    member this.Explore(level:int) = explore level this

