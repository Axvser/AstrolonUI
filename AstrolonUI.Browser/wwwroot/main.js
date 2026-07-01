import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

// 确保 NativeWebView iframe 导航到独立渲染页面
setTimeout(function() {
    var host = document.querySelector('.avalonia-native-host');
    if (host) {
        var iframe = host.querySelector('iframe');
        if (iframe && !iframe.src) {
            iframe.src = window.location.origin + '/standalone-md.html';
            console.log('[main] Auto-navigated iframe to: ' + iframe.src);
        }
    }
}, 2000);
