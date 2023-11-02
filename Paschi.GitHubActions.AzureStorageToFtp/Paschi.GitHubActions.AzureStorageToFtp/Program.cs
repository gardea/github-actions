using IHost host = Host.CreateDefaultBuilder(args)
   .ConfigureServices((_, services) => services.AddGitHubActionServices())
   .Build();

static TService Get<TService>(IHost host)
    where TService : notnull =>
    host.Services.GetRequiredService<TService>();

static async Task StartCopyAsync(ActionInputs inputs, IHost host)
{
    using CancellationTokenSource tokenSource = new();

    Console.CancelKeyPress += delegate
    {
        tokenSource.Cancel();
    };

    var provider = Get<CopyProvider>(host);
    var cn = await provider.CopyAsync(inputs);
    var copiedFiles = cn.Any(x => x.Copied);
    var errors = cn.Where(x => !x.Copied).Count();
    var title = string.Empty;
    StringBuilder summary = new();
    //if (cn is { Count: > 0 })
    //{
    //    var logger = Get<ILoggerFactory>(host).CreateLogger(nameof(StartCopyAsync));
    //}
    //else
    //{
    //    summary.Append("No metrics were determined.");
    //}

    // https://docs.github.com/actions/reference/workflow-commands-for-github-actions#setting-an-output-parameter
    // ::set-output deprecated as mentioned in https://github.blog/changelog/2022-10-11-github-actions-deprecating-save-state-and-set-output-commands/
    var githubOutputFile = Environment.GetEnvironmentVariable("GITHUB_OUTPUT", EnvironmentVariableTarget.Process);
    if (!string.IsNullOrWhiteSpace(githubOutputFile))
    {
        using (var textWriter = new StreamWriter(githubOutputFile!, true, Encoding.UTF8))
        {
            textWriter.WriteLine($"copied-files={copiedFiles}");
            textWriter.WriteLine($"errors={errors}");
            textWriter.WriteLine($"summary-files={cn.Where(x => x.Copied).Count()}");
            textWriter.WriteLine("summary-details<<EOF");
            textWriter.WriteLine(summary);
            textWriter.WriteLine("EOF");
        }
    }
    else
    {
        Console.WriteLine($"::set-output name=copied-files::{copiedFiles}");
        Console.WriteLine($"::set-output name=errors::{errors}");
        Console.WriteLine($"::set-output name=summary-files::{cn.Where(x => x.Copied).Count()}");
        Console.WriteLine($"::set-output name=summary-details::{summary}");
    }

    Environment.Exit(0);
}
var nargs = new List<string>(args);
#if DEBUG
if(!args.Where(x => x == "-a" || x == "--connection-string").Any())
{
    var a = Environment.GetEnvironmentVariable("A");
    if(a is { Length: > 0 })
    {
        nargs.AddRange(new string[]
        {
            "-a",
            a
        });
    }
}
if (!args.Where(x => x == "-c" || x == "--container-name").Any())
{
    var c = Environment.GetEnvironmentVariable("C");
    if (c is { Length: > 0 })
    {
        nargs.AddRange(new string[]
        {
            "-c",
            c
        });
    }
}
if (!args.Where(x => x == "-p" || x == "--publish-profile").Any())
{
    var p = Environment.GetEnvironmentVariable("P");
    if (p is { Length: > 0 })
    {
        nargs.AddRange(new string[]
        {
            "-p",
            p
        });
    }
}
if (!args.Where(x => x == "-s" || x == "--source").Any())
{
    var s = Environment.GetEnvironmentVariable("S");
    if (s is { Length: > 0 })
    {
        nargs.AddRange(new string[]
        {
            "-s",
            s
        });
    }
}
if (!args.Where(x => x == "-d" || x == "--destination").Any())
{
    var d = Environment.GetEnvironmentVariable("D");
    if (d is { Length: > 0 })
    {
        nargs.AddRange(new string[]
        {
            "-d",
            d
        });
    }
}

#endif
var parser = Default.ParseArguments<ActionInputs>(() => new(), nargs);
parser.WithNotParsed(
    errors =>
    {
        Get<ILoggerFactory>(host)
            .CreateLogger($"{typeof(ActionInputs).Namespace}.Program")
            .LogError(
                string.Join(Environment.NewLine, errors.Select(error => error.ToString())));

        Environment.Exit(2);
    });

await parser.WithParsedAsync(options => StartCopyAsync(options, host));
await host.RunAsync();