using System;
using System.Collections.Generic;
using Veya.Models;

namespace Veya.Services;

public class Lexer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new()
    {
        {"function", TokenKind.Function}, {"async", TokenKind.Async}, {"struct", TokenKind.Struct},
        {"enum", TokenKind.Enum}, {"if", TokenKind.If}, {"else", TokenKind.Else},
        {"for", TokenKind.For}, {"while", TokenKind.While}, {"return", TokenKind.Return},
        {"import", TokenKind.Import}, {"mut", TokenKind.Mut}, {"in", TokenKind.In},
        {"not", TokenKind.Not}, {"and", TokenKind.And}, {"or", TokenKind.Or},
        {"is", TokenKind.Is}, {"true", TokenKind.True}, {"false", TokenKind.False},
        {"task", TokenKind.Task}, {"spawn", TokenKind.Spawn}, {"await", TokenKind.Await},
        {"unsafe", TokenKind.Unsafe}, {"Ok", TokenKind.Ok}, {"Err", TokenKind.Err},
        {"Int", TokenKind.TypeInt}, {"Float", TokenKind.TypeFloat}, {"Bool", TokenKind.TypeBool},
        {"Char", TokenKind.TypeChar}, {"String", TokenKind.TypeString}, {"Option", TokenKind.TypeOption},
        {"List", TokenKind.TypeList}, {"Dict", TokenKind.TypeDict}, {"Result", TokenKind.TypeResult},
        {"Map", TokenKind.TypeMap}, {"Some", TokenKind.Some}, {"None", TokenKind.None}
    };

    public List<Token> Tokenize(string source)
    {
        var tokens = new List<Token>();
        var lines = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            var lineOriginal = lines[i];
            var lineNum = i + 1;
            var lineTrimmed = lineOriginal.Trim();

            if (string.IsNullOrWhiteSpace(lineTrimmed) || lineTrimmed.StartsWith("//") || lineTrimmed.StartsWith("#"))
                continue;

            TokenizeLine(lineTrimmed, lineNum, lineOriginal.Length - lineTrimmed.Length, tokens);
            tokens.Add(new Token(TokenKind.Newline, null, lineNum, lineOriginal.Length));
        }

        tokens.Add(new Token(TokenKind.Eof, null, lines.Length, 0));
        return tokens;
    }

    private void TokenizeLine(string line, int lineNum, int offset, List<Token> tokens)
    {
        int pos = 0;
        while (pos < line.Length)
        {
            if (char.IsWhiteSpace(line[pos])) { pos++; continue; }

            // Commenti inline
            if (line.Substring(pos).StartsWith("//")) break;

            // Numeri (Float e Int)
            if (char.IsDigit(line[pos]))
            {
                int start = pos;
                while (pos < line.Length && (char.IsDigit(line[pos]) || (line[pos] == '.' && (pos + 1 >= line.Length || line[pos + 1] != '.')))) 
                    pos++;
                var val = line.Substring(start, pos - start);
                if (val.Contains('.'))
                    tokens.Add(new Token(TokenKind.FloatLiteral, double.Parse(val, System.Globalization.CultureInfo.InvariantCulture), lineNum, offset + start));
                else
                    tokens.Add(new Token(TokenKind.IntLiteral, long.Parse(val), lineNum, offset + start));
                continue;
            }

            // Stringhe
            if (line[pos] == '"')
            {
                int start = pos++;
                while (pos < line.Length && line[pos] != '"') pos++;
                if (pos < line.Length) pos++; // Skip chiusura quote
                tokens.Add(new Token(TokenKind.StringLiteral, line.Substring(start + 1, pos - start - 2), lineNum, offset + start));
                continue;
            }

            // Identificatori e Keyword
            if (char.IsLetter(line[pos]) || line[pos] == '_')
            {
                int start = pos;
                while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '_')) pos++;
                var name = line.Substring(start, pos - start);
                if (Keywords.TryGetValue(name, out var kind))
                    tokens.Add(new Token(kind, null, lineNum, offset + start));
                else
                    tokens.Add(new Token(TokenKind.Identifier, name, lineNum, offset + start));
                continue;
            }

            // Operatori a 2 caratteri
            if (pos + 1 < line.Length)
            {
                var op2 = line.Substring(pos, 2);
                var kind2 = op2 switch
                {
                    "==" => TokenKind.Equal, "!=" => TokenKind.NotEqual,
                    "<=" => TokenKind.LessEqual, ">=" => TokenKind.GreaterEqual,
                    "+=" => TokenKind.PlusAssign, "-=" => TokenKind.MinusAssign,
                    "->" => TokenKind.Arrow, ".." => TokenKind.Range,
                    _ => (TokenKind?)null
                };
                if (kind2.HasValue)
                {
                    tokens.Add(new Token(kind2.Value, null, lineNum, offset + pos));
                    pos += 2;
                    continue;
                }
            }

            // Operatori a 1 carattere
            var kind1 = line[pos] switch
            {
                '+' => TokenKind.Plus, '-' => TokenKind.Minus, '*' => TokenKind.Star,
                '/' => TokenKind.Slash, '%' => TokenKind.Percent, '=' => TokenKind.Assign,
                '<' => TokenKind.Less, '>' => TokenKind.Greater, '(' => TokenKind.LParen,
                ')' => TokenKind.RParen, '[' => TokenKind.LBracket, ']' => TokenKind.RBracket,
                '{' => TokenKind.LBrace, '}' => TokenKind.RBrace,
                ',' => TokenKind.Comma, ':' => TokenKind.Colon, '.' => TokenKind.Dot,
                _ => (TokenKind?)null
            };

            if (kind1.HasValue)
            {
                tokens.Add(new Token(kind1.Value, null, lineNum, offset + pos));
                pos++;
            }
            else
            {
                throw new Exception($"Riga {lineNum}: Carattere inatteso '{line[pos]}'");
            }
        }
    }
}
