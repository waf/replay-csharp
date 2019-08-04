# replay-csharp

A roslyn-powered editable [REPL](https://en.wikipedia.org/wiki/Read%E2%80%93eval%E2%80%93print_loop) for C#.

<p align="center">
<img src="https://github.com/waf/replay-csharp/raw/master/doc/replay.gif" style="max-width:100%;" width="600px" align="middle">
</p>

## Features

- Correct mistakes on previous lines, even after you evaluate them.
- Re-evaluate a line multiple times by hitting <kbd>ctrl</kbd> + <kbd>enter</kbd>
- Intellisense and method signature documentation
- Syntax highlighting
- Reference assemblies by `#r path/to/my.dll`
- Reference nuget packages by `#nuget MyPackage`
- Pretty-print evaluation results
- Detect incomplete expressions (e.g. `if (condition) {`), and insert a "soft newline" rather than evaluating the incomplete expression.

## Running

Requires .NET Core 3 on Windows (due to WPF).

- Prebuilt binaries can be downloaded from the `dist` directory.
- To build from source, clone the repository and run `dotnet build -c Release`
