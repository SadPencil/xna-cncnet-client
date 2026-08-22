#nullable enable

using System;
using System.IO;
using System.Linq;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Singleplayer
{
    /// <summary>
    /// Determines mission completion by scanning the game engine's log file for
    /// the score screen marker.
    /// </summary>
    public class LogFileMissionCompletionParser : IMissionCompletionParser
    {
        private const string SCORE_SCREEN_MARKER = "ScoreScreen: Loaded ";

        public bool? Parse()
        {
            string? logFileName = GetLogFilePath();
            if (string.IsNullOrWhiteSpace(logFileName))
                return null;

            string fullPath = ProgramConstants.GamePath + logFileName;
            if (!File.Exists(fullPath))
                return null;

            // Reject stale logs so we don't misattribute an old win to this session
            // (e.g. if the game failed to produce a fresh log).
            if (File.GetLastWriteTime(fullPath) < DateTime.Now.AddMinutes(-5.0))
                return null;

            try
            {
                foreach (string line in File.ReadAllLines(fullPath))
                {
                    if (line.StartsWith(SCORE_SCREEN_MARKER, StringComparison.Ordinal))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"{nameof(LogFileMissionCompletionParser)}: failed to parse game log '{fullPath}': {ex.Message}");
                return null;
            }

            return false;
        }

        private static string? GetLogFilePath()
        {
            string logFileName = ClientConfiguration.Instance.StatisticsLogFileName;

            if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
            {
                if (File.Exists(ProgramConstants.GamePath + "LaunchVinifera.exe") ||
                    File.Exists(ProgramConstants.GamePath + "LaunchVinifera.dat"))
                {
                    string debugDirectory = ProgramConstants.GamePath + "Debug";
                    if (Directory.Exists(debugDirectory))
                    {
                        string? newestDebugLog = Directory.GetFiles(debugDirectory, "DEBUG_*")
                            .OrderByDescending(File.GetLastWriteTime)
                            .FirstOrDefault();

                        if (newestDebugLog != null)
                            return "Debug/" + Path.GetFileName(newestDebugLog);
                    }
                }

                return logFileName;
            }
        }
    }
}
