using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;

namespace BeiShuiCS2.Services
{
    public class VoiceMessage
    {
        public string Type { get; set; } = "";
        public string Data { get; set; } = "";
    }

    public class VoiceService
    {
        private WebView2? _webView;
        private SignalRService? _signalR;
        private bool _isInCall;
        private bool _initialized;

        public bool IsInCall => _isInCall;

        public event Action<bool>? OnCallStateChanged;

        /// <summary>
        /// 初始化语音服务：确保 WebView2 内核就绪后注入 HTML，再绑定信令
        /// </summary>
        public async Task InitializeAsync(WebView2 webView, SignalRService signalR)
        {
            _webView = webView;
            _signalR = signalR;

            // 1. 等待 WebView2 初始化完成
            await webView.EnsureCoreWebView2Async();

            // 2. 加载嵌入的 voice.html
            var htmlContent = await Task.Run(() => GetEmbeddedVoiceHtml());
            webView.NavigateToString(htmlContent);

            // 3. 等待导航完成（WebView2 异步加载 HTML）
            await Task.Delay(100); // 给 WebView2 一点时间解析 HTML

            // 4. 绑定 JS 消息回调
            webView.CoreWebView2.WebMessageReceived += OnWebMessage;

            // 5. 绑定 SignalR 语音信令
            _signalR.OnVoiceSignal((type, data) =>
            {
                if (_webView?.CoreWebView2 != null)
                {
                    var json = JsonSerializer.Serialize(new { type, data });
                    _webView.CoreWebView2.PostWebMessageAsJson(json);
                }
            });

            _initialized = true;
        }

        private async void OnWebMessage(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = JsonSerializer.Deserialize<VoiceMessage>(e.WebMessageAsJson);
                if (message == null || _signalR == null) return;

                await _signalR.SendVoiceSignal(message.Type, message.Data);
            }
            catch { }
        }

        /// <summary>
        /// 开始通话：通知 JS 创建 PeerConnection
        /// PostWebMessageAsJson 是同步调用，此处用 Task 兼容调用方接口
        /// </summary>
        public Task StartCall(string roomCode)
        {
            if (!_initialized) return Task.FromResult(false);

            _isInCall = true;
            OnCallStateChanged?.Invoke(true);

            if (_webView?.CoreWebView2 != null)
            {
                var json = JsonSerializer.Serialize(new { type = "start_call", roomCode });
                _webView.CoreWebView2.PostWebMessageAsJson(json);
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// 结束通话：通知 JS 清理 PeerConnection
        /// </summary>
        public Task EndCall()
        {
            if (!_initialized) return Task.FromResult(false);

            _isInCall = false;
            OnCallStateChanged?.Invoke(false);

            if (_webView?.CoreWebView2 != null)
            {
                var json = JsonSerializer.Serialize(new { type = "end_call" });
                _webView.CoreWebView2.PostWebMessageAsJson(json);
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// 获取嵌入的 WebRTC voice.html 内容
        /// </summary>
        private string GetEmbeddedVoiceHtml()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "BeiShuiCS2.Voice.voice.html";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
            catch { }

            return GetDefaultVoiceHtml();
        }

        private static string GetDefaultVoiceHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
* { margin: 0; padding: 0; }
body { background: transparent; }
</style>
<script>
let pc = null;
const config = { iceServers: [{ urls: 'stun:stun.l.google.com:19302' }] };

window.chrome.webview.addEventListener('message', async (e) => {
    const msg = e.data;
    switch(msg.type) {
        case 'start_call': createOffer(); break;
        case 'offer': await handleOffer(msg.data); break;
        case 'answer': await pc.setRemoteDescription(new RTCSessionDescription(JSON.parse(msg.data))); break;
        case 'ice_candidate': await pc.addIceCandidate(new RTCIceCandidate(JSON.parse(msg.data))); break;
        case 'end_call': cleanup(); break;
    }
});

async function createOffer() {
    pc = new RTCPeerConnection(config);
    setupPcHandlers();
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    stream.getTracks().forEach(t => pc.addTrack(t, stream));
    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);
    window.chrome.webview.postMessage(JSON.stringify({ type: 'offer', data: JSON.stringify(offer) }));
}

async function handleOffer(offerStr) {
    pc = new RTCPeerConnection(config);
    setupPcHandlers();
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    stream.getTracks().forEach(t => pc.addTrack(t, stream));
    await pc.setRemoteDescription(new RTCSessionDescription(JSON.parse(offerStr)));
    const answer = await pc.createAnswer();
    await pc.setLocalDescription(answer);
    window.chrome.webview.postMessage(JSON.stringify({ type: 'answer', data: JSON.stringify(answer) }));
}

function setupPcHandlers() {
    pc.onicecandidate = e => {
        if (e.candidate) {
            window.chrome.webview.postMessage(JSON.stringify({ type: 'ice_candidate', data: JSON.stringify(e.candidate) }));
        }
    };
    pc.ontrack = e => {
        const audio = document.createElement('audio');
        audio.srcObject = e.streams[0];
        audio.autoplay = true;
    };
}

function cleanup() {
    if (pc) { pc.close(); pc = null; }
}
</script>
</head>
<body></body>
</html>";
        }
    }
}
