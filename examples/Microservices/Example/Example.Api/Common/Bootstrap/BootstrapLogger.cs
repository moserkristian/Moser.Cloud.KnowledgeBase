namespace Example.Api.Common.Bootstrap;

[Obsolete]
internal sealed class BootstrapLogger<T> : ILogger<T>, IDisposable
{
    private readonly ILogger<T> _logger;
    private readonly ILoggerFactory _factory;
    private readonly IDisposable _scope;

    public BootstrapLogger(string? connectionString)
    {
        _factory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
            })
            .AddApplicationInsights(connectionString
                ?? throw new ArgumentNullException(nameof(connectionString), 
                "Application Insights connection string is required."));
        });

        _logger = _factory.CreateLogger<T>();

        var enrichmentData = new Dictionary<string, object>
        {
            { "Microservice", !string.IsNullOrWhiteSpace(typeof(T).Assembly.GetName().Name)
                ? typeof(T).Assembly.GetName().Name!
                : "Unknown" },
            { "ThreadId", Thread.CurrentThread.ManagedThreadId },
            { "Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                ?? "Unknown" }
        };

        _scope = _logger.BeginScope(enrichmentData) ?? NullScope.Instance;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
        _logger.BeginScope(state) ?? NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
            _logger.Log(logLevel, eventId, state, exception, formatter);

    public void Dispose()
    {
        _scope.Dispose();
        _factory.Dispose();
    }
}

internal sealed class NullScope : IDisposable
{
    public static NullScope Instance { get; } = new NullScope();
    private NullScope() { }
    public void Dispose() { }
}