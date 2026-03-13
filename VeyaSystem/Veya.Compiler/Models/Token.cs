namespace Veya.Models;

public enum TokenKind
{
    // Keyword
    Function, Async, Struct, Enum, If, Else, For, While, Return, Import, Mut, In, Not, And, Or, Is, True, False,
    Task, Spawn, Await, Unsafe, Ok, Err,
    
    // Types
    TypeInt, TypeFloat, TypeBool, TypeChar, TypeString, TypeOption, TypeList, TypeDict, TypeResult, TypeMap, Some, None,

    // Literals
    FloatLiteral, IntLiteral, StringLiteral, Identifier,

    // Operators
    Plus, Minus, Star, Slash, Percent, Assign, Equal, NotEqual, Less, Greater, LessEqual, GreaterEqual,
    PlusAssign, MinusAssign, Arrow, Range,

    // Delimiters
    LParen, RParen, LBracket, RBracket, LBrace, RBrace, Comma, Colon, Dot,

    // Special
    Indent, Dedent, Newline, Eof
}

public record Token(TokenKind Kind, object? Value, int Line, int Column);
