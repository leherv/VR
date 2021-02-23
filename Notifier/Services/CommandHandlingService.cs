using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace VRNotifier.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandService _commandService;
        private readonly ILogger<CommandHandlingService> _logger;

        public CommandHandlingService(CommandService commandService, DiscordSocketClient discordSocketClient, IServiceProvider serviceProvider, ILogger<CommandHandlingService> logger)
        {
            _commandService = commandService;
            _client = discordSocketClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
            // Hook MessageReceived so we can process each message to see if it qualifies as a command.
            _client.MessageReceived += MessageReceivedAsync;
            // Hook CommandExecuted to handle post-command-execution logic.
            _commandService.CommandExecuted += CommandExecutedAsync;
        }
        
        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>.
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        }


        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            // This value holds the offset where the prefix ends
            var argPos = 0;
            // Perform prefix check. 
            if (!message.HasCharPrefix('!', ref argPos)) return;

            var context = new SocketCommandContext(_client, message);
            // Perform the execution of the command. In this method,
            // the command service will perform precondition and parsing check
            // then execute the command if one is matched.
            var result = await _commandService.ExecuteAsync(context, argPos, _serviceProvider);
            // we will handle the result in CommandExecutedAsync,

            // Optionally, we may inform the user if the command fails
            // to be executed; however, this may not always be desired,
            // as it may clog up the request queue should a user spam a
            // command.
            // if (!result.IsSuccess)
            // await context.Channel.SendMessageAsync(result.ErrorReason);
        }
        
        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified)
                return;

            // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;

            // the command failed, let's notify the user that something happened.
            _logger.LogError("Executing command {command} failed du to: {errorDetails}", command.Value.Name, result);
            await context.Channel.SendMessageAsync("Something went wrong.");
        }
    }
}