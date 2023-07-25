using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Yarn;

namespace Helpy.Shared.Conversation
{
    public class HelpyLibrary : Library
    {
        private TroubleContext trouble;

        enum DalamudAvailabilityStatus
        {
            Unknown,
            Unavailable,
            Available
        }

        private class ThaliakGqlModel
        {
            public class Data
            {
                [JsonPropertyName("repository")]
                public Repository? Repository { get; set; }
            }

            public class LatestVersion
            {
                [JsonPropertyName("versionString")]
                public string? VersionString { get; set; }
            }

            public class Repository
            {
                [JsonPropertyName("latestVersion")]
                public LatestVersion? LatestVersion { get; set; }
            }

            public class Root
            {
                [JsonPropertyName("data")]
                public Data? Data { get; set; }
            }
        }

        private class DalamudReleaseData
        {
            [JsonPropertyName("supportedGameVer")]
            public string? SupportedGameVer { get; set; }
        }

        private DalamudAvailabilityStatus dalamudStatus = DalamudAvailabilityStatus.Unknown;

        public HelpyLibrary(TroubleContext trouble)
        {
            this.trouble = trouble;

            RegisterFunction("GetPlatform", FunGetPlatform);
            RegisterFunction("GetIndexMismatchKind", FunGetIndexMismatchKind);
            RegisterFunction("HasVerBckMismatch", FunHasVerBckMismatch);
            RegisterFunction("HasAutoLogin", FunHasAutoLogin);
            RegisterFunction("HasThirdPlugins", FunHasThirdPlugins);
            RegisterFunction("HasSpecificPlugin", FunHasSpecificPlugin);
            RegisterFunction("IsCrashTsPack", FunIsCrashTsPack);
            RegisterFunction("RegexLastExceptionDalamud", FunRegexLastExceptionDalamud);
            RegisterFunction("RegexLastExceptionXL", FunRegexLastExceptionXL);
            RegisterFunction("RegexCrashLog", FunRegexCrashLog);
            RegisterFunction("HasRecentExceptionDalamud", FunHasRecentExceptionDalamud);
            RegisterFunction("HasRecentExceptionXL", FunHasRecentExceptionXL);
            RegisterFunction("HasRecentException", FunHasRecentException);
            RegisterFunction("IsDalamudReleaseStatusGotten", FunIsDalamudReleaseStatusGotten);
            RegisterFunction("IsDalamudReleaseAvailable", FunIsDalamudReleaseAvailable);

            Task.Run(async () =>
            {
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Helpy", "1.0.0"));

                    var dalamudReleaseJson =
                        await client.GetFromJsonAsync<DalamudReleaseData>(
                            "https://kamori.goats.dev/Dalamud/Release/VersionInfo?track=release&appId=helpy");

                    if (dalamudReleaseJson?.SupportedGameVer == null)
                    {
                        Console.WriteLine("Kamori response invalid");
                        this.dalamudStatus = DalamudAvailabilityStatus.Unknown;
                        return;
                    }
                    
                    var thaliakGqlMessage =
                        new HttpRequestMessage(HttpMethod.Post, "https://thaliak.xiv.dev/graphql/2022-08-14");
                    thaliakGqlMessage.Content =
                        new StringContent(
                            "{\"query\":\"query GetLatestGameVersion {  repository(slug:\\\"4e9a232b\\\") {    latestVersion {      versionString    }  }}\"}",
                            Encoding.UTF8, "application/json");

                    var thaliakResponse = await client.SendAsync(thaliakGqlMessage);
                    thaliakResponse.EnsureSuccessStatusCode();
                    var thaliakJson = await thaliakResponse.Content.ReadFromJsonAsync<ThaliakGqlModel.Root>();

                    if (thaliakJson?.Data?.Repository?.LatestVersion == null)
                    {
                        Console.WriteLine("Thaliak response invalid");
                        this.dalamudStatus = DalamudAvailabilityStatus.Unknown;
                        return;
                    }

                    if (thaliakJson.Data.Repository.LatestVersion.VersionString == dalamudReleaseJson.SupportedGameVer)
                    {
                        dalamudStatus = DalamudAvailabilityStatus.Available;
                        Console.WriteLine("Dalamud OK!");
                    }
                    else
                    {
                        Console.WriteLine("No Dalamud yet");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Can't fetch dalamud availability\n{ex}");
                    dalamudStatus = DalamudAvailabilityStatus.Unknown;
                }
            });
        }

        private void CheckPrecondition()
        {
            if (!trouble.IsPackLoaded)
                throw new Exception("Trouble function called, but no pack was loaded. Something is wrong with the flow.");
        }

        private int FunGetPlatform()
        {
            CheckPrecondition();

            return trouble.PayloadXL!.Platform;
        }

        private int FunGetIndexMismatchKind()
        {
            CheckPrecondition();

            return (int) trouble.PayloadXL!.IndexIntegrity;
        }

        private bool FunHasVerBckMismatch()
        {
            CheckPrecondition();

            return !trouble.PayloadXL!.BckMatch;
        }

        private bool FunHasAutoLogin()
        {
            CheckPrecondition();

            return trouble.PayloadXL!.IsAutoLogin;
        }

        private bool FunHasThirdPlugins()
        {
            CheckPrecondition();

            return trouble.PayloadDalamud?.HasThirdRepo ?? false;
        }

        private bool FunHasSpecificPlugin(string name)
        {
            CheckPrecondition();

            return trouble.PayloadDalamud?.LoadedPlugins.Any(x => x.InternalName == name) ?? false;
        }
        
        private bool FunIsCrashTsPack()
        {
            CheckPrecondition();

            return trouble.IsCrashPack;
        }

        private bool FunRegexLastExceptionDalamud(string regexStr)
        {
            CheckPrecondition();

            if (trouble.LastExceptionDalamud == null)
                return false;

            var regex = new Regex(regexStr);
            return regex.IsMatch(trouble.LastExceptionDalamud.Info);
        }

        private bool FunRegexLastExceptionXL(string regexStr)
        {
            CheckPrecondition();

            if (trouble.LastExceptionXL == null)
                return false;

            var regex = new Regex(regexStr);
            return regex.IsMatch(trouble.LastExceptionXL.Info);
        }
        
        private bool FunRegexCrashLog(string regexStr)
        {
            CheckPrecondition();

            if (trouble.CrashLog == null)
                return false;

            var regex = new Regex(regexStr);
            return regex.IsMatch(trouble.CrashLog);
        }

        const int ExceptionDaysLimit = 2;

        private bool FunHasRecentExceptionDalamud()
        {
            CheckPrecondition();

            if (trouble.LastExceptionDalamud == null)
                return false;

            var timePassed = DateTime.Now - trouble.LastExceptionDalamud.When;
            return timePassed.Days <= ExceptionDaysLimit;
        }

        private bool FunHasRecentExceptionXL()
        {
            CheckPrecondition();

            if (trouble.LastExceptionXL == null)
                return false;

            var timePassed = DateTime.Now - trouble.LastExceptionXL.When;
            return timePassed.Days <= ExceptionDaysLimit;
        }

        private bool FunHasRecentException()
        {
            CheckPrecondition();

            return FunHasRecentExceptionDalamud() || FunHasRecentExceptionXL();
        }

        private bool FunIsDalamudReleaseStatusGotten()
        {
            return dalamudStatus != DalamudAvailabilityStatus.Unknown;
        }

        private bool FunIsDalamudReleaseAvailable()
        {
            return dalamudStatus == DalamudAvailabilityStatus.Available;
        }
    }
}
