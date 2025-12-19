# ChaosCLI

A command-line chaos engineering tool for testing system resilience by simulating various failure scenarios and resource stress conditions.

## Overview

ChaosCLI is a lightweight, cross-platform tool designed to help developers and DevOps engineers test how their systems handle various stress conditions and failures. It provides simple commands to simulate timeouts, CPU load, memory pressure, I/O stress, and custom exit codes.

## Features

- **Timeout Simulation** - Introduce delays with configurable exit codes
- **CPU Burn** - Generate CPU load with configurable parallelism
- **Memory Spike** - Allocate and hold memory to simulate memory pressure
- **I/O Spam** - Generate disk I/O load with repeated read/write operations
- **Exit Code Testing** - Exit with specific codes for pipeline testing
- **Dry-Run Mode** - Preview actions without executing them
- **Verbose Output** - Detailed logging for debugging

## Requirements

- .NET 10.0 or later

## Installation

### Build from Source

```bash
git clone https://github.com/yourusername/ChaosCLI.git
cd ChaosCLI
dotnet build -c Release
```

### Run

```bash
dotnet run -- [command] [options]
```

Or publish as a standalone executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### General Options

All commands support the following options:

- `--dry-run` - Show what would happen without executing
- `-v, --verbose` - Enable verbose output

### Commands

#### Timeout

Simulates a timeout by waiting for a specified duration and then exiting with an optional exit code.

```bash
chaoscli timeout [options]
```

**Options:**
- `-m, --ms <MS>` - How long to wait in milliseconds (default: 2000)
- `-e, --exit-code <CODE>` - Exit code to return (default: 0)

**Examples:**
```bash
# Wait 5 seconds and exit successfully
chaoscli timeout --ms 5000

# Wait 3 seconds and exit with failure code
chaoscli timeout --ms 3000 --exit-code 1

# Dry run
chaoscli timeout --ms 10000 --dry-run
```

#### CPU Burn

Burns CPU resources for a specified duration with configurable parallelism.

```bash
chaoscli cpu-burn [options]
```

**Options:**
- `-s, --seconds <SEC>` - Duration in seconds (default: 5)
- `-p, --parallel <N>` - Number of worker tasks (default: logical processor count)

**Examples:**
```bash
# Burn CPU for 10 seconds using all cores
chaoscli cpu-burn --seconds 10

# Burn CPU using 4 threads for 30 seconds
chaoscli cpu-burn --seconds 30 --parallel 4

# Preview without execution
chaoscli cpu-burn --seconds 60 --dry-run -v
```

#### Memory Spike

Allocates a specified amount of memory and holds it for a configurable duration.

```bash
chaoscli mem-spike [options]
```

**Options:**
- `-m, --mb <MB>` - Megabytes to allocate (default: 256)
- `-h, --hold <SEC>` - How long to hold memory in seconds (default: 5, 0 = release immediately)

**Examples:**
```bash
# Allocate 512MB and hold for 10 seconds
chaoscli mem-spike --mb 512 --hold 10

# Allocate 1GB and release immediately
chaoscli mem-spike --mb 1024 --hold 0

# Verbose mode to see allocation progress
chaoscli mem-spike --mb 2048 --hold 15 -v
```

#### I/O Spam

Generates disk I/O load by repeatedly writing and reading a file.

```bash
chaoscli io-spam [options]
```

**Options:**
- `-n, --iterations <N>` - Number of write+read iterations (default: 50)
- `-b, --bytes <BYTES>` - Bytes to write per iteration (default: 1048576 = 1MB)
- `-f, --file <PATH>` - Specific file path to use (default: temp file)

**Examples:**
```bash
# Default: 50 iterations of 1MB each
chaoscli io-spam

# Heavy I/O: 200 iterations of 5MB each
chaoscli io-spam --iterations 200 --bytes 5242880

# Use a specific file (won't be deleted)
chaoscli io-spam --iterations 100 --file ./test-io.bin

# Verbose output
chaoscli io-spam --iterations 50 -v
```

#### Exit Code

Exits immediately with a specified exit code (useful for testing pipelines and error handling).

```bash
chaoscli exit [options]
```

**Options:**
- `-c, --code <CODE>` - Exit code to return (default: 1)

**Examples:**
```bash
# Exit with code 1 (generic failure)
chaoscli exit

# Exit with custom code
chaoscli exit --code 42

# Dry run always returns 0
chaoscli exit --code 1 --dry-run
```

## Use Cases

### CI/CD Pipeline Testing

Test how your pipeline handles command failures:

```bash
# Test timeout handling in deployment scripts
chaoscli timeout --ms 30000 --exit-code 124

# Test pipeline behavior on failure
chaoscli exit --code 1
```

### Load Testing

Simulate resource constraints:

```bash
# Simulate high CPU during load test
chaoscli cpu-burn --seconds 60 --parallel 8

# Simulate memory pressure
chaoscli mem-spike --mb 2048 --hold 30
```

### Container Stress Testing

Test container resource limits and OOM behavior:

```bash
# Kubernetes pod with memory limits
docker run --memory=512m myimage chaoscli mem-spike --mb 600 --hold 10
```

### Chaos Engineering

Introduce controlled failures in microservices:

```bash
# Random service delays
chaoscli timeout --ms $RANDOM --exit-code 0

# Intermittent I/O issues
chaoscli io-spam --iterations 1000 --bytes 10485760
```

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Code Structure

- `Program.cs` - Main entry point and command definitions
- Commands are implemented as sealed classes inheriting from `Command<TSettings>`
- All commands support dry-run and verbose modes through `ChaosCommandSettings`

## Dependencies

- [Spectre.Console](https://spectreconsole.net/) - Rich terminal UI library
- [Spectre.Console.Cli](https://spectreconsole.net/cli/) - Command-line parsing and routing

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Built with [Spectre.Console](https://spectreconsole.net/) for beautiful terminal output.

## Roadmap

Future features under consideration:

- [ ] Network chaos (latency, packet loss simulation)
- [ ] Disk space filling
- [ ] Process spawning stress
- [ ] Configuration file support
- [ ] Scheduled/repeated execution modes
- [ ] Metrics export (Prometheus, JSON)

## Safety Notes

?? **Use responsibly!** This tool is designed to stress system resources. Always:

- Use in isolated test environments
- Understand the impact of each command
- Monitor system health during testing
- Have proper resource limits in place (containers, VMs)
- Use `--dry-run` to preview actions first
