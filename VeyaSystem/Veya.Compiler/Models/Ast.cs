using System.Collections.Generic;

namespace Veya.Models;

// Tipi di Veya
public abstract record VeyaType;
public record IntType : VeyaType;
public record FloatType : VeyaType;
public record BoolType : VeyaType;
public record CharType : VeyaType;
public record StringType : VeyaType;
public record OptionType(VeyaType Inner) : VeyaType;
public record ListType(VeyaType Inner) : VeyaType;
public record DictType(VeyaType Key, VeyaType Value) : VeyaType;
public record ResultType(VeyaType OkType, VeyaType ErrType) : VeyaType;
public record MapType(VeyaType Key, VeyaType Value) : VeyaType;
public record NamedType(string Name) : VeyaType;
public record VoidType : VeyaType;
public record InferredType : VeyaType;

// Nodi dell'AST
public abstract record AstNode;

public record ProgramNode(List<TopLevelItem> Items) : AstNode;

public abstract record TopLevelItem : AstNode;
public record ImportDecl(List<string> Path) : TopLevelItem;
public record FunctionDef(string Name, bool IsAsync, List<Param> Params, VeyaType? ReturnType, List<Statement> Body) : TopLevelItem;
public record StructDef(string Name, List<FieldDef> Fields) : TopLevelItem;
public record EnumDef(string Name, List<string> Variants) : TopLevelItem;

public record Param(string Name, VeyaType Type);
public record FieldDef(string Name, VeyaType Type);

public abstract record Statement : AstNode;
public record VarDecl(string Name, bool IsMutable, VeyaType? DeclaredType, Expr Value) : Statement;
public record Assignment(Expr Target, Expr Value) : Statement;
public record CompoundAssign(string Target, string Op, Expr Value) : Statement;
public record ReturnStmt(Expr? Value) : Statement;
public record ExprStatement(Expr Expression) : Statement;
public record IfStmt(Expr Condition, List<Statement> ThenBody, List<Statement>? ElseBody) : Statement;
public record IfIsStmt(Expr Expression, bool IsSome, string VarName, List<Statement> ThenBody, List<Statement>? ElseBody) : Statement;
public record ForRangeStmt(string VarName, Expr Start, Expr End, List<Statement> Body) : Statement;
public record WhileStmt(Expr Condition, List<Statement> Body) : Statement;
public record UnsafeBlock(List<Statement> Body) : Statement;

public abstract record Expr : AstNode;
public record IntLiteral(long Value) : Expr;
public record FloatLiteral(double Value) : Expr;
public record StringLiteral(string Value) : Expr;
public record BoolLiteral(bool Value) : Expr;
public record Identifier(string Name) : Expr;
public record BinaryOp(Expr Left, string Op, Expr Right) : Expr;
public record UnaryOp(string Op, Expr Operand) : Expr;
public record FunctionCall(string Name, List<Expr> Args) : Expr;
public record MethodCall(Expr Object, string Method, List<Expr> Args) : Expr;
public record FieldAccess(Expr Object, string Field) : Expr;
public record StructInit(string Name, List<(string Name, Expr Value)> Fields) : Expr;
public record OptionExpr(bool IsSome, Expr? Value) : Expr;
public record ResultExpr(bool IsOk, Expr? Value) : Expr;
public record SpawnExpr(Expr Call) : Expr;
public record AwaitExpr(Expr Task) : Expr;
