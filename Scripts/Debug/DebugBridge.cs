using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TokuTactics;

/// <summary>
/// Autoload node that runs a lightweight HTTP server on localhost.
/// Exposes the Godot viewport, scene tree, and node properties to external tools (MCP server).
/// Add as an Autoload named "DebugBridge" — only runs in debug/editor builds.
/// </summary>
public partial class DebugBridge : Node
{
    [Export] public int Port { get; set; } = 9880;

    private HttpListener _listener;
    private bool _running;

    // Queue of actions to run on the main thread (Godot is not thread-safe)
    private readonly Queue<Action> _mainThreadQueue = new();
    private readonly object _queueLock = new();

    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
        {
            GD.Print("[DebugBridge] Disabled in release builds.");
            QueueFree();
            return;
        }

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            _running = true;
            Task.Run(ListenLoop);

            // Tail Godot's own log file to capture GD.Print, errors, warnings, engine messages
            InitLogFileTailing();

            GD.Print($"[DebugBridge] Listening on http://localhost:{Port}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[DebugBridge] Failed to start: {ex.Message}");
        }
    }

    public override void _Process(double delta)
    {
        // Drain main-thread work queue
        lock (_queueLock)
        {
            while (_mainThreadQueue.Count > 0)
            {
                _mainThreadQueue.Dequeue().Invoke();
            }
        }
    }

    public override void _ExitTree()
    {
        _running = false;
        _listener?.Stop();
        _listener?.Close();
    }

    // ───────────────────────── HTTP listener ─────────────────────────

    private async Task ListenLoop()
    {
        while (_running)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(ctx));
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) { break; }
            catch (Exception ex)
            {
                GD.PrintErr($"[DebugBridge] Listener error: {ex.Message}");
            }
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";
        try
        {
            switch (path)
            {
                case "/ping":
                    Respond(ctx, 200, new { status = "ok", engine = "Godot", project = ProjectSettings.GetSetting("application/config/name").ToString() });
                    break;

                case "/screenshot":
                    HandleScreenshot(ctx);
                    break;

                case "/scene_tree":
                    HandleSceneTree(ctx);
                    break;

                case "/inspect":
                    HandleInspect(ctx);
                    break;

                case "/logs":
                    HandleLogs(ctx);
                    break;

                case "/input/action":
                    HandleInputAction(ctx);
                    break;

                case "/input/key":
                    HandleInputKey(ctx);
                    break;

                case "/input/mouse":
                    HandleInputMouse(ctx);
                    break;

                case "/call_method":
                    HandleCallMethod(ctx);
                    break;

                case "/wait":
                    HandleWait(ctx);
                    break;

                default:
                    Respond(ctx, 404, new { error = "Not found", available = new[] {
                        "/ping", "/screenshot", "/scene_tree", "/inspect?path=NodePath", "/logs",
                        "/input/action", "/input/key", "/input/mouse", "/call_method", "/wait"
                    } });
                    break;
            }
        }
        catch (Exception ex)
        {
            Respond(ctx, 500, new { error = ex.Message });
        }
    }

    // ───────────────────────── /screenshot ─────────────────────────

    private void HandleScreenshot(HttpListenerContext ctx)
    {
        var tcs = new TaskCompletionSource<(byte[] png, int w, int h)>();

        EnqueueMainThread(() =>
        {
            try
            {
                var viewport = GetViewport();
                if (viewport == null) { tcs.SetException(new Exception("Viewport is null")); return; }
                var tex = viewport.GetTexture();
                if (tex == null) { tcs.SetException(new Exception("Viewport texture is null")); return; }
                var image = tex.GetImage();
                if (image == null) { tcs.SetException(new Exception("Viewport image is null (frame not yet rendered?)")); return; }
                var png = image.SavePngToBuffer();
                tcs.SetResult((png, image.GetWidth(), image.GetHeight()));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });

        try
        {
            var (pngBytes, w, h) = tcs.Task.GetAwaiter().GetResult();
            var wantRaw = ctx.Request.QueryString["format"] == "raw";
            if (wantRaw)
            {
                ctx.Response.ContentType = "image/png";
                ctx.Response.ContentLength64 = pngBytes.Length;
                ctx.Response.OutputStream.Write(pngBytes, 0, pngBytes.Length);
                ctx.Response.Close();
            }
            else
            {
                var base64 = Convert.ToBase64String(pngBytes);
                Respond(ctx, 200, new { format = "png_base64", width = w, height = h, data = base64 });
            }
        }
        catch (Exception ex)
        {
            Respond(ctx, 500, new { error = $"Screenshot failed: {ex.Message}" });
        }
    }

    // ───────────────────────── /scene_tree ─────────────────────────

    private void HandleSceneTree(HttpListenerContext ctx)
    {
        int maxDepth = 6;
        var depthParam = ctx.Request.QueryString["depth"];
        if (depthParam != null && int.TryParse(depthParam, out var d)) maxDepth = d;

        var tcs = new TaskCompletionSource<object>();

        EnqueueMainThread(() =>
        {
            try
            {
                var root = GetTree().CurrentScene;
                var tree = WalkTree(root, 0, maxDepth);
                tcs.SetResult(tree);
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });

        var result = tcs.Task.GetAwaiter().GetResult();
        Respond(ctx, 200, result);
    }

    private Dictionary<string, object> WalkTree(Node node, int depth, int maxDepth)
    {
        var entry = new Dictionary<string, object>
        {
            ["name"] = node.Name.ToString(),
            ["type"] = node.GetClass(),
            ["path"] = node.GetPath().ToString()
        };

        if (node is Node2D n2d)
        {
            entry["position"] = new { x = n2d.Position.X, y = n2d.Position.Y };
            entry["visible"] = n2d.Visible;
        }
        else if (node is Node3D n3d)
        {
            entry["position"] = new { x = n3d.Position.X, y = n3d.Position.Y, z = n3d.Position.Z };
            entry["visible"] = n3d.Visible;
        }
        else if (node is Control ctrl)
        {
            entry["position"] = new { x = ctrl.Position.X, y = ctrl.Position.Y };
            entry["size"] = new { w = ctrl.Size.X, h = ctrl.Size.Y };
            entry["visible"] = ctrl.Visible;
        }

        if (depth < maxDepth && node.GetChildCount() > 0)
        {
            var children = new List<Dictionary<string, object>>();
            for (int i = 0; i < node.GetChildCount(); i++)
            {
                children.Add(WalkTree(node.GetChild(i), depth + 1, maxDepth));
            }
            entry["children"] = children;
        }
        else if (node.GetChildCount() > 0)
        {
            entry["child_count"] = node.GetChildCount();
        }

        return entry;
    }

    // ───────────────────────── /inspect ─────────────────────────

    private void HandleInspect(HttpListenerContext ctx)
    {
        var nodePath = ctx.Request.QueryString["path"];
        if (string.IsNullOrEmpty(nodePath))
        {
            Respond(ctx, 400, new { error = "Missing ?path= parameter" });
            return;
        }

        var tcs = new TaskCompletionSource<object>();

        EnqueueMainThread(() =>
        {
            try
            {
                var node = GetTree().CurrentScene.GetNodeOrNull(nodePath);
                if (node == null)
                {
                    tcs.SetResult(new { error = $"Node not found: {nodePath}" });
                    return;
                }

                var props = new Dictionary<string, object>();
                foreach (var p in node.GetPropertyList())
                {
                    var name = p["name"].AsString();
                    // Skip internal/heavy properties
                    if (name.StartsWith("_") || name == "script") continue;
                    try
                    {
                        var val = node.Get(name);
                        props[name] = val.VariantType switch
                        {
                            Variant.Type.Nil => null,
                            Variant.Type.Bool => val.AsBool(),
                            Variant.Type.Int => val.AsInt64(),
                            Variant.Type.Float => val.AsDouble(),
                            Variant.Type.String => val.AsString(),
                            Variant.Type.Vector2 => new { x = val.AsVector2().X, y = val.AsVector2().Y },
                            Variant.Type.Vector3 => new { x = val.AsVector3().X, y = val.AsVector3().Y, z = val.AsVector3().Z },
                            Variant.Type.Color => new { r = val.AsColor().R, g = val.AsColor().G, b = val.AsColor().B, a = val.AsColor().A },
                            _ => val.ToString()
                        };
                    }
                    catch { props[name] = "<unreadable>"; }
                }

                var info = new Dictionary<string, object>
                {
                    ["name"] = node.Name.ToString(),
                    ["type"] = node.GetClass(),
                    ["path"] = node.GetPath().ToString(),
                    ["script"] = node.GetScript().VariantType != Variant.Type.Nil ? ((Script)node.GetScript()).ResourcePath : null,
                    ["groups"] = GetGroupNames(node),
                    ["signal_connections"] = node.GetSignalConnectionList("").Count,
                    ["properties"] = props
                };

                tcs.SetResult(info);
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });

        var result = tcs.Task.GetAwaiter().GetResult();
        Respond(ctx, 200, result);
    }

    private static List<string> GetGroupNames(Node node)
    {
        var groups = new List<string>();
        foreach (var g in node.GetGroups())
            groups.Add(g.ToString());
        return groups;
    }

    // ───────────────────────── /logs ─────────────────────────

    // Ring buffer for recent log entries
    private static readonly List<LogEntry> _logs = new();
    private static readonly object _logLock = new();
    private const int MaxLogs = 200;

    // Godot log file tailing state
    private string _logFilePath;
    private long _logFilePos;

    private record LogEntry(string Level, string Message, string Timestamp);

    /// <summary>Call from anywhere: DebugBridge.Log("info", "something happened");</summary>
    public static void Log(string level, string message)
    {
        lock (_logLock)
        {
            _logs.Add(new LogEntry(level, message, DateTime.UtcNow.ToString("o")));
            if (_logs.Count > MaxLogs) _logs.RemoveAt(0);
        }
    }

    /// <summary>Initialize log file tailing — seek to end so we only capture new output.</summary>
    private void InitLogFileTailing()
    {
        try
        {
            _logFilePath = ProjectSettings.GlobalizePath("user://logs/godot.log");
            if (File.Exists(_logFilePath))
            {
                _logFilePos = new FileInfo(_logFilePath).Length;
                GD.Print($"[DebugBridge] Tailing log file: {_logFilePath}");
            }
            else
            {
                GD.Print($"[DebugBridge] Log file not found: {_logFilePath} — /logs will only show explicit DebugBridge.Log() calls");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[DebugBridge] Log file init failed: {ex.Message}");
        }
    }

    /// <summary>Read any new lines from the Godot log file and append to ring buffer.</summary>
    private void PollLogFile()
    {
        if (_logFilePath == null || !File.Exists(_logFilePath)) return;
        try
        {
            var fi = new FileInfo(_logFilePath);
            if (fi.Length <= _logFilePos)
            {
                // File was rotated or truncated — reset
                if (fi.Length < _logFilePos) _logFilePos = 0;
                return;
            }

            using var fs = new FileStream(_logFilePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(_logFilePos, SeekOrigin.Begin);
            using var reader = new StreamReader(fs, Encoding.UTF8);
            var timestamp = DateTime.UtcNow.ToString("o");
            while (reader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var level = ClassifyLogLine(line);
                lock (_logLock)
                {
                    _logs.Add(new LogEntry(level, line, timestamp));
                    if (_logs.Count > MaxLogs) _logs.RemoveAt(0);
                }
            }
            _logFilePos = fs.Position;
        }
        catch { /* best-effort — don't crash the bridge */ }
    }

    private static string ClassifyLogLine(string line)
    {
        if (line.Contains("ERROR") || line.Contains("SCRIPT ERROR") || line.StartsWith("E "))
            return "error";
        if (line.Contains("WARNING") || line.Contains("SCRIPT WARNING") || line.StartsWith("W "))
            return "warning";
        return "print";
    }

    private void HandleLogs(HttpListenerContext ctx)
    {
        // Poll for new log file content before responding
        PollLogFile();

        var sinceParam = ctx.Request.QueryString["since"];
        List<LogEntry> snapshot;
        lock (_logLock) { snapshot = new List<LogEntry>(_logs); }

        if (sinceParam != null && DateTime.TryParse(sinceParam, out var since))
            snapshot = snapshot.FindAll(l => DateTime.Parse(l.Timestamp) > since);

        Respond(ctx, 200, new { count = snapshot.Count, logs = snapshot });
    }

    // ───────────────────────── /input/action ─────────────────────────

    /// <summary>Simulate a Godot input action (the ones defined in Project → Input Map).</summary>
    private void HandleInputAction(HttpListenerContext ctx)
    {
        var body = ReadJsonBody(ctx);
        var action = body.GetValueOrDefault("action")?.ToString();
        var pressed = body.TryGetValue("pressed", out var p) && p is JsonElement pe ? pe.GetBoolean() : true;
        var strength = body.TryGetValue("strength", out var s) && s is JsonElement se ? se.GetSingle() : 1.0f;

        if (string.IsNullOrEmpty(action))
        {
            Respond(ctx, 400, new { error = "Missing 'action' field" });
            return;
        }

        var tcs = new TaskCompletionSource<bool>();
        EnqueueMainThread(() =>
        {
            try
            {
                if (!InputMap.HasAction(action))
                {
                    tcs.SetException(new Exception($"Unknown action: '{action}'. Check Project → Input Map."));
                    return;
                }

                var ev = new InputEventAction { Action = action, Pressed = pressed, Strength = strength };
                Input.ParseInputEvent(ev);

                // If it was a press, auto-release after one frame unless caller said pressed=false
                if (pressed)
                {
                    GetTree().CreateTimer(0.05).Timeout += () =>
                    {
                        var release = new InputEventAction { Action = action, Pressed = false };
                        Input.ParseInputEvent(release);
                    };
                }

                tcs.SetResult(true);
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });

        try
        {
            tcs.Task.GetAwaiter().GetResult();
            Respond(ctx, 200, new { ok = true, action, pressed });
        }
        catch (Exception ex)
        {
            Respond(ctx, 400, new { error = ex.Message });
        }
    }

    // ───────────────────────── /input/key ─────────────────────────

    /// <summary>Simulate a keyboard key press/release.</summary>
    private void HandleInputKey(HttpListenerContext ctx)
    {
        var body = ReadJsonBody(ctx);
        var keyName = body.GetValueOrDefault("key")?.ToString()?.ToUpper();
        var pressed = body.TryGetValue("pressed", out var p) && p is JsonElement pe ? pe.GetBoolean() : true;
        var duration = body.TryGetValue("duration_ms", out var d) && d is JsonElement de ? de.GetInt32() : 50;

        if (string.IsNullOrEmpty(keyName))
        {
            Respond(ctx, 400, new { error = "Missing 'key' field (e.g. 'SPACE', 'A', 'ENTER')" });
            return;
        }

        if (!Enum.TryParse<Key>(keyName, true, out var keycode))
        {
            Respond(ctx, 400, new { error = $"Unknown key: '{keyName}'. Use Godot Key enum names." });
            return;
        }

        var tcs = new TaskCompletionSource<bool>();
        EnqueueMainThread(() =>
        {
            try
            {
                var ev = new InputEventKey { Keycode = keycode, Pressed = pressed, PhysicalKeycode = keycode };
                Input.ParseInputEvent(ev);

                if (pressed)
                {
                    GetTree().CreateTimer(duration / 1000.0).Timeout += () =>
                    {
                        var release = new InputEventKey { Keycode = keycode, Pressed = false, PhysicalKeycode = keycode };
                        Input.ParseInputEvent(release);
                    };
                }

                tcs.SetResult(true);
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });

        try
        {
            tcs.Task.GetAwaiter().GetResult();
            Respond(ctx, 200, new { ok = true, key = keyName, pressed, duration_ms = duration });
        }
        catch (Exception ex)
        {
            Respond(ctx, 500, new { error = ex.Message });
        }
    }

    // ───────────────────────── /input/mouse ─────────────────────────

    /// <summary>Simulate mouse movement, clicks, or both.</summary>
    private void HandleInputMouse(HttpListenerContext ctx)
    {
        var body = ReadJsonBody(ctx);
        var x = body.TryGetValue("x", out var xv) && xv is JsonElement xe ? xe.GetSingle() : -1f;
        var y = body.TryGetValue("y", out var yv) && yv is JsonElement ye ? ye.GetSingle() : -1f;
        var button = body.TryGetValue("button", out var bv) && bv is JsonElement be ? be.GetString() : null;
        var doubleClick = body.TryGetValue("double_click", out var dc) && dc is JsonElement dce && dce.GetBoolean();

        var tcs = new TaskCompletionSource<bool>();
        EnqueueMainThread(() =>
        {
            try
            {
                var pos = new Vector2(x, y);

                // Warp mouse if position given
                if (x >= 0 && y >= 0)
                    Input.WarpMouse(pos);

                // Click if button specified
                if (!string.IsNullOrEmpty(button))
                {
                    var btn = button.ToLower() switch
                    {
                        "left" => MouseButton.Left,
                        "right" => MouseButton.Right,
                        "middle" => MouseButton.Middle,
                        _ => MouseButton.Left
                    };

                    var press = new InputEventMouseButton
                    {
                        ButtonIndex = btn,
                        Pressed = true,
                        Position = pos,
                        GlobalPosition = pos,
                        DoubleClick = doubleClick
                    };
                    Input.ParseInputEvent(press);

                    GetTree().CreateTimer(0.05).Timeout += () =>
                    {
                        var release = new InputEventMouseButton
                        {
                            ButtonIndex = btn,
                            Pressed = false,
                            Position = pos,
                            GlobalPosition = pos
                        };
                        Input.ParseInputEvent(release);
                    };
                }

                tcs.SetResult(true);
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });

        try
        {
            tcs.Task.GetAwaiter().GetResult();
            Respond(ctx, 200, new { ok = true, x, y, button, double_click = doubleClick });
        }
        catch (Exception ex)
        {
            Respond(ctx, 500, new { error = ex.Message });
        }
    }

    // ───────────────────────── /call_method ─────────────────────────

    /// <summary>
    /// Call a method on any node in the scene tree. This is the most powerful
    /// integration testing primitive — Claude can invoke game logic directly.
    /// POST { "path": ".", "method": "StartBattle", "args": [...] }
    /// </summary>
    private void HandleCallMethod(HttpListenerContext ctx)
    {
        var body = ReadJsonBody(ctx);
        var nodePath = body.GetValueOrDefault("path")?.ToString();
        var method = body.GetValueOrDefault("method")?.ToString();

        if (string.IsNullOrEmpty(nodePath) || string.IsNullOrEmpty(method))
        {
            Respond(ctx, 400, new { error = "Missing 'path' and/or 'method' fields" });
            return;
        }

        // Parse args — supports simple JSON types
        var args = new Godot.Collections.Array();
        if (body.TryGetValue("args", out var argsVal) && argsVal is JsonElement argsEl && argsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var arg in argsEl.EnumerateArray())
            {
                args.Add(JsonElementToVariant(arg));
            }
        }

        var tcs = new TaskCompletionSource<object>();
        EnqueueMainThread(() =>
        {
            try
            {
                var node = GetTree().CurrentScene.GetNodeOrNull(nodePath);
                if (node == null)
                {
                    tcs.SetResult(new { error = $"Node not found: {nodePath}" });
                    return;
                }

                if (!node.HasMethod(method))
                {
                    tcs.SetResult(new { error = $"Node '{nodePath}' ({node.GetClass()}) has no method '{method}'" });
                    return;
                }

                var result = node.Callv(method, args);
                var converted = VariantToSerializable(result);
                tcs.SetResult(new { ok = true, path = nodePath, method, result = converted });
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });

        try
        {
            var result = tcs.Task.GetAwaiter().GetResult();
            Respond(ctx, 200, result);
        }
        catch (Exception ex)
        {
            Respond(ctx, 500, new { error = ex.Message });
        }
    }

    // ───────────────────────── /wait ─────────────────────────

    /// <summary>Wait N milliseconds (let the game advance), then return. Useful between actions.</summary>
    private void HandleWait(HttpListenerContext ctx)
    {
        var msParam = ctx.Request.QueryString["ms"] ?? "500";
        if (!int.TryParse(msParam, out var ms)) ms = 500;
        ms = Math.Clamp(ms, 16, 10000);

        System.Threading.Thread.Sleep(ms);
        Respond(ctx, 200, new { ok = true, waited_ms = ms });
    }

    // ───────────────────────── JSON / Variant helpers ─────────────────────────

    private static Dictionary<string, object> ReadJsonBody(HttpListenerContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
        var raw = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(raw)) return new Dictionary<string, object>();

        var doc = JsonDocument.Parse(raw);
        var dict = new Dictionary<string, object>();
        foreach (var prop in doc.RootElement.EnumerateObject())
            dict[prop.Name] = prop.Value;
        return dict;
    }

    private static Variant JsonElementToVariant(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => Variant.From(el.GetString()),
            JsonValueKind.Number when el.TryGetInt64(out var l) => Variant.From(l),
            JsonValueKind.Number => Variant.From(el.GetDouble()),
            JsonValueKind.True => Variant.From(true),
            JsonValueKind.False => Variant.From(false),
            JsonValueKind.Null => default,
            _ => Variant.From(el.ToString())
        };
    }

    private static object VariantToSerializable(Variant v)
    {
        return v.VariantType switch
        {
            Variant.Type.Nil => null,
            Variant.Type.Bool => v.AsBool(),
            Variant.Type.Int => v.AsInt64(),
            Variant.Type.Float => v.AsDouble(),
            Variant.Type.String => v.AsString(),
            Variant.Type.Vector2 => new { x = v.AsVector2().X, y = v.AsVector2().Y },
            Variant.Type.Vector3 => new { x = v.AsVector3().X, y = v.AsVector3().Y, z = v.AsVector3().Z },
            _ => v.ToString()
        };
    }

    // ───────────────────────── Helpers ─────────────────────────

    private void EnqueueMainThread(Action action)
    {
        lock (_queueLock) { _mainThreadQueue.Enqueue(action); }
    }

    private static void Respond(HttpListenerContext ctx, int status, object body)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        ctx.Response.Close();
    }

}
