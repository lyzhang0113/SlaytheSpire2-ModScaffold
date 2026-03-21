using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace STS2MenuControl;

[ModInitializer("Initialize")]
public static class MenuControlMod
{
    public const string Version = "0.1.0";
    private const int Port = 8081;

    private static HttpListener? _listener;
    private static Thread? _serverThread;
    internal static readonly ConcurrentQueue<Action> _mainThreadQueue = new();
    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Initialize()
    {
        try
        {
            var tree = (SceneTree)Engine.GetMainLoop();
            tree.Connect(SceneTree.SignalName.ProcessFrame, Callable.From(ProcessMainThreadQueue));

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();

            _serverThread = new Thread(ServerLoop)
            {
                IsBackground = true,
                Name = "STS2MenuControl_Server"
            };
            _serverThread.Start();

            GD.Print($"[STS2MenuControl] v{Version} server started on http://localhost:{Port}/");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[STS2MenuControl] Failed to start: {ex}");
        }
    }

    private static void ProcessMainThreadQueue()
    {
        int processed = 0;
        while (_mainThreadQueue.TryDequeue(out var action) && processed < 10)
        {
            try { action(); }
            catch (Exception ex) { GD.PrintErr($"[STS2MenuControl] Main thread error: {ex}"); }
            processed++;
        }
    }

    internal static T RunOnMainThread<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        _mainThreadQueue.Enqueue(() =>
        {
            try { tcs.SetResult(func()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task.GetAwaiter().GetResult();
    }

    internal static void RunOnMainThreadAsync(Action action)
    {
        _mainThreadQueue.Enqueue(() =>
        {
            try { action(); }
            catch (Exception ex) { GD.PrintErr($"[STS2MenuControl] Main thread error: {ex}"); }
        });
    }

    private static void ServerLoop()
    {
        while (_listener?.IsListening == true)
        {
            try
            {
                var ctx = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx));
            }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    private static void HandleRequest(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            var res = ctx.Response;
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            string path = req.Url?.AbsolutePath ?? "/";

            if (path == "/" || path == "/health")
            {
                SendJson(res, new { message = $"STS2MenuControl v{Version}", status = "ok" });
            }
            else if (path == "/api/v1/menu")
            {
                if (req.HttpMethod == "GET")
                    HandleGetState(res);
                else if (req.HttpMethod == "POST")
                    HandlePostAction(req, res);
                else
                    SendError(res, 405, "Method not allowed");
            }
            else
            {
                SendError(res, 404, "Not found");
            }
        }
        catch (Exception ex)
        {
            try { SendError(ctx.Response, 500, $"Internal error: {ex.Message}"); }
            catch { }
        }
    }

    private static void HandleGetState(HttpListenerResponse res)
    {
        try
        {
            var state = RunOnMainThread(() => MenuStateService.BuildState());
            SendJson(res, state);
        }
        catch (Exception ex)
        {
            SendError(res, 500, $"Failed to read state: {ex.Message}");
        }
    }

    private static void HandlePostAction(HttpListenerRequest req, HttpListenerResponse res)
    {
        string body;
        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            body = reader.ReadToEnd();

        Dictionary<string, JsonElement>? parsed;
        try { parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body); }
        catch { SendError(res, 400, "Invalid JSON"); return; }

        if (parsed == null || !parsed.TryGetValue("action", out var actionElem))
        {
            SendError(res, 400, "Missing 'action' field");
            return;
        }

        string action = actionElem.GetString() ?? "";
        int? optionIndex = null;
        if (parsed.TryGetValue("option_index", out var oiElem) && oiElem.ValueKind == JsonValueKind.Number)
            optionIndex = (int)oiElem.GetDouble();

        try
        {
            var result = RunOnMainThread(() => MenuActionService.Execute(action, optionIndex, parsed));
            SendJson(res, result);
        }
        catch (MenuActionException ex)
        {
            SendError(res, ex.StatusCode, ex.Message, ex.Details);
        }
        catch (Exception ex)
        {
            SendError(res, 500, $"Action failed: {ex.Message}");
        }
    }

    internal static void SendJson(HttpListenerResponse res, object data)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOpts);
        res.ContentType = "application/json";
        res.ContentLength64 = bytes.Length;
        res.StatusCode = 200;
        res.OutputStream.Write(bytes, 0, bytes.Length);
        res.Close();
    }

    internal static void SendError(HttpListenerResponse res, int status, string message, Dictionary<string, object>? extra = null)
    {
        var err = new Dictionary<string, object>
        {
            ["status"] = "error",
            ["error"] = message
        };
        if (extra != null)
        {
            foreach (var kv in extra)
                err[kv.Key] = kv.Value;
        }
        SendJson(res, err);
        try { res.StatusCode = status; } catch { }
    }
}

internal class MenuActionException : Exception
{
    public int StatusCode { get; }
    public Dictionary<string, object>? Details { get; }

    public MenuActionException(int statusCode, string message, Dictionary<string, object>? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Details = details;
    }
}
