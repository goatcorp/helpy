using System.Text.Json.Serialization;
using System.Xml;

namespace Helpy.Conversation
{
    /// <summary>
    /// Information about a plugin, packaged in a json file with the DLL.
    /// </summary>
    public record PluginManifest
    {
        /// <summary>
        /// Gets the author/s of the plugin.
        /// </summary>
        public string? Author { get; init; }

        /// <summary>
        /// Gets or sets the public name of the plugin.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets a punchline of the plugins functions.
        /// </summary>
        public string? Punchline { get; init; }

        /// <summary>
        /// Gets a description of the plugins functions.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets a changelog.
        /// </summary>
        public string? Changelog { get; init; }

        /// <summary>
        /// Gets a list of tags defined on the plugin.
        /// </summary>
        public List<string>? Tags { get; init; }

        /// <summary>
        /// Gets a list of category tags defined on the plugin.
        /// </summary>
        public List<string>? CategoryTags { get; init; }

        /// <summary>
        /// Gets a value indicating whether or not the plugin is hidden in the plugin installer.
        /// This value comes from the plugin master and is in addition to the list of hidden names kept by Dalamud.
        /// </summary>
        public bool IsHide { get; init; }

        /// <summary>
        /// Gets the internal name of the plugin, which should match the assembly name of the plugin.
        /// </summary>
        public string InternalName { get; init; } = null!;

        /// <summary>
        /// Gets the current assembly version of the plugin.
        /// </summary>
        public Version AssemblyVersion { get; init; } = null!;

        /// <summary>
        /// Gets the current testing assembly version of the plugin.
        /// </summary>
        public Version? TestingAssemblyVersion { get; init; }

        /// <summary>
        /// Gets a value indicating whether the plugin is only available for testing.
        /// </summary>
        public bool IsTestingExclusive { get; init; }

        /// <summary>
        /// Gets an URL to the website or source code of the plugin.
        /// </summary>
        public string? RepoUrl { get; init; }


        /// <summary>
        /// Gets the API level of this plugin. For the current API level, please see <see cref="PluginManager.DalamudApiLevel"/>
        /// for the currently used API level.
        /// </summary>
        public int DalamudApiLevel { get; init; }

        /// <summary>
        /// Gets the number of downloads this plugin has.
        /// </summary>
        public long DownloadCount { get; init; }

        /// <summary>
        /// Gets the last time this plugin was updated.
        /// </summary>
        public long LastUpdate { get; init; }

        /// <summary>
        /// Gets the download link used to install the plugin.
        /// </summary>
        public string DownloadLinkInstall { get; init; } = null!;

        /// <summary>
        /// Gets the download link used to update the plugin.
        /// </summary>
        public string DownloadLinkUpdate { get; init; } = null!;

        /// <summary>
        /// Gets the download link used to get testing versions of the plugin.
        /// </summary>
        public string DownloadLinkTesting { get; init; } = null!;

        /// <summary>
        /// Gets the required Dalamud load step for this plugin to load. Takes precedence over LoadPriority.
        /// Valid values are:
        /// 0. During Framework.Tick, when drawing facilities are available
        /// 1. During Framework.Tick
        /// 2. No requirement
        /// </summary>
        public int LoadRequiredState { get; init; }

        /// <summary>
        /// Gets a value indicating whether Dalamud must load this plugin not at the same time with other plugins and the game.
        /// </summary>
        public bool LoadSync { get; init; }

        /// <summary>
        /// Gets the load priority for this plugin. Higher values means higher priority. 0 is default priority.
        /// </summary>
        public int LoadPriority { get; init; }

        /// <summary>
        /// Gets a value indicating whether the plugin can be unloaded asynchronously. 
        /// </summary>
        public bool CanUnloadAsync { get; init; }

        /// <summary>
        /// Gets a list of screenshot image URLs to show in the plugin installer.
        /// </summary>
        public List<string>? ImageUrls { get; init; }

        /// <summary>
        /// Gets an URL for the plugin's icon.
        /// </summary>
        public string? IconUrl { get; init; }

        /// <summary>
        /// Gets a value indicating whether this plugin accepts feedback.
        /// </summary>
        public bool AcceptsFeedback { get; init; } = true;

        /// <summary>
        /// Gets a message that is shown to users when sending feedback.
        /// </summary>
        public string? FeedbackMessage { get; init; }

        /// <summary>
        /// Gets a value indicating whether this plugin is DIP17.
        /// To be removed.
        /// </summary>
        [JsonPropertyName("_isDip17Plugin")]
        public bool IsDip17Plugin { get; init; } = false;

        /// <summary>
        /// Gets the DIP17 channel name.
        /// </summary>
        [JsonPropertyName("_Dip17Channel")]
        public string? Dip17Channel { get; init; }
    }

    /// <summary>
    /// Information about a plugin, packaged in a json file with the DLL. This variant includes additional information such as
    /// if the plugin is disabled and if it was installed from a testing URL. This is designed for use with manifests on disk.
    /// </summary>
    public record LocalPluginManifest : PluginManifest
    {
        [JsonIgnore]
        public const string FlagMainRepo = "OFFICIAL";

        /// <summary>
        /// Gets or sets a value indicating whether the plugin is disabled and should not be loaded.
        /// This value supersedes the ".disabled" file functionality and should not be included in the plugin master.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plugin should only be loaded when testing is enabled.
        /// This value supersedes the ".testing" file functionality and should not be included in the plugin master.
        /// </summary>
        public bool Testing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plugin should be deleted during the next cleanup.
        /// </summary>
        public bool ScheduledForDeletion { get; set; }

        /// <summary>
        /// Gets or sets the 3rd party repo URL that this plugin was installed from. Used to display where the plugin was
        /// sourced from on the installed plugin view. This should not be included in the plugin master. This value is null
        /// when installed from the main repo.
        /// </summary>
        public string InstalledFromUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether this manifest is associated with a plugin that was installed from a third party
        /// repo. Unless the manifest has been manually modified, this is determined by the InstalledFromUrl being null.
        /// </summary>
        public bool IsThirdParty => !string.IsNullOrEmpty(this.InstalledFromUrl) && this.InstalledFromUrl != FlagMainRepo;

        /// <summary>
        /// Gets the effective version of this plugin.
        /// </summary>
        public Version EffectiveVersion => this.Testing && this.TestingAssemblyVersion != null ? this.TestingAssemblyVersion : this.AssemblyVersion;
    }
}
