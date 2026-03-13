using System;
using System.Collections.Generic;
using Veya.Models;

namespace Veya.Services;

public class Parser
{
    private readonly List<Token> _tokens;
    private int _pos = 0;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    private TokenKind Current => _pos < _tokens.Count ? _tokens[_pos].Kind : TokenKind.Eof;
    private int Line => _pos < _tokens.Count ? _tokens[_pos].Line : 0;

    private Token Advance() => _tokens[_pos++];

    private void Expect(TokenKind kind)
    {
        if (Current == kind) Advance();
        else throw new Exception($"Riga {Line}: Atteso {kind}, trovato {Current}");
    }

    private string ExpectId()
    {
        if (Current == TokenKind.Identifier) return (string)Advance().Value!;
        throw new Exception($"Riga {Line}: Atteso identificatore, trovato {Current}");
    }

    private void SkipNewlines()
    {
        while (Current == TokenKind.Newline) Advance();
    }

    public ProgramNode Parse()
    {
        var items = new List<TopLevelItem>();
        SkipNewlines();
        while (Current != TokenKind.Eof)
        {
            items.Add(ParseTopLevelItem());
            SkipNewlines();
        }
        return new ProgramNode(items);
    }

    private TopLevelItem ParseTopLevelItem()
    {
        return Current switch
        {
            TokenKind.Import => ParseImport(),
            TokenKind.Function => ParseFunction(false, false),
            TokenKind.Async => ParseAsyncFunction(),
            TokenKind.Task => ParseTaskFunction(),
            TokenKind.Struct => ParseStruct(),
            TokenKind.Enum => ParseEnum(),
            _ => throw new Exception($"Riga {Line}: Elemento non valido {Current}")
        };
    }

    private ImportDecl ParseImport()
    {
        Expect(TokenKind.Import);
        var path = new List<string> { ExpectId() };
        while (Current == TokenKind.Dot)
        {
            Advance();
            path.Add(ExpectId());
        }
        return new ImportDecl(path);
    }

    private FunctionDef ParseAsyncFunction()
    {
        Expect(TokenKind.Async);
        return ParseFunction(true, false);
    }

    private FunctionDef ParseTaskFunction()
    {
        Expect(TokenKind.Task);
        return ParseFunction(true, true); // Treat task as an async operation
    }

    private FunctionDef ParseFunction(bool isAsync, bool isTask)
    {
        if (isAsync || isTask)
        {
            if (Current == TokenKind.Function) Advance();
        }
        else
        {
            Expect(TokenKind.Function);
        }
        var name = ExpectId();
        Expect(TokenKind.LParen);
        var parameters = new List<Param>();
        while (Current != TokenKind.RParen)
        {
            if (parameters.Count > 0) Expect(TokenKind.Comma);
            var pName = ExpectId();
            Expect(TokenKind.Colon);
            parameters.Add(new Param(pName, ParseType()));
        }
        Expect(TokenKind.RParen);
        VeyaType? retType = null;
        if (Current == TokenKind.Arrow)
        {
            Advance();
            retType = ParseType();
        }
        SkipNewlines();
        return new FunctionDef(name, isAsync, parameters, retType, ParseBlock());
    }

    private StructDef ParseStruct()
    {
        Expect(TokenKind.Struct);
        var name = ExpectId();
        SkipNewlines();
        Expect(TokenKind.LBrace);
        var fields = new List<FieldDef>();
        while (Current != TokenKind.RBrace && Current != TokenKind.Eof)
        {
            var fName = ExpectId();
            Expect(TokenKind.Colon);
            fields.Add(new FieldDef(fName, ParseType()));
            SkipNewlines();
        }
        if (Current == TokenKind.RBrace) Advance();
        return new StructDef(name, fields);
    }

    private EnumDef ParseEnum()
    {
        Expect(TokenKind.Enum);
        var name = ExpectId();
        SkipNewlines();
        Expect(TokenKind.LBrace);
        var variants = new List<string>();
        while (Current != TokenKind.RBrace && Current != TokenKind.Eof)
        {
            variants.Add(ExpectId());
            SkipNewlines();
        }
        if (Current == TokenKind.RBrace) Advance();
        return new EnumDef(name, variants);
    }

    private VeyaType ParseType()
    {
        var kind = Current;
        Advance();
        var type = kind switch
        {
            TokenKind.TypeInt => (VeyaType)new IntType(),
            TokenKind.TypeFloat => new FloatType(),
            TokenKind.TypeBool => new BoolType(),
            TokenKind.TypeChar => new CharType(),
            TokenKind.TypeString => new StringType(),
            TokenKind.TypeOption => ParseGeneric(t => new OptionType(t)),
            TokenKind.TypeList => ParseGeneric(t => new ListType(t)),
            TokenKind.TypeResult => ParseGeneric2((ok, err) => new ResultType(ok, err)),
            TokenKind.TypeMap => ParseGeneric2((k, v) => new MapType(k, v)),
            TokenKind.TypeDict => ParseGeneric2((k, v) => new DictType(k, v)),
            TokenKind.Identifier => new NamedType((string)_tokens[_pos - 1].Value!),
            _ => throw new Exception($"Riga {Line}: Tipo non valido {kind}")
        };
        return type;
    }

    private VeyaType ParseGeneric(Func<VeyaType, VeyaType> creator)
    {
        Expect(TokenKind.LBracket);
        var inner = ParseType();
        Expect(TokenKind.RBracket);
        return creator(inner);
    }

    private VeyaType ParseGeneric2(Func<VeyaType, VeyaType, VeyaType> creator)
    {
        Expect(TokenKind.LBracket);
        var type1 = ParseType();
        Expect(TokenKind.Comma);
        var type2 = ParseType();
        Expect(TokenKind.RBracket);
        return creator(type1, type2);
    }

    private List<Statement> ParseBlock()
    {
        Expect(TokenKind.LBrace);
        var stmts = new List<Statement>();
        while (Current != TokenKind.RBrace && Current != TokenKind.Eof)
        {
            SkipNewlines();
            if (Current == TokenKind.RBrace) break;
            stmts.Add(ParseStatement());
            SkipNewlines();
        }
        if (Current == TokenKind.RBrace) Advance();
        return stmts;
    }

    private Statement ParseStatement()
    {
        return Current switch
        {
            TokenKind.Return => ParseReturn(),
            TokenKind.If => ParseIf(),
            TokenKind.For => ParseFor(),
            TokenKind.While => ParseWhile(),
            TokenKind.Mut => ParseMutDecl(),
            TokenKind.Unsafe => ParseUnsafeBlock(),
            _ => ParseExprOrAssign()
        };
    }

    private Statement ParseUnsafeBlock()
    {
        Expect(TokenKind.Unsafe);
        SkipNewlines();
        return new UnsafeBlock(ParseBlock());
    }

    private Statement ParseReturn()
    {
        Advance();
        if (Current == TokenKind.Newline || Current == TokenKind.Dedent || Current == TokenKind.Eof)
            return new ReturnStmt(null);
        return new ReturnStmt(ParseExpr());
    }

    private Statement ParseIf()
    {
        Advance();
        var cond = ParseExpr();
        if (Current == TokenKind.Is)
        {
            Advance();
            bool isSome = Advance().Kind == TokenKind.Some;
            Expect(TokenKind.LParen);
            var name = isSome ? ExpectId() : "";
            Expect(TokenKind.RParen);
            SkipNewlines();
            var then = ParseBlock();
            List<Statement>? @else = null;
            if (Current == TokenKind.Else)
            {
                Advance(); SkipNewlines();
                @else = ParseBlock();
            }
            return new IfIsStmt(cond, isSome, name, then, @else);
        }
        SkipNewlines();
        var thenBody = ParseBlock();
        List<Statement>? elseBody = null;
        if (Current == TokenKind.Else)
        {
            Advance(); SkipNewlines();
            elseBody = ParseBlock();
        }
        return new IfStmt(cond, thenBody, elseBody);
    }

    private Statement ParseFor()
    {
        Advance();
        var varName = ExpectId();
        Expect(TokenKind.In);
        var start = ParseExpr();
        Expect(TokenKind.Range);
        var end = ParseExpr();
        SkipNewlines();
        return new ForRangeStmt(varName, start, end, ParseBlock());
    }

    private Statement ParseWhile()
    {
        Advance();
        var cond = ParseExpr();
        SkipNewlines();
        return new WhileStmt(cond, ParseBlock());
    }

    private Statement ParseMutDecl()
    {
        Advance();
        var name = ExpectId();
        VeyaType? type = null;
        if (Current == TokenKind.Colon)
        {
            Advance();
            type = ParseType();
        }
        Expect(TokenKind.Assign);
        return new VarDecl(name, true, type, ParseExpr());
    }

    private Statement ParseExprOrAssign()
    {
        if (Current == TokenKind.Identifier)
        {
            int startPos = _pos;
            var name = ExpectId();
            if (Current == TokenKind.Colon)
            {
                Advance();
                var type = ParseType();
                Expect(TokenKind.Assign);
                return new VarDecl(name, false, type, ParseExpr());
            }
            if (Current == TokenKind.Assign)
            {
                Advance();
                return new VarDecl(name, false, null, ParseExpr());
            }
            if (Current == TokenKind.PlusAssign || Current == TokenKind.MinusAssign)
            {
                var op = Advance().Kind == TokenKind.PlusAssign ? "+=" : "-=";
                return new CompoundAssign(name, op, ParseExpr());
            }
            _pos = startPos;
        }
        return new ExprStatement(ParseExpr());
    }

    private Expr ParseExpr() => ParseBinary(0);

    private Expr ParseBinary(int level)
    {
        if (level > 4) return ParsePostfix();
        var left = ParseBinary(level + 1);
        while (IsOpAtLevel(level, out string? op))
        {
            Advance();
            left = new BinaryOp(left, op!, ParseBinary(level + 1));
        }
        return left;
    }

    private bool IsOpAtLevel(int level, out string? op)
    {
        op = null;
        if (level == 0 && (Current == TokenKind.Or)) { op = "||"; return true; }
        if (level == 1 && (Current == TokenKind.And)) { op = "&&"; return true; }
        if (level == 2 && Current is TokenKind.Equal or TokenKind.NotEqual or TokenKind.Less or TokenKind.Greater)
        {
            op = Current switch { TokenKind.Equal => "==", TokenKind.NotEqual => "!=", TokenKind.Less => "<", _ => ">" };
            return true;
        }
        if (level == 3 && (Current == TokenKind.Plus || Current == TokenKind.Minus))
        {
            op = Current == TokenKind.Plus ? "+" : "-"; return true;
        }
        if (level == 4 && (Current == TokenKind.Star || Current == TokenKind.Slash))
        {
            op = Current == TokenKind.Star ? "*" : "/"; return true;
        }
        return false;
    }

    private Expr ParsePostfix()
    {
        var expr = ParsePrimary();
        while (Current == TokenKind.Dot)
        {
            Advance();
            var member = ExpectId();
            if (Current == TokenKind.LParen)
                expr = new MethodCall(expr, member, ParseArgs());
            else
                expr = new FieldAccess(expr, member);
        }
        return expr;
    }

    private Expr ParsePrimary()
    {
        return Current switch
        {
            TokenKind.IntLiteral => new IntLiteral((long)Advance().Value!),
            TokenKind.FloatLiteral => new FloatLiteral((double)Advance().Value!),
            TokenKind.StringLiteral => new StringLiteral((string)Advance().Value!),
            TokenKind.True => (Advance() != null ? new BoolLiteral(true) : null)!,
            TokenKind.False => (Advance() != null ? new BoolLiteral(false) : null)!,
            TokenKind.Identifier => ParseIdOrCall(),
            TokenKind.LParen => ParseParenExpr(),
            TokenKind.Some => ParseSome(),
            TokenKind.None => (Advance() != null ? new OptionExpr(false, null) : null)!,
            TokenKind.Ok => ParseOk(),
            TokenKind.Err => ParseErr(),
            TokenKind.Spawn => ParseSpawn(),
            TokenKind.Await => ParseAwait(),
            _ => throw new Exception($"Riga {Line}: Espressione non valida {Current}")
        };
    }

    private Expr ParseOk()
    {
        Advance(); Expect(TokenKind.LParen);
        var val = ParseExpr();
        Expect(TokenKind.RParen);
        return new ResultExpr(true, val);
    }

    private Expr ParseErr()
    {
        Advance(); Expect(TokenKind.LParen);
        var val = ParseExpr();
        Expect(TokenKind.RParen);
        return new ResultExpr(false, val);
    }

    private Expr ParseSpawn()
    {
        Advance();
        var call = ParseExpr(); // should be FunctionCall
        return new SpawnExpr(call);
    }

    private Expr ParseAwait()
    {
        Advance();
        var task = ParseExpr();
        return new AwaitExpr(task);
    }

    private Expr ParseIdOrCall()
    {
        var name = ExpectId();
        if (Current == TokenKind.LParen) return new FunctionCall(name, ParseArgs());
        return new Identifier(name);
    }

    private List<Expr> ParseArgs()
    {
        Expect(TokenKind.LParen);
        var args = new List<Expr>();
        while (Current != TokenKind.RParen)
        {
            if (args.Count > 0) Expect(TokenKind.Comma);
            args.Add(ParseExpr());
        }
        Expect(TokenKind.RParen);
        return args;
    }

    private Expr ParseParenExpr()
    {
        Expect(TokenKind.LParen);
        var e = ParseExpr();
        Expect(TokenKind.RParen);
        return e;
    }

    private Expr ParseSome()
    {
        Advance(); Expect(TokenKind.LParen);
        var val = ParseExpr();
        Expect(TokenKind.RParen);
        return new OptionExpr(true, val);
    }
}
