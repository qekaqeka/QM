
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.EntityFrameworkCore;
using QM.DB;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace QM.Commands.Music
{
    [ModuleLifespan(ModuleLifespan.Singleton)]
    internal class Music : BaseCommandModule
    {
        private async Task<LavalinkNodeConnection?> GetLavalinkNodeAsync(CommandContext ctx)
        {
            LavalinkExtension lavalink = ctx.Client.GetLavalink();
            if (lavalink.ConnectedNodes.Any())
                return lavalink.GetIdealNodeConnection();

            await ctx.RespondAsync("Не удалось подключиться к серверу управления подключениями.");

            return null;
        }

        private async Task<Player?> ConnectToVoiceChannelAsync(CommandContext ctx)
        {
            if (ctx.Guild == null)
                return null;

            var node = await GetLavalinkNodeAsync(ctx);

            if (node == null)
                return null;

            var channel = ctx.Member?.VoiceState?.Channel;
            if (channel == null)
            {
                await ctx.RespondAsync("Для запуска необходмо находиться в голосовом канале");
                return null;
            }
            LavalinkGuildConnection conn = await node.ConnectAsync(channel);
            Player? player = null;

            if (node.GetGuildConnection(ctx.Guild)?.Channel == channel)
                player = Player.GetPlayer(conn.Guild);

            if (player == null)
            {
                player = new Player(conn);
                player.PlaybackStarted += OnPlaybackStarted;
            }

            return player;
        }

        private static async void OnPlaybackStarted(object sender, PlaybackStartedEventArgs e)
        {
            if (sender is Player player)
            {
                DiscordClient discord = player.Connection.Node.Discord;
                DiscordChannel channel = await discord.GetChannelAsync(e.Track.ChannelId);
                if (channel != null)
                {
                    await discord.SendMessageAsync(channel, "Сейчас играет: **" + e.Track.LavalinkTrack.Title + "**");
                }
            }
        }

        [Command]
        [Aliases("j", "ощшт", "о")]
        [Description("Подключение к голосовому каналу, в котором находится пользователь, вызвывший эту комманду.")]
        public async Task Join(CommandContext ctx)
        {
            await ConnectToVoiceChannelAsync(ctx);
        }

        [Command]
        [Aliases("l", "дуфму", "д")]
        [Description("Отключение от голосового канала.")]
        public async Task Leave(CommandContext ctx)
        {
            if (ctx.Guild == null) return;

            var node = await GetLavalinkNodeAsync(ctx);

            if (node == null) return;


            var conn = node.GetGuildConnection(ctx.Guild);


            if (conn == null)
            {
                await ctx.RespondAsync("Я не нахожусь в голосовом канале.");
                return;
            }

            var channelName = conn.Channel.Name;

            Player.GetPlayer(conn.Guild)?.DisconnectAsync()?.GetAwaiter().GetResult();

            await ctx.RespondAsync("Отключения от канала: **" + channelName + "**.");
        }

        [Command]
        [Aliases("p", "здфн", "з")]
        [Description("Воспроизведение видео на YouTube по запросу.")]
        public async Task Play(CommandContext ctx, [RemainingText, Description("Искомое видео.")] string search)
        {
            var player = await ConnectToVoiceChannelAsync(ctx);

            if (player == null) return;

            if (string.IsNullOrEmpty(search))
            {
                await ctx.RespondAsync("Отсутствует строка запроса.");
                return;
            }

            LavalinkLoadResult loadResult;

            if (Uri.TryCreate(search, UriKind.Absolute, out var uri))
            {
                loadResult = await player.Connection.GetTracksAsync(uri);
            }
            else
            {
                loadResult = await player.Connection.GetTracksAsync(search);
            }
            

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync("Произошла ошибка с треком: **" + search + "**.");
                return;
            }

            var track = loadResult.Tracks.First();
            await player.AddAsync(track, ctx.User, ctx.Channel, true);
        }

        [Command]
        [Aliases("ps", "зфгыу", "зы")]
        [Description("Остановка воспроизведения.")]
        public async Task Pause(CommandContext ctx)
        {
            if (ctx.Guild == null) return;

            var node = await GetLavalinkNodeAsync(ctx);

            if (node == null) return;

            var conn = node.GetGuildConnection(ctx.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Я не нахожусь в голосовом канале.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Я не играю музыку.");
                return;
            }

            await conn.PauseAsync();
            await ctx.RespondAsync("Воспроизведение остановлено.");
        }

        [Command]
        [Aliases("rs", "куыгьу", "кы")]
        [Description("Продолжение воспроизведения.")]
        public async Task Resume(CommandContext ctx)
        {
            if (ctx.Guild == null) return;


            var node = await GetLavalinkNodeAsync(ctx);
            if (node == null) return;


            var conn = node.GetGuildConnection(ctx.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Я не нахожусь в голосовом канале.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Я не играю музыку.");
                return;
            }

            await conn.ResumeAsync();
            await ctx.RespondAsync("Воспроизведение продолжено.");
        }

        [Command]
        [Aliases("sr", "ыуфкср", "ык")]
        [Description("Поиск треков.")]
        public async Task Search(CommandContext ctx, [RemainingText, Description("Строка запроса.")] string search)
        {
            var player = await ConnectToVoiceChannelAsync(ctx);

            if (player == null) return;

            if (string.IsNullOrEmpty(search))
            {
                await ctx.RespondAsync("Отсутствует строка запроса.");
                return;
            }

            LavalinkLoadResult loadResult = await player.Connection.GetTracksAsync(search);
            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync("Произошла запросом: **" + search + "**.");
                return;
            }

            var pages = new List<Page>();
            var tracksChunks = loadResult.Tracks.Take(1000).Chunk(10).ToList<LavalinkTrack[]>();
            int trackNumber = 1;
            int pageNumber = 1;
            foreach (var chunk in tracksChunks)
            {
                StringBuilder pageBuilder = new StringBuilder();

                for (int index = 0; index < chunk.Length; ++index)
                {
                    string trackName = chunk[index].Title;
                    if (trackName.Length > 40)
                        trackName = trackName.Substring(0, 40) + "...";

                    pageBuilder.AppendLine($"{trackNumber}. [{trackName}]({chunk[index].Uri})");

                    ++trackNumber;
                }
                DiscordEmbedBuilder deb = new DiscordEmbedBuilder();

                deb.AddField($"Страница {pageNumber}/{tracksChunks.Count} ({pageNumber * 10 - 9}-{trackNumber - 1})", pageBuilder.ToString(), true);

                deb.Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = "Напишите номер выбранного трека в " + ctx.Channel.Name + "."
                };

                pages.Add(new Page(embed: deb));
                ++pageNumber;;
            }
            var inter = ctx.Client.GetInteractivity();
            var cts = new CancellationTokenSource();
            var ctoken = cts.Token;

            _ = inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, deletion: ButtonPaginationBehavior.DeleteButtons, token: ctoken);

            var result = await ctx.Message.GetNextMessageAsync(message =>
            {

                if (!int.TryParse(message.Content, out int res))
                    return false;

                if (res > 0 && res <= loadResult.Tracks.Count())
                    return true;

                message.RespondAsync($"Некорректный индекс. Допустимый диапазон 1-{loadResult.Tracks.Count()}");
                return false;

            });

            if (!result.TimedOut)
            { 
                cts.Cancel();
                int trackIndex = int.Parse(result.Result.Content) - 1;
                LavalinkTrack track = loadResult.Tracks.ElementAt(trackIndex);
                await player.AddAsync(track, ctx.User, ctx.Channel, true);
            }
        }


        [Command]
        [Aliases("jm", "огьз", "оь")]
        public async Task Jump(CommandContext ctx, int index)
        {
            var player = await ConnectToVoiceChannelAsync(ctx);

            if (player == null) return;

            List<Track> tracks;
            using (TracksContext tracksContext = new ())
            {
                tracks = tracksContext.Tracks.ToList().Where(track => track.DiscordGuildId == ctx.Guild.Id).ToList();
            }

            if (index < 1 || index > tracks.Count)
            {
                await ctx.RespondAsync($"Некорректный номер трека. Допустимый диапазон 1 - {tracks.Count}");
            }

            var track = tracks.ElementAt(index - 1);
            await player.PlayAsync(track);
        }

        [Command]
        [Aliases("q", "йгугу", "й")]
        public async Task Queue(CommandContext ctx)
        {
            if (ctx.Guild == null) return;


            var pages = new List<Page>();
            var tracksChunks = new List<Track[]>();
            using (TracksContext tracksContext = new ())
            {
                tracksChunks = tracksContext.Tracks.ToList().Where(track => track.DiscordGuildId == ctx.Guild.Id).Chunk(10).ToList();
            }

            if (tracksChunks.Count == 0)
            {
                await ctx.RespondAsync("Очередь пуста!");
                return;
            }

            int pageNumber = 1;
            int trackNumber = 1;
            foreach (var tracks in tracksChunks)
            {
                var pageBuilder = new StringBuilder();

                for (int index = 0; index < tracks.Length; ++index)
                {
                    Track track = tracks[index];

                    string trackName = track.LavalinkTrack.Title;
                    if (trackName.Length > 30)
                        trackName = trackName.Substring(0, 30) + "..."; 

                    var discordUser = await ctx.Client.GetUserAsync(track.UserId);

                    string userName = discordUser.Username;

                    pageBuilder.AppendLine($"{trackNumber}. [{trackName}]({track.LavalinkTrack.Uri}) - Запросил {userName}");

                    ++trackNumber;
                }

                DiscordEmbedBuilder deb = new DiscordEmbedBuilder();

                deb.AddField($"Страница {pageNumber}/{tracksChunks.Count}", pageBuilder.ToString(), true);
                deb.Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = "Напишите номер страницы, чтобы перейти к ней, либо же воспользуйтесь кнопками."
                };
                pages.Add(new Page(embed: deb));
                ++pageNumber;
            }
            var inter = ctx.Client.GetInteractivity();
           
            _ = inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);   
        }

        [Command]
        [Aliases("s", "ылшз", "ы")]
        public async Task Skip(CommandContext ctx, int count = 1)
        {
            var player = Player.GetPlayer(ctx.Guild);

            if (player?.CurrentTrack == null) return;

            List<Track> tracks;

            using (TracksContext tracksContext = new ())
            {
                tracks = tracksContext.Tracks.ToList().Where(track => track.DiscordGuildId == ctx.Guild.Id).ToList();
            }

            int index = tracks.IndexOf(player.CurrentTrack);

            if (index == -1 || count < 1) return;

            if (count + index + 1 > tracks.Count)
            {
                await player.Stop();
                await ctx.RespondAsync("Так как перемещение ушло за предел списка, произведение остановленно.");
                return;
            }

            var track = tracks.ElementAt(index + count);
            await player.PlayAsync(track);
        }

        [Command]
        [Aliases("b", "и", "ифсл")]
        public async Task Back(CommandContext ctx, int count = 1)
        {
            var player = Player.GetPlayer(ctx.Guild);
            List<Track> tracks;

            if (player?.CurrentTrack == null) return;

            using (TracksContext tracksContext = new ())
            {
                tracks = tracksContext.Tracks.ToList().Where(track => track.DiscordGuildId == ctx.Guild.Id).ToList();
            }

            int index = tracks.IndexOf(player.CurrentTrack);
            if (index == -1 || count < 1) return;

            if (index - count < 0)
            {
                await player.Stop();
                await ctx.RespondAsync("Так как перемещение ушло за предел списка, произведение остановленно.");
                return;
            }

            var track = tracks.ElementAt(index - count);
            await player.PlayAsync(track);
        }

        [Command]
        [Aliases("lp", "дз", "дщщз")]
        public async Task Loop(CommandContext ctx, LoopType loopType)
        {
            if (ctx.Guild == null) return;

            var player = Player.GetPlayer(ctx.Guild);

            if (player == null) return;

            player.Loop = loopType;
            await ctx.RespondAsync("Тип цикла переключен.");
        }

        [Command]
        [Aliases("сдуфк")]
        public async Task Clear(CommandContext ctx)
        {
            if (ctx.Guild == null) return;

            int count = 0;
            using (TracksContext tracksContext = new ())
            {
                count = tracksContext.Tracks.Where(t => t.DiscordGuildId == ctx.Guild.Id).ExecuteDelete();
                tracksContext.SaveChanges();
            }

            await ctx.RespondAsync($"Очередь очищена. Было удаленно {count} треков!");
        }

        [Command]
        [Aliases("куьщму")]
        public async Task Remove(CommandContext ctx, int index)
        {
            if (ctx.Guild == null) return;

            using (TracksContext tracksContext = new ())
            {
                List<Track> tracks = tracksContext.Tracks.Where(t => t.DiscordGuildId == ctx.Guild.Id).ToList();

                if (index < 1 || index > tracks.Count)
                {
                    await ctx.RespondAsync($"Некорректный индекс, допустимый диапазон 1 - {tracks.Count}");
                    return;
                }
                Track track = tracks.ElementAt(index - 1);
                tracksContext.Tracks.Remove(track);
                tracksContext.SaveChanges();
            }

            await ctx.RespondAsync("Трек был успешно удалён.");
        }
    }
}
