using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UyapEditorMcp;

if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
{
    Console.Error.WriteLine("UyapEditorMcp requires a Windows x64 interactive session.");
    return 1;
}

if (args is [WordDocConverter.WorkerArgument, var input, var output])
    return WordDocConverter.RunWorker(input, output);

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
    string Arg(string name) => builder.Configuration[name]
        ?? throw new EditorFailure(EditorErrors.ConfigMissing, name);

    var settings = new EditorSettings(Arg("editor-dir"), Arg("java-bin"), Arg("input-root"),
        Arg("output-root"), Arg("state-dir"), builder.Configuration["backup-root"]);
    builder.Services.AddSingleton(settings);
    builder.Services.AddSingleton(provider => new BridgeReader(provider.GetRequiredService<EditorSettings>()));
    builder.Services.AddSingleton(_ => new LocalDocumentRepository(settings));
    builder.Services.AddSingleton(provider => new LocalArchiveService(provider.GetRequiredService<LocalDocumentRepository>()));
    builder.Services.AddSingleton(provider => new LocalEditor(settings,
        provider.GetRequiredService<LocalDocumentRepository>(), provider.GetRequiredService<BridgeReader>()));
    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<EditorTools>();

    using var host = builder.Build();
    await host.RunAsync();
    return 0;
}
catch (EditorFailure e)
{
    Console.Error.WriteLine($"{e.Code}: {e.Message}");
    return 1;
}
