using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Octokit;

namespace ObsidianLauncher.ViewModels
{
    public struct DownloadResult
    {
        public readonly bool Succes = false;
        public string Message = string.Empty;
        public DownloadResult(bool success, string message)
        {
            Succes = success;
            Message = message;
        }
    }
    public static class Download
    {
        private const string RepositoryOwner = "Custom-Extension-Works";
        private const string RepositoryName = "Project-Obsidian";

        public static async Task<DownloadResult> DownloadAndInstallObsidian(string ObsidianPath, string ObsidianDirectory)
        {
            ObsidianDirectory = Path.Combine(ObsidianPath, "Libraries", "Obsidian");
            Directory.CreateDirectory(ObsidianDirectory);
            string versionFilePath = Path.Combine(ObsidianDirectory, "version.txt");

            GitHubClient gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("ObsidianLauncher"));
            Release latestRelease = await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);

            // Read the current version from the version.txt file or set it to an empty string if not found
            string currentVersion = File.Exists(versionFilePath) ? File.ReadAllText(versionFilePath) : "";

            if (currentVersion != latestRelease.TagName)
            {
                // Filter the assets to find the release zip file
                ReleaseAsset latestReleaseZipAsset = latestRelease.Assets
                    .FirstOrDefault(a => a.Name.EndsWith(".zip") && a.Name.StartsWith($"{latestRelease.TagName}"));

                if (latestReleaseZipAsset == null)
                {
                    return new DownloadResult(false, "No suitable Obsidian release found.");
                }

                string latestReleaseUrl = latestReleaseZipAsset.BrowserDownloadUrl;
                string localZipFilePath = Path.Combine(ObsidianDirectory, $"Obsidian_{latestRelease.TagName}.zip");

                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync(latestReleaseUrl);

                        if (!response.IsSuccessStatusCode)
                        {
                            return new DownloadResult(false, "Failed to download Obsidian.");
                        }

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                stream = new FileStream(localZipFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                        {
                            await contentStream.CopyToAsync(stream);
                        }
                    }

                    // Extract the zip file to the ObsidianDirectory 
                    ZipFile.ExtractToDirectory(localZipFilePath, ObsidianDirectory, true);

                    // Update the version information in the version.txt file
                    await File.WriteAllTextAsync(versionFilePath, latestRelease.TagName);

                    return new DownloadResult(true, "Obsidian downloaded and installed successfully.");
                }
                catch (Exception ex)
                {
                    return new DownloadResult(false, $"Failed to download or install Obsidian: {ex.Message}");
                }
                finally
                {
                    // Ensure the zip file is deleted regardless of whether the extraction was successful or not
                    if (File.Exists(localZipFilePath))
                    {
                        File.Delete(localZipFilePath);
                    }
                }
            }
            else
            {
                return new DownloadResult(false, $"Obsidian is up-to-date.");
            }
        }
    }
}
