#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// An instance of a Map in a given GameMode
    /// </summary>
    public record GameModeMap : IGameModeMap
    {
        public required GameMode GameMode { get; init; }
        public required Map Map { get; init; }
        public bool IsFavorite { get; set; } = false;
        public GameModeMap() { }

        [SetsRequiredMembers]
        public GameModeMap(GameMode gameMode, Map map)
        {
            GameMode = gameMode;
            Map = map;
        }

        [SetsRequiredMembers]
        public GameModeMap(GameMode gameMode, Map map, bool isFavorite)
        {
            GameMode = gameMode;
            Map = map;
            IsFavorite = isFavorite;
        }

        public string ToUntranslatedString() => $"{Map.UntranslatedName}:{GameMode.UntranslatedUIName}";


        public override string ToString()
        {
            return $"{Map.Name}:{GameMode.UIName}";
        }

        public List<int> AllowedStartingLocations
        {
            get
            {
                var ret = Map.AllowedStartingLocations ?? GameMode.AllowedStartingLocations ?? Enumerable.Range(1, MaxPlayers).ToList();

                if (ret.Count != MaxPlayers)
                    throw new Exception(string.Format("The number of AllowedStartingLocations does not equal to MaxPlayer.".L10N("Client:Main:InvalidAllowedStartingLocationsCount")));

                return ret;
            }
        }

        public int CoopDifficultyLevel =>
            Map.CoopDifficultyLevel ?? GameMode.CoopDifficultyLevel ?? 0;

        public CoopMapInfo CoopInfo =>
            Map.CoopInfo ?? GameMode.CoopInfo ?? new CoopMapInfo();

        public bool EnforceMaxPlayers =>
            Map.EnforceMaxPlayers ?? GameMode.EnforceMaxPlayers ?? false;

        public bool ForceNoTeams =>
            Map.ForceNoTeams ?? GameMode.ForceNoTeams ?? false;

        public bool ForceRandomStartLocations =>
            Map.ForceRandomStartLocations ?? GameMode.ForceRandomStartLocations ?? false;

        public bool HumanPlayersOnly =>
            Map.HumanPlayersOnly ?? GameMode.HumanPlayersOnly ?? false;

        public bool IsCoop =>
            Map.IsCoop ?? GameMode.IsCoop ?? false;

        public int MaxPlayers =>
            // Note: GameLobbyBase.GetMapList() assumes the priority of Map.MaxPlayers over GameMode.MaxPlayers.
            // If you have modified the expression here, you should also update GameLobbyBase.GetMapList().
            Map.MaxPlayers ?? GameMode.MaxPlayers ?? 0;

        public int MinPlayers =>
            GameMode.MinPlayersOverride ?? Map.MinPlayers ?? GameMode.MinPlayers ?? 0;

        public bool MultiplayerOnly =>
            Map.MultiplayerOnly ?? GameMode.MultiplayerOnly ?? false;

    }
}
