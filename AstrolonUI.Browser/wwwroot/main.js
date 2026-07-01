import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

// ===== C# JSImport 互调接口 =====
// 将 Markdown 文本渲染到 #preview（左下角浮动面板，对应 Agent 对话区域）
export function renderMarkdown(text) {
    var preview = document.getElementById('preview');
    if (!preview) return;
    if (typeof window.renderMarkdown === 'function') {
        window.renderMarkdown(text);
    } else if (typeof window.md !== 'undefined') {
        try {
            preview.innerHTML = window.md.render(text || '');
            preview.querySelectorAll('.mermaid').forEach(function(el) {
                try { window.mermaid.run({ nodes: [el] }); } catch(e) {}
            });
        } catch(e) {
            preview.innerHTML = '<pre>' + window.escapeHtml(text || '') + '</pre>';
        }
    } else {
        preview.innerHTML = '<p style="color:red">渲染引擎未加载</p>';
    }
    // 显示/隐藏预览面板
    preview.style.display = (text && text.length > 0) ? 'block' : 'none';
    // 也更新标题（由 renderer.js 设置）
    if (text && text.length > 0) {
        document.title = '✅ Rendered - AstrolonUI';
    }
}

export function showMarkdownEditor() {
    console.log('[MarkdownView] JSImport 就绪');
}

export function getPreviewHtml() {
    var pv = document.getElementById('preview');
    return pv ? pv.innerHTML : '';
}

// 导航 NativeWebView 的 iframe 到指定 URL
export function navigateWebView(url) {
    var host = document.querySelector('.avalonia-native-host');
    if (!host) { console.warn('[WebView] native host not found'); return; }
    var iframe = host.querySelector('iframe');
    if (!iframe) { console.warn('[WebView] iframe not found'); return; }
    iframe.src = url;
    console.log('[WebView] navigated to: ' + url);
}

// 通过 fetch 获取 HTML 文本（供 C# NavigateToString 使用）
export async function fetchText(url) {
    try {
        var resp = await fetch(url);
        return await resp.text();
    } catch(e) {
        console.error('[fetchText] ❌ ' + e.message);
        return '<p style="color:red">加载失败: ' + e.message + '</p>';
    }
}

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

// Avalonia 应用启动后，自动导航 NativeWebView 的 iframe 到独立渲染页面
setTimeout(function() {
    var host = document.querySelector('.avalonia-native-host');
    if (host) {
        var iframe = host.querySelector('iframe');
        if (iframe && !iframe.src) {
            iframe.src = window.location.origin + '/standalone-md.html';
            console.log('[main] Auto-navigated WebView iframe to: ' + iframe.src);
        } else {
            console.log('[main] iframe already has src: ' + (iframe ? iframe.src : 'no iframe'));
        }
    } else {
        console.log('[main] .avalonia-native-host not found');
    }
}, 2000);
