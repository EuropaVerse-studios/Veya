using System;
using System.IO;
using System.Linq;
using System.Diagnostics;

using Veya.Services;

namespace Veya.CLI;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        var cmd = args[0].ToLowerInvariant();
        try
        {
            switch (cmd)
            {
                case "new":
                    if (args.Length < 2) throw new Exception("Nome progetto mancante. Uso: veya new <nome_progetto>");
                    CreateNewProject(args[1]);
                    break;
                case "build":
                    BuildProject();
                    break;
                case "run":
                    RunProject();
                    break;
                case "parse":
                    ParseProject();
                    break;
                case "doctor":
                    Console.WriteLine("[v doctor] Veya environment is correctly configured.");
                    break;
                default:
                    Console.WriteLine($"Comando sconosciuto: {cmd}");
                    PrintUsage();
                    break;
            }
        }
        catch (Exception ex)
        {
            PrintError(ex.Message);
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Veya Package Manager (v0.1.0)");
        Console.WriteLine("Uso: veya <comando> [opzioni]");
        Console.WriteLine("\nComandi disponibili:");
        Console.WriteLine("  new <nome>    Crea un nuovo progetto Veya");
        Console.WriteLine("  build         Compila il progetto corrente (legge veya.toml)");
        Console.WriteLine("  run           Compila ed esegue il progetto corrente");
        Console.WriteLine("  parse         Effettua solo il Lexing e Parsing del codice (dry-run)");
        Console.WriteLine("  doctor        Controlla l'installazione e l'ambiente");
    }

    static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[Errore di Compilazione]");
        Console.ResetColor();

        Console.WriteLine(message);

        // Simple did-you-mean for common syntax issues
        if (message.Contains("Carattere inatteso")) {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("SUGGERIMENTO: Verifica la sintassi, possibile parentesi mancante o carattere non valido.");
            Console.ResetColor();
        }
        else if (message.Contains("Trovato Eof")) {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("SUGGERIMENTO: Raggiunta la fine del file anticipatamente. Possibile blocco '}' non chiuso.");
            Console.ResetColor();
        }
    }

    static void CreateNewProject(string name)
    {
        var targetDir = Path.Combine(Directory.GetCurrentDirectory(), name);
        if (Directory.Exists(targetDir))
            throw new Exception($"La directory '{name}' esiste già.");

        Directory.CreateDirectory(targetDir);
        Directory.CreateDirectory(Path.Combine(targetDir, "src"));

        var tomlContent = "[package]\n" +
                          $"name = \"{name}\"\n" +
                          "version = \"0.1.0\"\n" +
                          "authors = [\"autore\"]\n\n" +
                          "[dependencies]\n" +
                          "# Aggiungi librerie qui\n";

        File.WriteAllText(Path.Combine(targetDir, "veya.toml"), tomlContent);

        var mainSrc = "function main() {\n" +
                      $"    print(\"Benvenuto in Veya, progetto: {name}!\")\n" +
                      "}\n";

        File.WriteAllText(Path.Combine(targetDir, "src", "main.veya"), mainSrc);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Success] Progetto '{name}' creato con successo.");
        Console.ResetColor();
        Console.WriteLine($"\nPer avviare il progetto:\n  cd {name}\n  veya run");
    }

    static (string Name, string MainFile) ReadManifest()
    {
        var tomlPath = Path.Combine(Directory.GetCurrentDirectory(), "veya.toml");
        if (!File.Exists(tomlPath))
            throw new Exception("File 'veya.toml' non trovato. Sei nella radice di un progetto Veya?");

        var tomlSource = File.ReadAllText(tomlPath);
        
        // Estrattore semplice Regex-based (Zero Dipendenze)
        string name = "output";
        var match = System.Text.RegularExpressions.Regex.Match(tomlSource, @"name\s*=\s*""([^""]+)""");
        if (match.Success) 
        {
            name = match.Groups[1].Value;
        }

        var mainFile = Path.Combine(Directory.GetCurrentDirectory(), "src", "main.veya");
        if (!File.Exists(mainFile))
            throw new Exception($"Entry point non trovato: '{mainFile}' manca.");

        return (name, mainFile);
    }

    static void ParseProject()
    {
        var manifest = ReadManifest();
        var name = manifest.Name;
        var mainFile = manifest.MainFile;
        Console.WriteLine($"[Parsing] Controllo sintassi per '{name}'...");

        var sourceCode = File.ReadAllText(mainFile);

        var lexer = new Lexer();
        var tokens = lexer.Tokenize(sourceCode);
        
        var parser = new Parser(tokens);
        var ast = parser.Parse();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[Success] Sintassi valida! AST generato con successo.");
        Console.ResetColor();
    }

    static string BuildProject()
    {
        var manifest = ReadManifest();
        var name = manifest.Name;
        var mainFile = manifest.MainFile;
        Console.WriteLine($"[Building] Compilazione pacchetto '{name}'...");

        var sourceCode = File.ReadAllText(mainFile);

        var lexer = new Lexer();
        var tokens = lexer.Tokenize(sourceCode);
        
        var parser = new Parser(tokens);
        var ast = parser.Parse();

        var codegen = new Codegen();
        var cppCode = codegen.Generate(ast);

        var buildDir = Path.Combine(Directory.GetCurrentDirectory(), "build");
        Directory.CreateDirectory(buildDir);
        
        var cppFile = Path.Combine(buildDir, $"{name}.cpp");
        File.WriteAllText(cppFile, cppCode);

        var exeName = Path.Combine(buildDir, name + (OperatingSystem.IsWindows() ? ".exe" : ""));
        var args = $"-std=c++23 \"{cppFile}\" -o \"{exeName}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = "g++",
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        process!.WaitForExit();

        if (process.ExitCode != 0)
        {
            var err = process.StandardError.ReadToEnd();
            throw new Exception($"Generazione C++ fallita:\n{err}");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Success] Eseguibile generato: build/{Path.GetFileName(exeName)}");
        Console.ResetColor();

        return exeName;
    }

    static void RunProject()
    {
        var exePath = BuildProject();
        Console.WriteLine($"\n[Running]");
        
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false
        });
        process!.WaitForExit();
    }
}
