using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Helpy.Conversation
{
    public class TroubleContext
    {
        public bool IsPackLoaded { get; private set; }

        public TroubleshootingPayloadXL? PayloadXL { get; private set; }

        public TroubleshootingPayloadDalamud? PayloadDalamud { get; private set; }

        public string? DalamudLog { get; private set; }

        public string? LauncherLog { get; private set; }

        public ExceptionPayload? LastExceptionXL { get; private set; }
        
        public ExceptionPayload? LastExceptionDalamud { get; private set; }

        private static Regex dalamudTroubleRegex = 
            new Regex("TROUBLESHOOTING:(?<payload>.*)$", RegexOptions.Compiled | RegexOptions.Multiline);

        private static Regex exceptionRegex =
            new Regex("LASTEXCEPTION:(?<payload>.*)$", RegexOptions.Compiled | RegexOptions.Multiline);

        public void LoadPack(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var archive = new ZipArchive(stream);

            var troubleJson = ReadEntry(archive.GetEntry("trouble.json"));
            if (string.IsNullOrEmpty(troubleJson))
                throw new Exception("trouble.json was not found");

            PayloadXL = JsonSerializer.Deserialize<TroubleshootingPayloadXL>(troubleJson);
            if (PayloadXL == null)
                throw new Exception("trouble.json deserialized to null");

            LauncherLog = ReadEntry(archive.GetEntry("output.log"));
            LastExceptionXL = FindException(LauncherLog);

            DalamudLog = ReadEntry(archive.GetEntry("dalamud.log"));
            FindDalamudTrouble();
            LastExceptionDalamud = FindException(DalamudLog);

            IsPackLoaded = true;
        }

        private static ExceptionPayload? FindException(string? log)
        {
            if (string.IsNullOrEmpty(log))
                return null;

            var matches = exceptionRegex.Matches(log);
            var last = matches.LastOrDefault();

            if (last == null)
            {
                Console.WriteLine("Log had no exception payloads");
                return null;
            }

            var valueText = last.Groups["payload"].Value;
            var json = Convert.FromBase64String(valueText);

            return JsonSerializer.Deserialize<ExceptionPayload>(valueText);
        }

        private void FindDalamudTrouble()
        {
            if (string.IsNullOrEmpty(DalamudLog))
                return;

            var matches = dalamudTroubleRegex.Matches(DalamudLog);
            var last = matches.LastOrDefault();

            if (last == null)
            {
                PayloadDalamud = null;
                Console.WriteLine("Dalamud log had no ts payloads");
                return;
            }

            var valueText = last.Groups["payload"].Value;
            var json = Convert.FromBase64String(valueText);
            PayloadDalamud = JsonSerializer.Deserialize<TroubleshootingPayloadDalamud>(json);
            if (PayloadDalamud == null)
                throw new Exception("Couldn't deserialize Dalamud ts payload");
        }

        private static string? ReadEntry(ZipArchiveEntry? entry)
        {
            if (entry == null)
                return null;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        public class ExceptionPayload
        {
            public DateTime When { get; set; }

            public string Info { get; set; }

            public string Context { get; set; }
        }

        public class TroubleshootingPayloadXL
        {
            public DateTime When { get; set; }

            public bool IsDx11 { get; set; }

            public bool IsAutoLogin { get; set; }

            public bool IsUidCache { get; set; }

            public bool DalamudEnabled { get; set; }

            public int DalamudLoadMethod { get; set; }

            public decimal DalamudInjectionDelay { get; set; }

            public bool SteamIntegration { get; set; }

            public bool EncryptArguments { get; set; }

            public string LauncherVersion { get; set; }

            public string LauncherHash { get; set; }

            public bool Official { get; set; }

            public int DpiAwareness { get; set; }

            public int Platform { get; set; }

            public string ObservedGameVersion { get; set; }

            public string ObservedEx1Version { get; set; }
            public string ObservedEx2Version { get; set; }
            public string ObservedEx3Version { get; set; }
            public string ObservedEx4Version { get; set; }

            public bool BckMatch { get; set; }

            public enum IndexIntegrityResult
            {
                Failed,
                Exception,
                NoGame,
                ReferenceNotFound,
                ReferenceFetchFailure,
                Success,
            }

            public IndexIntegrityResult IndexIntegrity { get; set; }
        }

        public class TroubleshootingPayloadDalamud
        {
            public LocalPluginManifest[] LoadedPlugins { get; set; }

            public string DalamudVersion { get; set; }

            public string DalamudGitHash { get; set; }

            public string GameVersion { get; set; }

            public string Language { get; set; }

            public bool DoDalamudTest => false;

            public string? BetaKey { get; set; }

            public bool DoPluginTest { get; set; }

            public bool LoadAllApiLevels { get; set; }

            public bool InterfaceLoaded { get; set; }

            public bool ForcedMinHook { get; set; }

            /// <summary>
            /// Third party repository for dalamud plugins.
            /// </summary>
            public sealed class ThirdPartyRepoSettings
            {
                /// <summary>
                /// Gets or sets the third party repo url.
                /// </summary>
                public string Url { get; set; }

                /// <summary>
                /// Gets or sets a value indicating whether the third party repo is enabled.
                /// </summary>
                public bool IsEnabled { get; set; }

                /// <summary>
                /// Gets or sets a short name for the repo url.
                /// </summary>
                public string Name { get; set; }

                /// <summary>
                /// Clone this object.
                /// </summary>
                /// <returns>A shallow copy of this object.</returns>
                public ThirdPartyRepoSettings Clone() => this.MemberwiseClone() as ThirdPartyRepoSettings;
            }

            public List<ThirdPartyRepoSettings> ThirdRepo => new();

            public bool HasThirdRepo { get; set; }
        }
    }
}
