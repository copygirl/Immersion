# Immersion

## Development

- [Godot] 3.4.4, Mono version with C# support
- [.NET 6 SDK], to avoid MSBuild, and since Godot 4.0 will use it anyway.
- On Linux, you need [Mono] to run / debug the game from VS Code / VS Codium.
- [VS Code] or [VS Codium], with the following extensions:
  - C#¹
  - Mono Debug²
  - godot-tools
  - C# Tools for Godot
  - EditorConfig for VS Code
  - Todo Tree

1) The *C#* extension is available as the proprietary [`ms-dotnettools.csharp`] from the *Visual Studio Marketplace*, which is incompatible with VS Codium due to its debugger's licensing (see [dotnet/core#505](https://github.com/dotnet/core/issues/505)), or as the open-source variant [`muhammad-sammy.csharp`] from the *Open VSX Registry*.
2) *[Mono Debug]* is not available on *Open VSX Registry* (if you're using VS Codium), so you'll have to download it from the *Visual Studio Marketplace* and install it manually from the `.vsix` file.

[Godot]: https://godotengine.org/download
[.NET 6 SDK]: https://dotnet.microsoft.com/en-us/download
[Mono]: https://www.mono-project.com/download/stable/
[VS Code]: https://code.visualstudio.com/Download
[VS Codium]: https://vscodium.com/#install

[`ms-dotnettools.csharp`]: https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp
[`muhammad-sammy.csharp`]: https://open-vsx.org/extension/muhammad-sammy/csharp
[Mono Debug]: https://marketplace.visualstudio.com/items?itemName=ms-vscode.mono-debug
