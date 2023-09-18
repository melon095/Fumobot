using Fumo.Shared.Models;
using Fumo.WebService.Models;
using Riok.Mapperly.Abstractions;

namespace Fumo.WebService.Mapper;

[Mapper]
public partial class CommandMapper
{
#pragma warning disable RMG020 // Source member is not mapped to any target member
    public partial BasicCommandDTO CommandToBasic(ChatCommand command);
#pragma warning restore RMG020 // Source member is not mapped to any target member

    private int TimeSpanToSeconds(TimeSpan t) => (int)t.TotalSeconds;
}
