using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;
using QM.Commands.Music;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace QM
{
    internal class Programm
    {
        public static void Main(string[] args) => Programm.MainTask(args).ConfigureAwait(false).GetAwaiter().GetResult();

        private static async Task MainTask(string[] args)
        {
            DiscordClient discord = new (new DiscordConfiguration()
            {
                Token = new StreamReader("token.txt").ReadLine(),
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
                Hostname = "127.0.0.1",
                Port = 2333
            };
            LavalinkConfiguration lavalinkConfig = new ()
            {
                Password = "qekaqeka",
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
            Console.ReadKey();
            await discord.DisconnectAsync();
        }
    }
}
