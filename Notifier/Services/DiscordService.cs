using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessEntities;
using Common.Config;
using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VRNotifier.Services
{
    public class DiscordService: BackgroundService, INotificationService
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordSettings _discordSettings;
        private readonly CommandHandlingService _commandHandlingService;
        private readonly ILogger<DiscordService> _logger;
        private const string NOTIFICATION_CATEGORY_CHANNEL = "Notifications"; 

        public DiscordService(CommandHandlingService commandHandlingService,
            DiscordSocketClient client,
            IOptions<DiscordSettings> discordSettings,
            ILogger<DiscordService> logger)
        {
            _commandHandlingService = commandHandlingService;
            _client = client;
            _logger = logger;
            _discordSettings = discordSettings.Value;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Connecting to Discord");
                await _client.LoginAsync(TokenType.Bot, _discordSettings.ApiKey);
                await _client.StartAsync();
                await _commandHandlingService.InitializeAsync();
                _logger.LogInformation("Connection successful.");
            }
            catch (Exception e)
            {
                _logger.LogError("Something went wrong: {exception}\n {innerException}", e.Message, e.InnerException?.Message);
            }
        }

        public async Task<Result> Notify(NotificationInfo notificationInfo)
        {
            try
            {
                var notifiableChannelInformation = await FilterNotifiableEndpointInformation(notificationInfo);
                var notifyChannelTasks = notifiableChannelInformation.Select(notifiableChannelInfo =>
                    NotifyChannel(notifiableChannelInfo, notificationInfo.Message));
                var executionResult = await Task.WhenAll(notifyChannelTasks);
                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogError("Failed during notification for message {message} due to: {errorMessage}", notificationInfo.Message, e.Message);
                return Result.Failure("Something went wrong.");
            }
        }

        private async Task<Result> NotifyChannel((SocketGuild socketGuild, ITextChannel socketTextChannel) endpointInformation, string message)
        {
            try
            {
                await endpointInformation.socketTextChannel?.SendMessageAsync(message);
                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to notify channel with name {channelName} due to {errorMessage}", endpointInformation.socketTextChannel?.Name ?? "Something went wrong", e.Message);
                return Result.Failure($"Failed to notify channel with name {endpointInformation.socketTextChannel?.Name ?? "Something went wrong"} due to {e.Message}");
            }
           
        }
        
        private async Task<IEnumerable<(SocketGuild socketGuild, ITextChannel socketTextChannel)>> FilterNotifiableEndpointInformation(NotificationInfo notificationInfo)
        {
            var notifiableEndpointInformation = new List<(SocketGuild, ITextChannel)>();
            foreach (var notificationEndpointIdentifier in notificationInfo.NotificationEndpointIdentifiers)
            {
                var guildResult = FetchGuild(notificationEndpointIdentifier);
                if (guildResult.IsFailure)
                {
                    _logger.LogError("Could not notify guild with identifier {identifier} due to {error}", notificationEndpointIdentifier, guildResult.Error);
                    continue;
                }

                var categoryChannelResult =
                    FetchSocketCategoryChannel(guildResult.Value, NOTIFICATION_CATEGORY_CHANNEL);
                if (categoryChannelResult.IsFailure)
                {
                    _logger.LogWarning("Could not find category channel with name {name} trying to create it...", NOTIFICATION_CATEGORY_CHANNEL);
                    var categoryCreationResult = await CreateCategoryChannel(guildResult.Value, NOTIFICATION_CATEGORY_CHANNEL);
                    if (categoryCreationResult.IsFailure)
                    {
                        _logger.LogError("Could not create category channel with name {name}.", NOTIFICATION_CATEGORY_CHANNEL);
                        continue;
                    }
                    categoryChannelResult = categoryCreationResult;
                }

                var channelResult = FetchChannel(guildResult.Value, notificationInfo.MediaName);
                if (channelResult.IsFailure)
                {
                    _logger.LogWarning("Could not find channel with name {mediaName} trying to create it...", notificationInfo.MediaName.ToLower());
                    var channelCreationResult = await CreateNotificationChannel(guildResult.Value, categoryChannelResult.Value, notificationInfo.MediaName.ToLower());
                    if (channelCreationResult.IsFailure)
                    {
                        _logger.LogError("Could not create notification channel with name {mediaName}.", notificationInfo.MediaName.ToLower());
                        continue;
                    }
                    channelResult = channelCreationResult;
                }
                notifiableEndpointInformation.Add((guildResult.Value, channelResult.Value));
            }

            return notifiableEndpointInformation;
        }

        private Result<SocketGuild> FetchGuild(string notificationEndpointIdentifier)
        {
            var guild = _client.Guilds.FirstOrDefault(g =>
                g.Id.ToString().Equals(notificationEndpointIdentifier));
            return guild == null
                ? Result.Failure<SocketGuild>($"No Guild for notificationEndpointIdentifier {notificationEndpointIdentifier} found.")
                : Result.Success(guild);
        }

        private Result<ITextChannel> FetchChannel(SocketGuild socketGuild, string channelName)
        {
            var channel = socketGuild.TextChannels.FirstOrDefault(c => c.Name.ToLower().Equals(channelName));
            return channel == null
                ? Result.Failure<ITextChannel>($"No notification channel with name {channelName} found.")
                : Result.Success<ITextChannel>(channel);
        }
        
        private async Task<Result<ITextChannel>> CreateNotificationChannel(SocketGuild socketGuild, ICategoryChannel categoryChannel, string channelName)
        {
            try
            {
                var textChannel = await socketGuild.CreateTextChannelAsync(channelName,
                    textChannelProperties => { textChannelProperties.CategoryId = categoryChannel.Id; }
                );
                return Result.Success<ITextChannel>(textChannel);
            }
            catch (Exception)
            {
                _logger.LogError("Failed to create notification channel with name {channelName}.", channelName);
                return Result.Failure<ITextChannel>(
                    $"Failed to create notification channel with name {channelName}");
            }
        }
        
        private Result<ICategoryChannel> FetchSocketCategoryChannel(SocketGuild socketGuild, string categoryName)
        {
            var categoryChannel = socketGuild.CategoryChannels.FirstOrDefault(c =>
                c.Name.ToLower().Equals(categoryName.ToLower()));
            return categoryChannel == null
                ? Result.Failure<ICategoryChannel>(
                    $"No category channel with name {categoryName} found.")
                : Result.Success<ICategoryChannel>(categoryChannel);
        }
        
        private async Task<Result<ICategoryChannel>> CreateCategoryChannel(SocketGuild socketGuild,
            string categoryName)
        {
            try
            {
                var socketCategoryChannel = await socketGuild.CreateCategoryChannelAsync(categoryName);
                return Result.Success<ICategoryChannel>(socketCategoryChannel);
            }
            catch (Exception)
            {
                _logger.LogError("Could not create category channel with name {categoryName}.", categoryName);
                return Result.Failure<ICategoryChannel>($"Could not create category channel with name {categoryName}.");
            }
        }
    }
}