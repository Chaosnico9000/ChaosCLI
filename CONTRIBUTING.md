# Contributing to ChaosCLI

Thank you for your interest in contributing to ChaosCLI! This document provides guidelines and instructions for contributing.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and collaborative environment for all contributors.

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:

- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior
- Actual behavior
- Your environment (OS, .NET version)
- Sample command that demonstrates the issue

### Suggesting Features

Feature suggestions are welcome! Please create an issue with:

- A clear description of the feature
- Use cases and benefits
- Proposed command syntax (if applicable)
- Any potential drawbacks or considerations

### Submitting Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Make your changes** following the coding standards below
3. **Test your changes** thoroughly
4. **Update documentation** (README.md) if needed
5. **Write clear commit messages**
6. **Submit a pull request** with a description of your changes

## Development Setup

### Prerequisites

- .NET 10.0 SDK or later
- Git
- A code editor (Visual Studio, VS Code, Rider, etc.)

### Getting Started

```bash
# Clone your fork
git clone https://github.com/yourusername/ChaosCLI.git
cd ChaosCLI

# Build the project
dotnet build

# Run the application
dotnet run -- [command] [options]

# Run tests (when available)
dotnet test
```

## Coding Standards

### General Guidelines

- Follow C# coding conventions and .NET best practices
- Use meaningful variable and method names
- Keep methods focused and concise
- Write self-documenting code; comments should explain "why", not "what"

### Style Guidelines

- Use `init` for properties in settings classes
- Use `sealed` for command classes
- Use descriptive names for command options
- Prefer string interpolation over concatenation
- Use `var` when the type is obvious
- Follow existing naming conventions:
  - PascalCase for classes, methods, properties
  - camelCase for local variables and parameters

### Command Implementation

When adding a new command:

1. **Create a command class** inheriting from `Command<TSettings>`
2. **Define a nested Settings class** inheriting from `ChaosCommandSettings`
3. **Add command options** using `[CommandOption]` attributes
4. **Implement Execute method** with proper error handling
5. **Support dry-run mode** - check `settings.DryRun` before executing actions
6. **Use Spectre.Console** for output formatting
7. **Return appropriate exit codes**

Example structure:

```csharp
public sealed class MyCommand : Command<MyCommand.Settings>
{
    public sealed class Settings : ChaosCommandSettings
    {
        [Description("Description of the option.")]
        [CommandOption("-o|--option <VALUE>")]
        [DefaultValue(defaultValue)]
        public int Option { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[yellow]Command name[/]: description");

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[grey]Dry-run: action not performed.[/]");
            return 0;
        }

        // Actual implementation
        
        AnsiConsole.MarkupLine("[green]Done.[/]");
        return 0;
    }
}
```

### Error Handling

- Use try-catch blocks where appropriate
- Return meaningful exit codes (0 = success, non-zero = failure)
- Use Spectre.Console markup for error messages
- Escape user input in console output: `Markup.Escape(userInput)`

### Documentation

- Update README.md with new commands and options
- Include usage examples for new features
- Document any breaking changes
- Add XML documentation comments for public APIs (if applicable)

## Testing Guidelines

When tests are added to the project:

- Write unit tests for new commands
- Test edge cases and error conditions
- Test dry-run mode behavior
- Ensure tests are deterministic and don't depend on external state
- Mock file system operations where appropriate

## Commit Message Guidelines

Write clear, concise commit messages:

```
Short summary (50 chars or less)

More detailed explanation if needed. Wrap at 72 characters.
Explain what changed and why, not how.

- Bullet points are okay
- Use present tense: "Add feature" not "Added feature"
```

Examples:
- `Add network latency simulation command`
- `Fix memory leak in cpu-burn command`
- `Update README with new examples`
- `Refactor error handling in IoSpamCommand`

## Pull Request Process

1. Ensure your code builds without warnings
2. Update documentation as needed
3. Follow the coding standards outlined above
4. Provide a clear description of changes in the PR
5. Link related issues in the PR description
6. Be responsive to feedback and questions
7. Squash commits if requested

## Adding New Commands

When proposing a new command:

1. **Consider the use case** - Is this useful for chaos engineering?
2. **Keep it safe** - Commands should be controllable and reversible
3. **Make it configurable** - Use command options for flexibility
4. **Support dry-run** - Always allow users to preview actions
5. **Document thoroughly** - Add examples and use cases to README

### Potential Command Ideas

- Network chaos (latency, packet loss)
- Disk space filling
- Process spawning stress
- Thread exhaustion
- Random crashes/panics
- Configuration drift simulation

## Questions?

If you have questions about contributing, please:

- Check existing issues and discussions
- Create a new issue with the "question" label
- Reach out to maintainers

## License

By contributing to ChaosCLI, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to ChaosCLI! ??
