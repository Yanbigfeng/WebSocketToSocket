var ws;

function WebSocketTest() {
    if ("WebSocket" in window) {
        alert("您的浏览器支持 WebSocket!");

        // 打开一个 web socket
        ws = new WebSocket("ws://192.168.1.25:8885");
        ws.onopen = function () {
            // Web Socket 已连接上，使用 send() 方法发送数据
            ws.send("login,我已经连接上了！！！");
        };

        ws.onmessage = function (evt) {
            var received_msg = evt.data;
            document.getElementById("look").innerHTML += "</br>" + received_msg;
        };

        ws.onclose = function () {
            // 关闭 websocket
            alert("连接已关闭...");
        };
    } else {
        // 浏览器不支持 WebSocket
        alert("您的浏览器不支持 WebSocket!");
    }
}

function webSocketClose() {
    ws.close();
    alert("关闭了通讯")
}
//单聊
function send() {
    var msg = document.getElementById("message").value;
    var data = "" + document.getElementById("userId").value + "," + msg
    if (msg == "" || msg == undefined) {
        alert("请填写发送内容！")
        return;
    }
    ws.send(data);
}
//群发（所有用户）
function sendGroup() {
    var msg = document.getElementById("message").value;
    var data = "all," + msg
    if (msg == "" || msg == undefined) {
        alert("请填写发送内容！")
        return;
    }
    ws.send(data);
}
//群组发送A
function sendGroupA() {
    var msg = document.getElementById("message").value;
    var data = "groupA," + msg
    if (msg == "" || msg == undefined) {
        alert("请填写发送内容！")
        return;
    }
    ws.send(data);
}
//群组发送A
function sendGroupB() {
    var msg = document.getElementById("message").value;
    var data = "groupB," + msg
    if (msg == "" || msg == undefined) {
        alert("请填写发送内容！")
        return;
    }
    ws.send(data);
}
