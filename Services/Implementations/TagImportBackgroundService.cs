using TaskApi_Mediporta.Services.Interfaces;

public class TagImportHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TagImportHostedService> _logger;

    public TagImportHostedService(IServiceProvider serviceProvider, ILogger<TagImportHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Automatic tag import service starting.");

        using var scope = _serviceProvider.CreateScope();
        var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();

        try
        {
            await tagService.ImportTagsAsync();
            _logger.LogInformation("Tag import completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while importing tags.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tag import service stopping.");
        return Task.CompletedTask;
    }
}