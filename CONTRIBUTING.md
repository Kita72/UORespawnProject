# Contributing to UORespawn

First off, thank you for considering contributing to UORespawn! It's people like you that make UORespawn such a great tool for the ServUO community.

## Code of Conduct

This project and everyone participating in it is governed by respect and professionalism. Please be considerate and respectful of others.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to see if the problem has already been reported. When you create a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples**
- **Describe the behavior you observed and what you expected**
- **Include screenshots if possible**
- **Note your environment** (Windows/macOS version, .NET version)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, include:

- **Use a clear and descriptive title**
- **Provide a detailed description of the suggested enhancement**
- **Explain why this enhancement would be useful**
- **List any alternative solutions you've considered**

### Pull Requests

1. Fork the repository
2. Create a new branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Test your changes thoroughly
5. Commit your changes (`git commit -m 'Add some amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

#### Pull Request Guidelines

- Keep changes focused - one feature/fix per PR
- Update documentation if needed
- Add tests if applicable
- Follow the existing code style
- Write clear commit messages

## Development Setup

### Prerequisites

- .NET 9 SDK
- Visual Studio 2022 (v17.8+) with:
  - .NET Multi-platform App UI development workload
  - ASP.NET and web development workload

### Building

```bash
git clone https://github.com/yourusername/UORespawn.git
cd UORespawn
dotnet restore
dotnet build
```

### Running

```bash
cd UORespawnApp
dotnet run --framework net9.0-windows10.0.19041.0
```

## Coding Standards

- Follow C# coding conventions
- Use meaningful variable and method names
- Comment complex logic
- Keep methods focused and concise
- Use async/await properly

## Project Structure

```
UORespawnApp/
??? Components/         # Blazor components
?   ??? Layout/        # Navigation and layout components
?   ??? Pages/         # Page components
??? Scripts/           # Utility classes
??? wwwroot/           # Static assets
?   ??? maps/         # Map images
?   ??? js/           # JavaScript interop
?   ??? css/          # Stylesheets
??? Data/              # Data files
```

## Component Guidelines

### Blazor Components

- Use `@code` blocks for logic
- Keep components focused (single responsibility)
- Use proper disposal patterns (`IDisposable`)
- Follow Blazor naming conventions

### CSS

- Use scoped CSS (`.razor.css`) when possible
- Support both light and dark themes
- Use Bootstrap classes for consistency

## Testing

- Test on both Windows and macOS if possible
- Test all spawn systems (Map, World, Static)
- Verify auto-save functionality
- Check file sync with ServUO folder

## Questions?

Feel free to reach out:
- Wilson: [www.servuo.dev/members/wilson.12169](https://www.servuo.dev/members/wilson.12169)
- GitHub Issues: For bugs and features
- ServUO Forums: For general discussion

## Thank You!

Your contributions help make UORespawn better for everyone in the ServUO community!
