using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace ObsidianLauncher.ViewModels
{
    public struct UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string DownloadUrl { get; set; }
    }

    public static class LauncherUpdateChecker
    {
        private const string RepositoryOwner = "Custom-Extension-Works";
        private const string RepositoryName = "Project-Obsidian-Launcher";

        public static async Task<UpdateCheckResult> CheckForUpdate(string currentVersion)
        {
            try
            {
                GitHubClient gitHubClient = new GitHubClient(new ProductHeaderValue("ObsidianLauncher"));
                Release latestRelease = await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);

                // Remove 'v' prefix if present for comparison
                string current = currentVersion.TrimStart('v');
                string latest = latestRelease.TagName.TrimStart('v');

                bool updateAvailable = IsNewerVersion(latest, current);

                return new UpdateCheckResult
                {
                    UpdateAvailable = updateAvailable,
                    LatestVersion = latestRelease.TagName,
                    DownloadUrl = latestRelease.HtmlUrl
                };
            }
            catch
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    LatestVersion = currentVersion,
                    DownloadUrl = ""
                };
            }
        }

        private static bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                var latest = ParseVersion(latestVersion);
                var current = ParseVersion(currentVersion);

                // Compare major, minor, patch
                if (latest.Major > current.Major) return true;
                if (latest.Major < current.Major) return false;

                if (latest.Minor > current.Minor) return true;
                if (latest.Minor < current.Minor) return false;

                if (latest.Patch > current.Patch) return true;

                return false;
            }
            catch
            {
                // If parsing fails, fall back to string comparison
                return string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
            }
        }

        private static (int Major, int Minor, int Patch) ParseVersion(string version)
        {
            var parts = version.Split('.').Select(p => int.Parse(p.Trim())).ToArray();

            int major = parts.Length > 0 ? parts[0] : 0;
            int minor = parts.Length > 1 ? parts[1] : 0;
            int patch = parts.Length > 2 ? parts[2] : 0;

            return (major, minor, patch);
        }
    }
}