using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Veya.Services;
using Veya.Models;

namespace Veya.LSP;

class Program
{
    static async Task Main(string[] args)
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        var buffer = new byte[1024 * 1024];

        while (true)
        {
            var length = await ReadHeaderLength(stdin);
            if (length <= 0) break; // EOF

            var bodyBytes = new byte[length];
            var bytesRead = 0;
            while (bytesRead < length)
            {
                var r = await stdin.ReadAsync(bodyBytes, bytesRead, length - bytesRead);
                if (r == 0) break;
                bytesRead += r;
            }

            var requestJson = Encoding.UTF8.GetString(bodyBytes);
            
            try 
            {
                using var doc = JsonDocument.Parse(requestJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("method", out var methodProp)) continue;
                
                var method = methodProp.GetString();
                var id = root.TryGetProperty("id", out var idProp) ? idProp.Clone() : default;

                if (method == "initialize")
                {
                    SendResponse(stdout, id, new {
                        capabilities = new {
                            textDocumentSync = 1 // Full
                        }
                    });
                }
                else if (method == "textDocument/didOpen" || method == "textDocument/didChange")
                {
                    var paramsEl = root.GetProperty("params");
                    var textDocument = paramsEl.GetProperty("textDocument");
                    var uri = textDocument.GetProperty("uri").GetString();
                    var text = "";

                    if (method == "textDocument/didOpen") {
                        text = textDocument.GetProperty("text").GetString();
                    } else {
                        var changes = paramsEl.GetProperty("contentChanges").EnumerateArray();
                        var enumerator = changes.GetEnumerator();
                        if (enumerator.MoveNext()) text = enumerator.Current.GetProperty("text").GetString();
                    }

                    if (uri != null && text != null)
                        AnalyzeAndPublishDiagnostics(stdout, uri, text);
                }
            }
            catch (Exception ex)
            {
                // Ignore silent parsing errors for now
                File.AppendAllText("lsp_error.log", ex.ToString() + "\n");
            }
        }
    }

    private static async Task<int> ReadHeaderLength(Stream input)
    {
        var builder = new StringBuilder();
        var b = new byte[1];
        while (await input.ReadAsync(b, 0, 1) > 0)
        {
            builder.Append((char)b[0]);
            if (builder.ToString().EndsWith("\r\n\r\n"))
            {
                var headerLine = builder.ToString();
                var match = System.Text.RegularExpressions.Regex.Match(headerLine, @"Content-Length: (\d+)");
                if (match.Success) return int.Parse(match.Groups[1].Value);
            }
        }
        return -1;
    }

    private static void SendResponse(Stream stdout, JsonElement id, object result)
    {
        var response = JsonSerializer.Serialize(new {
            jsonrpc = "2.0",
            id = id,
            result = result
        });
        SendMessage(stdout, response);
    }

    private static void SendNotification(Stream stdout, string method, object parameters)
    {
        var n = JsonSerializer.Serialize(new {
            jsonrpc = "2.0",
            method = method,
            @params = parameters
        });
        SendMessage(stdout, n);
    }

    private static void SendMessage(Stream stdout, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var header = Encoding.ASCII.GetBytes($"Content-Length: {bytes.Length}\r\n\r\n");
        stdout.Write(header, 0, header.Length);
        stdout.Write(bytes, 0, bytes.Length);
        stdout.Flush();
    }

    private static void AnalyzeAndPublishDiagnostics(Stream stdout, string uri, string text)
    {
        var diagnostics = new List<object>();

        try
        {
            var lexer = new Lexer();
            var tokens = lexer.Tokenize(text);
            var parser = new Parser(tokens);
            var ast = parser.Parse();
        }
        catch (Exception ex)
        {
            int line = 0;
            if (ex.Message.StartsWith("Riga ")) 
            {
                var parts = ex.Message.Replace("Riga ", "").Split(':');
                if (parts.Length > 0 && int.TryParse(parts[0], out int errLine))
                    line = errLine > 0 ? errLine - 1 : 0; 
            }

            diagnostics.Add(new {
                severity = 1, // Error
                range = new {
                    start = new { line = line, character = 0 },
                    end = new { line = line, character = 100 }
                },
                message = ex.Message,
                source = "Veya"
            });
        }

        SendNotification(stdout, "textDocument/publishDiagnostics", new {
            uri = uri,
            diagnostics = diagnostics
        });
    }
}
