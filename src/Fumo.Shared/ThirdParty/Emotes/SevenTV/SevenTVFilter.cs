using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV;

public class SevenTVFilter
{
    public static void ByTags<TTag>(string searchTerm, List<TTag> emotes) where TTag : SevenTVBaseTag
    {
        // FIXME: This should be removed, once 7TV has a propery search.
        emotes.Sort((x, y) =>
        {
            var xTags = x.Tags;
            var yTags = y.Tags;

            bool xContains = xTags.Contains(searchTerm);
            bool yContains = yTags.Contains(searchTerm);

            if (xContains && !yContains)
            {
                return 1;
            }
            else if (!xContains && yContains)
            {
                return -1;
            }

            return 0;
        });
    }
}
