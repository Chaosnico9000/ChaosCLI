using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ChaosCLI;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(cfg =>
        {
            cfg.SetApplicationName("chaoscli");

            cfg.AddCommand<TimeoutCommand>("timeout")
               .WithDescription("Simulates a timeout by waiting and optionally exiting with a failure code.");

            cfg.AddCommand<CpuBurnCommand>("cpu-burn")
               .WithDescription("Burns CPU for N seconds with configurable parallelism.");

            cfg.AddCommand<MemorySpikeCommand>("mem-spike")
               .WithDescription("Allocates memory (managed) to simulate pressure and optionally holds it.");

            cfg.AddCommand<IoSpamCommand>("io-spam")
               .WithDescription("Writes/reads a temp file repeatedly to simulate IO load (safe, local).");

            cfg.AddCommand<ExitCodeCommand>("exit")
               .WithDescription("Exits immediately with a chosen exit code (useful for pipeline tests).");
        });

        try
        {
            return app.Run(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fatal:[/] {Markup.Escape(ex.Message)}");
            return 99;
        }
    }
}

public abstract class ChaosCommandSettings : CommandSettings
{
    [Description("Prints what would happen, without doing it.")]
    [CommandOption("--dry-run")]
    public bool DryRun { get; init; }

    [Description("Verbose output.")]
    [CommandOption("-v|--verbose")]
    public bool Verbose { get; init; }
}

public sealed class TimeoutCommand : Command<TimeoutCommand.Settings>
{
    public sealed class Settings : ChaosCommandSettings
    {
        [Description("How long to wait (milliseconds).")]
        [CommandOption("-m|--ms <MS>")]
        [DefaultValue(2000)]
        public int Milliseconds { get; init; }

        [Description("Exit code after waiting (0 means success).")]
        [CommandOption("-e|--exit-code <CODE>")]
        [DefaultValue(0)]
        public int ExitCode { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[yellow]Timeout[/]: wait {settings.Milliseconds}ms, then exit {settings.ExitCode}");

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[grey]Dry-run: no waiting performed.[/]");
            return 0;
        }

        Thread.Sleep(settings.Milliseconds);
        return settings.ExitCode;
    }
}

public sealed class CpuBurnCommand : Command<CpuBurnCommand.Settings>
{
    public sealed class Settings : ChaosCommandSettings
    {
        [Description("Duration in seconds.")]
        [CommandOption("-s|--seconds <SEC>")]
        [DefaultValue(5)]
        public int Seconds { get; init; }

        [Description("Number of worker tasks (default: logical processor count).")]
        [CommandOption("-p|--parallel <N>")]
        public int? Parallelism { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var workers = settings.Parallelism ?? Environment.ProcessorCount;
        AnsiConsole.MarkupLine($"[yellow]CPU burn[/]: {settings.Seconds}s with {workers} workers");

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[grey]Dry-run: no load generated.[/]");
            return 0;
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(settings.Seconds));
        var token = cts.Token;

        var tasks = Enumerable.Range(0, workers).Select(i => Task.Run(() =>
        {
            // Busy loop, but keeps it deterministic-ish
            long x = 0;
            while (!token.IsCancellationRequested)
            {
                x = (x * 48271 + 1) % 0x7fffffff;
            }
            return x;
        }, token)).ToArray();

        try
        {
            Task.WaitAll(tasks);
        }
        catch (AggregateException) { /* expected on cancel */ }

        AnsiConsole.MarkupLine("[green]Done.[/]");
        return 0;
    }
}

public sealed class MemorySpikeCommand : Command<MemorySpikeCommand.Settings>
{
    public sealed class Settings : ChaosCommandSettings
    {
        [Description("Approx. megabytes to allocate.")]
        [CommandOption("-m|--mb <MB>")]
        [DefaultValue(256)]
        public int Megabytes { get; init; }

        [Description("How long to hold memory (seconds). 0 = release immediately.")]
        [CommandOption("-h|--hold <SEC>")]
        [DefaultValue(5)]
        public int HoldSeconds { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[yellow]Memory spike[/]: allocate ~{settings.Megabytes}MB, hold {settings.HoldSeconds}s");

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[grey]Dry-run: nothing allocated.[/]");
            return 0;
        }

        var bytes = settings.Megabytes * 1024L * 1024L;
        // allocate in chunks to avoid single huge array issues
        const int chunkSize = 8 * 1024 * 1024; // 8MB
        var chunks = new List<byte[]>();

        long allocated = 0;
        while (allocated < bytes)
        {
            var size = (int)Math.Min(chunkSize, bytes - allocated);
            var chunk = GC.AllocateUninitializedArray<byte>(size, pinned: false);
            // touch memory so it actually gets committed
            for (int i = 0; i < chunk.Length; i += 4096) chunk[i] = 123;
            chunks.Add(chunk);
            allocated += size;

            if (settings.Verbose)
                AnsiConsole.MarkupLine($"[grey]Allocated {allocated / (1024 * 1024)}MB[/]");
        }

        if (settings.HoldSeconds > 0)
        {
            AnsiConsole.MarkupLine("[grey]Holding...[/]");
            Thread.Sleep(TimeSpan.FromSeconds(settings.HoldSeconds));
        }

        chunks.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        AnsiConsole.MarkupLine("[green]Released.[/]");
        return 0;
    }
}

public sealed class IoSpamCommand : Command<IoSpamCommand.Settings>
{
    public sealed class Settings : ChaosCommandSettings
    {
        [Description("How many iterations of write+read.")]
        [CommandOption("-n|--iterations <N>")]
        [DefaultValue(50)]
        public int Iterations { get; init; }

        [Description("Bytes to write each iteration.")]
        [CommandOption("-b|--bytes <BYTES>")]
        [DefaultValue(1024 * 1024)]
        public int BytesPerIteration { get; init; }

        [Description("Use a specific file path (default: temp file).")]
        [CommandOption("-f|--file <PATH>")]
        public string? FilePath { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var path = settings.FilePath ?? Path.Combine(Path.GetTempPath(), $"chaoscli-iospam-{Guid.NewGuid():N}.bin");
        AnsiConsole.MarkupLine($"[yellow]IO spam[/]: {settings.Iterations} iters, {settings.BytesPerIteration} bytes, file: {path}");

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[grey]Dry-run: no file written.[/]");
            return 0;
        }

        var buffer = new byte[settings.BytesPerIteration];
        Random.Shared.NextBytes(buffer);

        try
        {
            for (int i = 1; i <= settings.Iterations; i++)
            {
                File.WriteAllBytes(path, buffer);
                _ = File.ReadAllBytes(path);

                if (settings.Verbose)
                    AnsiConsole.MarkupLine($"[grey]Iteration {i}/{settings.Iterations}[/]");
            }
        }
        finally
        {
            if (settings.FilePath is null)
            {
                try { File.Delete(path); } catch { /* ignore */ }
            }
        }

        AnsiConsole.MarkupLine("[green]Done.[/]");
        return 0;
    }
}

public sealed class ExitCodeCommand : Command<ExitCodeCommand.Settings>
{
    public sealed class Settings : ChaosCommandSettings
    {
        [Description("Exit code to return (0-255 typical).")]
        [CommandOption("-c|--code <CODE>")]
        [DefaultValue(1)]
        public int Code { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[yellow]Exit[/]: returning code {settings.Code}");
        return settings.DryRun ? 0 : settings.Code;
    }
}