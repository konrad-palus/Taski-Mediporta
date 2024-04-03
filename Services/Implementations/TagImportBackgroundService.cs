using TaskApi_Mediporta.Services.Interfaces;

public class TagImportHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public TagImportHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();
        await tagService.ImportTagsAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}