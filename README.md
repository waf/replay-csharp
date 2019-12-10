# <img align="left" width="46" height="46" src="https://raw.githubusercontent.com/waf/replay-csharp/master/resource/logo_medium.png" />Replay

A roslyn-powered editable [REPL](https://en.wikipedia.org/wiki/Read%E2%80%93eval%E2%80%93print_loop) for C#.

<p align="center">
<img src="https://github.com/waf/replay-csharp/raw/master/resource/replay.gif" style="max-width:100%;" width="600px" align="middle">
</p>

## Features

- Correct mistakes on previous lines, even after you evaluate them.
- Re-evaluate a line multiple times by hitting <kbd>ctrl</kbd> + <kbd>enter</kbd>
- Intellisense and method signature documentation
- Syntax highlighting
- Reference assemblies by `#r path/to/my.dll`
- Reference nuget packages by `#nuget MyPackage`
- Pretty-print evaluation results
- Export your REPL session as C# or Markdown
- Detect incomplete expressions (e.g. `if (condition) {`), and insert a "soft newline" rather than evaluating the incomplete expression.

## Running

Replay requires Windows 10 (due to WPF).

- Download Replay from the [Releases page](https://github.com/waf/replay-csharp/releases) and unzip the archive.
- Run Replay.exe and type `help` to get started!
    - The very first run of Replay will be slow (around 7 or 8 seconds), but subsequent starts should be much faster (1 second or less).

## Building from source

- To build from source, clone the repository and run `dotnet build`
- Optionally, build a AOT-compiled version using `dotnet publish -r win-x64 -c Release`

## Attribution

- Application logo by 588ku from [pngtree.com](https://pngtree.com/).
