using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BeiShuiCS2
{
    /// <summary>
    /// WebView2 动画引擎辅助类
    /// 在窗口中嵌入 HTML/JS 动画背景（粒子、极光、鼠标交互）
    /// </summary>
    public static class WebView2AnimationHelper
    {
        private static string? _animationHtmlPath;

        /// <summary>
        /// 获取动画 HTML 文件的完整路径（解压到临时目录）
        /// </summary>
        private static string GetAnimationHtmlPath()
        {
            if (_animationHtmlPath != null) return _animationHtmlPath;

            // 将嵌入的 HTML 资源复制到临时文件
            var tempDir = Path.Combine(Path.GetTempPath(), "BeiShuiAurora");
            Directory.CreateDirectory(tempDir);
            _animationHtmlPath = Path.Combine(tempDir, "AnimationBackground.html");

            // 从嵌入资源或文件系统加载
            var resourcePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "AnimationBackground.html");

            // 先尝试从输出目录找
            if (File.Exists(resourcePath))
            {
                File.Copy(resourcePath, _animationHtmlPath, true);
            }
            else
            {
                // 从项目源目录找
                var projectPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..",
                    "Resources", "AnimationBackground.html");
                if (File.Exists(projectPath))
                {
                    File.Copy(Path.GetFullPath(projectPath), _animationHtmlPath, true);
                }
            }

            return _animationHtmlPath;
        }

        /// <summary>
        /// 在指定网格中嵌入 WebView2 动画背景
        /// 必须使用 Grid 作为容器，WebView2 会填满整个 Grid
        /// </summary>
        public static async Task<WebView2> AttachToGrid(Grid targetGrid, bool enableInterop = true)
        {
            var webView = new WebView2
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false,   // 不拦截鼠标事件
                IsEnabled = false,          // 禁用 HWND 级输入，防止 HWND 覆盖层拦截侧边栏点击
                AllowDrop = false,
                DefaultBackgroundColor = System.Drawing.Color.Transparent
            };

            // 设置 ZIndex 到最底层
            Panel.SetZIndex(webView, -1);

            // 插入到 Grid 的最底层
            targetGrid.Children.Insert(0, webView);

            try
            {
                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: Path.Combine(Path.GetTempPath(), "BeiShuiAurora", "WebView2Data"));

                await webView.EnsureCoreWebView2Async(env);

                // 配置 WebView2
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                // 设置消息回调（必须在 Navigate 之前绑定，否则部分 WebView2 版本会抛异常）
                if (enableInterop)
                {
                    webView.CoreWebView2.WebMessageReceived += (s, e) =>
                    {
                        try
                        {
                            // JS 发送的消息可能是 JSON 格式（如 {"type":"ready"}）
                            // TryGetWebMessageAsString() 只能处理纯文本，JSON 消息需用 WebMessageAsJson
                            if (!string.IsNullOrEmpty(e.WebMessageAsJson) && e.WebMessageAsJson != "null")
                            {
                                System.Diagnostics.Debug.WriteLine($"[WebView2] {e.WebMessageAsJson}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[WebView2] Message parse error: {ex.Message}");
                        }
                    };
                }

                // 加载 HTML 动画页面（必须在事件绑定之后）
                var htmlPath = GetAnimationHtmlPath();
                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WebView2] Initialization error: {ex.Message}");
                // 失败时移除 WebView2，不影响用户体验
                targetGrid.Children.Remove(webView);
                return webView;
            }

            return webView;
        }

        /// <summary>
        /// 向 JS 动画引擎发送消息
        /// </summary>
        public static async Task SendCommand(WebView2 webView, string command, object? value = null)
        {
            if (webView?.CoreWebView2 == null) return;

            try
            {
                var json = value != null
                    ? $"{{\"type\":\"{command}\",\"value\":{value}}}"
                    : $"{{\"type\":\"{command}\"}}";
                await webView.CoreWebView2.ExecuteScriptAsync(
                    $"window.dispatchEvent(new CustomEvent('csharp-command', {{detail: {json}}}))");
            }
            catch { }
        }

        /// <summary>
        /// 调用 JS 引擎的方法
        /// </summary>
        public static async Task<string?> ExecuteJs(WebView2 webView, string script)
        {
            if (webView?.CoreWebView2 == null) return null;
            try
            {
                return await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch { return null; }
        }

        /// <summary>
        /// 调整粒子数量（性能与视觉的平衡）
        /// </summary>
        public static async Task SetParticleCount(WebView2 webView, int count)
        {
            await ExecuteJs(webView, $"window.setParticleCount({count})");
        }

        /// <summary>
        /// 调整动画速度
        /// </summary>
        public static async Task SetSpeed(WebView2 webView, double speed)
        {
            await ExecuteJs(webView, $"window.setSpeed({speed})");
        }

        /// <summary>
        /// 调整光晕强度
        /// </summary>
        public static async Task SetOrbIntensity(WebView2 webView, double intensity)
        {
            await ExecuteJs(webView, $"window.setOrbIntensity({intensity})");
        }
    }
}
