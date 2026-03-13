using System.Text;
using Veya.Models;

namespace Veya.Services;

public class Codegen
{
    private StringBuilder _sb = new();
    private int _indent = 0;

    private void Line(string line = "")
    {
        if (line.Length > 0)
            _sb.Append(new string(' ', _indent * 4)).AppendLine(line);
        else
            _sb.AppendLine();
    }

    public string Generate(ProgramNode program)
    {
        Line("// === GENERATO DAL COMPILATORE VEYA (C# -> C++23) ===");
        Line("#include <iostream>");
        Line("#include <print>");
        Line("#include <string>");
        Line("#include <vector>");
        Line("#include <optional>");
        Line("#include <thread>");
        Line("#include <chrono>");
        Line("");
        EmitRuntime();

        foreach (var item in program.Items) EmitTopLevel(item);

        return _sb.ToString();
    }

    private void EmitRuntime()
    {
        Line("template<typename... Args>");
        Line("void veya_print(Args&&... args) {");
        _indent++;
        Line("((std::cout << std::forward<Args>(args)), ...);");
        Line("std::cout << '\\n';");
        _indent--;
        Line("}\n");

        Line("void veya_sleep(uint64_t ms) {");
        _indent++;
        Line("std::this_thread::sleep_for(std::chrono::milliseconds(ms));");
        _indent--;
        Line("}\n");
    }

    private void EmitTopLevel(TopLevelItem item)
    {
        switch (item)
        {
            case FunctionDef f: EmitFunction(f); break;
            case StructDef s: EmitStruct(s); break;
            case EnumDef e: EmitEnum(e); break;
        }
    }

    private void EmitFunction(FunctionDef f)
    {
        var args = string.Join(", ", f.Params.Select(p => $"{CppType(p.Type)} {p.Name}"));
        var ret = f.ReturnType != null ? CppType(f.ReturnType) : "void";
        
        // Se la funzione si chiama `main`, in C++ deve restituire `int` per la entry point standard
        if (f.Name == "main") ret = "int";

        Line($"{ret} {f.Name}({args}) {{");
        _indent++;
        foreach (var stmt in f.Body) EmitStmt(stmt);
        if (f.Name == "main") Line("return 0;");
        _indent--;
        Line("}\n");
    }

    private void EmitStruct(StructDef s)
    {
        Line($"struct {s.Name} {{");
        _indent++;
        foreach (var f in s.Fields) Line($"{CppType(f.Type)} {f.Name};");
        _indent--;
        Line("};\n");
    }

    private void EmitEnum(EnumDef e)
    {
        Line($"enum class {e.Name} {{");
        _indent++;
        Line(string.Join(", ", e.Variants));
        _indent--;
        Line("};\n");
    }

    private void EmitStmt(Statement stmt)
    {
        switch (stmt)
        {
            case VarDecl v:
                var t = v.DeclaredType != null ? CppType(v.DeclaredType) : "auto";
                var c = v.IsMutable ? "" : "const ";
                Line($"{c}{t} {v.Name} = {EmitExpr(v.Value)};");
                break;
            case Assignment a:
                Line($"{EmitExpr(a.Target)} = {EmitExpr(a.Value)};");
                break;
            case CompoundAssign ca:
                Line($"{ca.Target} {ca.Op} {EmitExpr(ca.Value)};");
                break;
            case ReturnStmt r:
                Line(r.Value != null ? $"return {EmitExpr(r.Value)};" : "return;");
                break;
            case ExprStatement e:
                Line($"{EmitExpr(e.Expression)};");
                break;
            case IfStmt i:
                Line($"if ({EmitExpr(i.Condition)}) {{");
                _indent++; foreach (var s in i.ThenBody) EmitStmt(s); _indent--;
                if (i.ElseBody != null) {
                    Line("} else {");
                    _indent++; foreach (var s in i.ElseBody) EmitStmt(s); _indent--;
                }
                Line("}");
                break;
            case WhileStmt w:
                Line($"while ({EmitExpr(w.Condition)}) {{");
                _indent++; foreach (var s in w.Body) EmitStmt(s); _indent--;
                Line("}");
                break;
            case ForRangeStmt f:
                Line($"for (int64_t {f.VarName} = {EmitExpr(f.Start)}; {f.VarName} < {EmitExpr(f.End)}; ++{f.VarName}) {{");
                _indent++; foreach (var s in f.Body) EmitStmt(s); _indent--;
                Line("}");
                break;
        }
    }

    private string EmitExpr(Expr expr) => expr switch
    {
        IntLiteral i => $"{i.Value}LL",
        FloatLiteral f => $"{f.Value}",
        StringLiteral s => $"std::string(\"{s.Value}\")",
        BoolLiteral b => b.Value ? "true" : "false",
        Identifier id => id.Name,
        BinaryOp b => $"({EmitExpr(b.Left)} {b.Op} {EmitExpr(b.Right)})",
        FunctionCall c => EmitCall(c.Name, c.Args),
        MethodCall m => EmitMethod(m),
        _ => "/* Unsupported */"
    };

    private string EmitCall(string name, List<Expr> args)
    {
        var a = args.Select(EmitExpr).ToList();
        if (name == "print")
        {
            return $"veya_print({string.Join(", ", a)})";
        }
        return $"{name}({string.Join(", ", a)})";
    }

    private string EmitMethod(MethodCall m)
    {
        if (m.Object is Identifier id && id.Name == "io" && m.Method == "print")
            return EmitCall("print", m.Args);
        return $"{EmitExpr(m.Object)}.{m.Method}({string.Join(", ", m.Args.Select(EmitExpr))})";
    }

    private string CppType(VeyaType t) => t switch
    {
        IntType => "int64_t", FloatType => "double", StringType => "std::string",
        BoolType => "bool", NamedType n => n.Name,
        OptionType o => $"std::optional<{CppType(o.Inner)}>",
        _ => "void"
    };
}
