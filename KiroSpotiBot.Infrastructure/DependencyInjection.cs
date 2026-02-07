using Azure.Data.Tables;
using KiroSpotiBot.Infrastructure.Options;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KiroSpotiBot.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.SectionName));
        
        // Register Azure Table Storage client
        var storageConnectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage connection string not configured. Please set AzureStorage:ConnectionString in configuration.");
        
        services.AddSingleton(new TableServiceClient(storageConnectionString));
        
        // Register encryption service
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        
        // Register repositories
        services.AddScoped<IGroupChatRepository, GroupChatRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITrackRecordRepository, TrackRecordRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();
        
        return services;
    }
}
