# Veya Language

**"Simple as Python, Secure as Rust, Powerful as C++."**

Veya is a modern systems programming language designed for performance, safety, and productivity. It transpiles to **C++23**, providing zero-cost abstractions and seamless integration with existing native ecosystems.

## Core Philosophy

1. **Safety First**: Zero memory corruption, mandatory null safety (Option/Result), and controlled `unsafe` blocks.
2. **Simplicity as a Feature**: Clean, C-like syntax with braces `{}`. No semicolons, no headers, no complexity.
3. **Productivity**: Built-in Package Manager, Language Server (LSP), and modern collections.

## Getting Started

### Installation

Veya is distributed as a .NET tool. To install it globally:

```bash
cd VeyaSystem/Veya.CLI
dotnet pack
dotnet tool install --global --add-source ./bin/Release/ Veya.CLI
```

### Create a new project

```bash
veya new my_project
cd my_project
```

### Run the project

```bash
veya run
```

## Language Features (Phase 2)

### 🚀 Concurrency
Veya supports native task-based concurrency:
```veya
task function fetch_data() -> Result[String, String] {
    return Ok("Data received")
}

function main() {
    mut t = spawn fetch_data()
    mut res = await t
}
```

### 🛡️ Memory Safety & Error Handling
Veya uses `Option` and `Result` for explicit error handling:
```veya
function divide(a: Int, b: Int) -> Result[Int, String] {
    if b == 0 { return Err("Division by zero") }
    return Ok(a / b)
}

function main() {
    mut res: Result[Int, String] = divide(10, 2)
}
```

### 🔒 Unsafe Blocks
For low-level operations, Veya provides `unsafe` blocks:
```veya
unsafe {
    // raw pointer manipulation or C++ interop
}
```

## Tooling

- **`veya new <name>`**: Scaffold a new project.
- **`veya build`**: Transpile and compile to a native executable (requires `g++` with C++23 support).
- **`veya run`**: Build and execute immediately.
- **`veya parse`**: Syntax check and AST generation.
- **`veya doctor`**: Verify environment and dependencies.

## Project Structure

- `VeyaSystem/`: Core C# implementation (Compiler, CLI, LSP).
- `veya-vscode/`: Official Visual Studio Code extension.
- `examples/`: Sample Veya applications.
- `veya.toml`: Project manifest and dependency management.
