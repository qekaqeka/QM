using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QM.Commands.Music;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace QM
{
    internal class QM
    {
        public static void Main(string[] args) => MainTask(args).ConfigureAwait(false).GetAwaiter().GetResult();

        private static async Task MainTask(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<QM>()
                .Build();

            DiscordClient discord = new (new DiscordConfiguration()
            {
                Token = config["QM:DiscordAPIToken"],
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
            });
            CommandsNextExtension commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
              StringPrefixes = new string[]{ "+" }
            });
            commands.RegisterCommands<Music>();
            commands.RegisterConverter(new LoopTypeConverter());
            ConnectionEndpoint endpoint = new ()
            {
                Hostname = Environment.GetEnvironmentVariable("LAVALINK_IP") ?? "127.0.0.1",
                Port = 2333
            };
            await Console.Out.WriteLineAsync(endpoint.Hostname);
            LavalinkConfiguration lavalinkConfig = new ()
            {
                Password = config["QM:LavalinkPassword"],
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            LavalinkExtension lavalink = discord.UseLavalink();
            discord.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(60.0)
            });
            await discord.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);
            await Task.Delay(-1);
            await discord.DisconnectAsync();
        }
    }
}
