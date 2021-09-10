using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessEntities;
using Common.Config;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Services;

namespace VRNotifier.Commands
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        private readonly TrackedMediaSettings _trackedMediaSettings;
        private readonly ISubscriptionService _subscriptionService;
        private readonly INotificationEndpointService _notificationEndpointService;
        private readonly ILogger<PublicModule> _logger;

        private const string HelpText =
            "Welcome to Vik Release Notifier (VRN)!\n" +
            "The following commands are available:\n" +
            "!subscribe [mediaName1], [mediaName2], ...\n" +
            "!unsubscribe [mediaName1], [mediaName2], ...\n" +
            "!listAvailable \n" +
            "!listSubscribed";

        public PublicModule(IOptions<TrackedMediaSettings> trackedMediaSettings,
            ILogger<PublicModule> logger,
            ISubscriptionService subscriptionService,
            INotificationEndpointService notificationEndpointService)
        {
            _trackedMediaSettings = trackedMediaSettings.Value;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _notificationEndpointService = notificationEndpointService;
        }

        [Command("help")]
        [Alias("h")]
        public Task Help() => ReplyAsync(HelpText);

        [Command("listAvailable")]
        public Task ListAvailable()
        {
            var message = "Available Media: \n" +
                          $"{string.Join("\n", _trackedMediaSettings.MediaNames)}";
            return ReplyAsync(message);
        }

        [Command("listSubscribed")]
        public async Task ListSubscribed()
        {
            var result = await _subscriptionService.GetSubscribedToMedia(GetNotificationEndpointNotifierIdentifier(Context), CancellationToken.None);
            string message;
            if (result.IsSuccess)
            {
                message = result.Value == null || !result.Value.Any() 
                    ? "No Subscriptions yet."
                    : $"Subscribed To:\n{string.Join("\n", result.Value.Select(m => m.MediaName))}";
            }
            else
            {
                message = "Something went wrong.";
            }
            await Context.Channel.SendMessageAsync(message);
        }

        // TODO: Facade for all the Services in Persistence (tailored for this use - one call to AddSubscriptions which creates NotificationEndpoint)
        [Command("subscribe")]
        public async Task Subscribe(params String[] mediaNames)
        {
            if (mediaNames == null || mediaNames.Length < 1)
            {
                await Context.Channel.SendMessageAsync("Nothing to do.");
            }
            var notificationEndpointNotifierIdentifier = GetNotificationEndpointNotifierIdentifier(Context);
            var notificationEndpointResult = await _notificationEndpointService.AddNotificationEndpoint(new NotificationEndpoint(notificationEndpointNotifierIdentifier, new List<Subscription>()), CancellationToken.None);
            var message = "Something went wrong";
            if (notificationEndpointResult.IsSuccess)
            {
                var subscriptions = mediaNames
                    .Select(mediaName => new Subscription(mediaName, notificationEndpointNotifierIdentifier))
                    .ToList();

                var result = await _subscriptionService.AddSubscriptions(subscriptions, CancellationToken.None);
                if (result.All(r => r.IsSuccess))
                    message = "Successfully subscribed";
            }
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("unsubscribe")]
        public async Task Unsubscribe(params string[] mediaNames)
        {
            if (mediaNames == null || mediaNames.Length < 1)
            {
                await Context.Channel.SendMessageAsync("Nothing to do.");
            }
            var notificationEndpointNotifierIdentifier = GetNotificationEndpointNotifierIdentifier(Context); 
            var deleteSubscriptionInstructions = mediaNames
                .Select(mediaName => new DeleteSubscriptionInstruction(mediaName, notificationEndpointNotifierIdentifier))
                .ToList();
            // TODO: when creating facade call this unscubscribe
            var result = await _subscriptionService.DeleteSubscriptions(deleteSubscriptionInstructions, CancellationToken.None);
            var message = result.Any(r => r.IsFailure)
                ? "Something went wrong."
                : "Successfully unsubscribed.";
            await Context.Channel.SendMessageAsync(message);
        }

        private string GetNotificationEndpointNotifierIdentifier(ICommandContext context)
        {
            return context.Guild.Id.ToString();
        }
    }
}