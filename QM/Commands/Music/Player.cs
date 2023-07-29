using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using QM.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace QM.Commands.Music
{
    public enum LoopType
    {
        None,
        One,
        All,
    }
    internal class PlaybackStartedEventArgs : EventArgs
    {
        public PlaybackStartedEventArgs(Track track) => Track = track;

        public Track Track { get; private set; }
    }
    internal class Player
    {
        private static readonly List<Player> _players = new List<Player>();
        public readonly LavalinkGuildConnection Connection;

        public Player(LavalinkGuildConnection connection)
        {
            if (!connection.IsConnected)
                throw new ArgumentException("connection must be connected", nameof(connection));

            Loop = LoopType.None;

            Connection = connection;

            Connection.PlaybackFinished += PlayNextAsync;

            Connection.DiscordWebSocketClosed += (_, _) => DisconnectAsync();

            CurrentTrack = null;

            lock (_players)
            {
                _players.Add(this);
            }
        }

        public static Player? GetPlayer(DiscordGuild guild)
        {
            Player? player;
            lock (_players)
            {
                player = _players.SingleOrDefault(p => p.Connection.Guild.Id == guild.Id);
            }
            return player;
        }

        public async Task PlayAsync(Track track)
        {
            if (track.LavalinkTrack.TrackString == string.Empty)
                return;

            CurrentTrack = track;

            await Connection.PlayAsync(track.LavalinkTrack);

            PlaybackStarted?.Invoke(this, new PlaybackStartedEventArgs(track));
        }

        public async Task AddAsync(LavalinkTrack track, DiscordUser client, DiscordChannel channel, bool play)
        {
            Track _track;
            using (TracksContext tracksContext = new ())
            {
                _track = tracksContext.Tracks.Add(new Track(Connection.Guild.Id, track, client.Id, channel.Id)).Entity;
                tracksContext.SaveChanges();
            }
            if (play || CurrentTrack == null) return;

            await PlayAsync(_track);
        }

        public async Task Stop()
        {
            CurrentTrack = null;
            await Connection.StopAsync();
        }

        public async Task DisconnectAsync()
        {
            CurrentTrack = null;

            Loop = LoopType.None;

            Connection.PlaybackFinished -= PlayNextAsync;

            if (Connection.IsConnected)
                await Connection.DisconnectAsync();

            lock (_players)
            {
                _players.Remove(this);
            }
        }

        private async Task PlayNextAsync(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        {

            if (!Connection.IsConnected) return;

            if (!e.Reason.MayStartNext()) return;

            if (CurrentTrack == null) return;

            List<Track> tracks;
            using (TracksContext tracksContext = new ())
            {
                tracks = tracksContext.Tracks.ToList().Where(track => track.DiscordGuildId == Connection.Guild.Id).ToList();
            }

            int index = tracks.IndexOf(CurrentTrack);

            if (index == -1) return;


            Track? nextTrack = null;
            LoopType loop = Loop;
            switch (loop)
            {
                case LoopType.None:
                    if (index != tracks.Count - 1)
                    {
                        nextTrack = null;
                    }
                    else
                    {
                        nextTrack = tracks[index + 1];
                    }
                    break;

                case LoopType.One:
                    nextTrack = CurrentTrack;
                    break;

                case LoopType.All:
                    if (index != tracks.Count - 1)
                    {
                        nextTrack = tracks[0];
                    }
                    else
                    {
                        nextTrack = tracks[index + 1];
                    }
                    break;
            }

            if (nextTrack == null) return;

            await PlayAsync(nextTrack);
        }

        public event PlaybackStartedHandler? PlaybackStarted;

        public Track? CurrentTrack { get; private set; }

        public LoopType Loop { get; set; }

        public delegate void PlaybackStartedHandler(object sender, PlaybackStartedEventArgs e);
    }
}
