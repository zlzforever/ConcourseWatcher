using Serilog;
using ConcourseWatcher;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        //
        builder.AddEnvironmentVariables("CONCOURSE_WATCHER_");
        builder.AddJsonFile($"serilog.json",
            optional: true, reloadOnChange: true);

        var configuration = builder.Build();
        Log.Logger = new LoggerConfiguration().ReadFrom
            .Configuration(configuration)
            .CreateLogger();
    })
    .ConfigureServices(services =>
    {
        //
        services.AddHostedService<Worker>();
    }).UseSerilog()
    .Build();

await host.RunAsync();