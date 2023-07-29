using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;


namespace QM.Commands.Music
{
    public class LoopTypeConverter : IArgumentConverter<LoopType>
    {
        Task<Optional<LoopType>> IArgumentConverter<LoopType>.ConvertAsync(string value, CommandContext ctx)
        {
            Optional<LoopType> result = new();

            if (string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
                result = (Optional<LoopType>)LoopType.None;

            else if (string.Equals(value, "one", StringComparison.OrdinalIgnoreCase))
                result = (Optional<LoopType>)LoopType.One;

            else if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
                result = (Optional<LoopType>)LoopType.All;

            return Task.FromResult(result);
        }
    }
}
