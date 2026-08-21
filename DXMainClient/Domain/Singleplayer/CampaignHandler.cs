using System;
using System.Collections.Generic;
using System.IO;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Singleplayer
{
    /// <summary>
    /// Parses the singleplayer campaign mission list and tracks which missions
    /// have been unlocked by the player.
    /// </summary>
    public class CampaignHandler
    {
        private const string UNLOCK_FILE = "Client/spscore.dat";
        private const string MISSIONS_SECTION = "Missions";

        private static CampaignHandler _instance;

        private CampaignHandler()
        {
            ReadBattleIni("INI/Battle.ini");
            ReadBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);

            LoadUnlockData();
        }

        public static CampaignHandler Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CampaignHandler();

                return _instance;
            }
        }

        /// <summary>
        /// All singleplayer campaign missions in the game.
        /// </summary>
        public List<Mission> Missions { get; } = new List<Mission>();

        /// <summary>
        /// Determines whether a mission was completed. Swappable for testing or
        /// for different game engines / log formats.
        /// </summary>
        public IMissionCompletionParser CompletionParser { get; set; } = new LogFileMissionCompletionParser();

        public void ReadBattleIni(string path)
        {
            string battleIniPath = ProgramConstants.GamePath + path;
            if (!File.Exists(battleIniPath))
            {
                Logger.Log("File " + path + " not found. Ignoring.");
                return;
            }

            var battleIni = new IniFile(battleIniPath);

            List<string> battleKeys = battleIni.GetSectionKeys("Battles");
            if (battleKeys == null)
                return; // File exists but [Battles] doesn't

            foreach (string battleEntry in battleKeys)
            {
                string battleSection = battleIni.GetStringValue("Battles", battleEntry, "NOT FOUND");

                if (!battleIni.SectionExists(battleSection))
                    continue;

                var mission = new Mission(battleIni.GetSection(battleSection), battleEntry);

                if (Missions.Exists(m => m.InternalName == mission.InternalName && !string.IsNullOrWhiteSpace(m.Scenario)))
                    throw new InvalidOperationException("Mission named " + mission.InternalName + " exists multiple times! (Maybe it exists in both " + path + " and another Battle*.ini?)");

                Missions.Add(mission);
            }
        }

        /// <summary>
        /// Called after the game process exits following a singleplayer mission.
        /// Unlocks the missions referenced by the completed mission's
        /// <see cref="Mission.UnlockMissions"/> list.
        /// </summary>
        /// <param name="missionInternalName">The internal name of the mission that was played.</param>
        public void PostGameExitOnSingleplayerMission(string missionInternalName)
        {
            bool? won = CompletionParser.Parse();

            if (won != true)
            {
                Logger.Log("CampaignHandler: the player did not win the mission (or no completion data was found), so no missions were unlocked.");
                return;
            }

            Mission mission = Missions.Find(m => m.InternalName == missionInternalName);
            if (mission == null)
            {
                Logger.Log("CampaignHandler: failed to unlock missions; could not find mission " + missionInternalName);
                return;
            }

            foreach (string unlockMissionName in mission.UnlockMissions)
            {
                Mission otherMission = Missions.Find(m => m.InternalName == unlockMissionName);
                if (otherMission == null)
                {
                    Logger.Log("CampaignHandler: failed to unlock mission " + unlockMissionName + " because it was not found!");
                    continue;
                }

                if (!otherMission.IsUnlocked)
                {
                    otherMission.IsUnlocked = true;
                    Logger.Log("CampaignHandler: unlocked mission " + otherMission.InternalName);
                }
            }

            WriteUnlockData();
        }

        private void LoadUnlockData()
        {
            string filePath = ProgramConstants.GamePath + UNLOCK_FILE;
            if (!File.Exists(filePath))
                return;

            var iniFile = new IniFile(filePath);
            IniSection missionsSection = iniFile.GetSection(MISSIONS_SECTION);
            if (missionsSection == null)
                return;

            foreach (var kvp in missionsSection.Keys)
            {
                if (kvp.Value != "1")
                    continue;

                Mission mission = Missions.Find(m => m.InternalName == kvp.Key);
                if (mission != null && mission.RequiresUnlocking)
                    mission.IsUnlocked = true;
            }
        }

        private void WriteUnlockData()
        {
            string filePath = ProgramConstants.GamePath + UNLOCK_FILE;

            var iniFile = new IniFile(filePath);

            foreach (Mission mission in Missions)
            {
                if (mission.RequiresUnlocking && mission.IsUnlocked)
                    iniFile.SetStringValue(MISSIONS_SECTION, mission.InternalName, "1");
            }

            iniFile.WriteIniFile();
        }
    }
}
