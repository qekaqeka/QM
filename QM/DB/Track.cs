using DSharpPlus.Lavalink;
using System;


namespace QM.DB
{
    public class Track
    {
        public Track(ulong discordGuildId, LavalinkTrack lavalinkTrack, ulong userId, ulong channelId)
        {
            DiscordGuildId = discordGuildId;
            LavalinkTrack = lavalinkTrack;
            UserId = userId;
            ChannelId = channelId;
        }

        public static bool operator ==(Track? left, Track? right)
        {
            return left?.Id == right?.Id;
        }

        public static bool operator !=(Track? left, Track? right)
        {
            return left?.Id != right?.Id;
        }

        public long Id { get; private set; }

        public ulong DiscordGuildId { get; private set; }

        public LavalinkTrack LavalinkTrack { get; private set; }

        public ulong UserId { get; private set; }

        public ulong ChannelId { get; private set; }

        public override bool Equals(object? obj)
        {
            if (obj is not Track track)
                return false;

            return track.Id == Id;
        }

        public override int GetHashCode() => (int)(Id % int.MaxValue);
    }
}
