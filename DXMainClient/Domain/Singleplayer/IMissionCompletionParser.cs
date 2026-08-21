#nullable enable

namespace DTAClient.Domain.Singleplayer
{
    /// <summary>
    /// Determines whether the most recently played singleplayer mission was won.
    /// </summary>
    public interface IMissionCompletionParser
    {
        /// <summary>
        /// Parses the game's log output and determines whether the player won.
        /// Returns null if no suitable log was found (missing, too old, etc.).
        /// </summary>
        bool? Parse();
    }
}
