using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;

namespace ObsidianLauncher.ViewModels
{
    public struct DownloadProgress
    {
        public double Percentage { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
    }

    public struct DownloadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public DownloadResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    public static class Download
    {
        private const string RepositoryOwner = "Custom-Extension-Works";
        private const string RepositoryName = "Project-Obsidian";

        public static async Task<DownloadResult> DownloadAndInstallObsidian(
            string ObsidianPath,
            string ObsidianDirectory,
            IProgress<DownloadProgress> progress = null)
        {
            ObsidianDirectory = Path.Combine(ObsidianPath, "Libraries", "Obsidian");
            Directory.CreateDirectory(ObsidianDirectory);
            string versionFilePath = Path.Combine(ObsidianDirectory, "version.txt");

            progress?.Report(new DownloadProgress
            {
                Percentage = 10,
                Message = "Connecting...",
                Status = "Checking for updates..."
            });

            GitHubClient gitHubClient = new GitHubClient(new ProductHeaderValue("ObsidianLauncher"));
            Release latestRelease;

            try
            {
                latestRelease = await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);
            }
            catch (Exception ex)
            {
                return new DownloadResult(false, $"Failed to check for updates: {ex.Message}");
            }

            string currentVersion = File.Exists(versionFilePath) ? File.ReadAllText(versionFilePath) : "";

            progress?.Report(new DownloadProgress
            {
                Percentage = 20,
                Message = $"Latest version: {latestRelease.TagName}",
                Status = "Checking version..."
            });

            if (currentVersion == latestRelease.TagName)
            {
                progress?.Report(new DownloadProgress
                {
                    Percentage = 100,
                    Message = "Already up to date",
                    Status = "Obsidian is up to date ✓"
                });
                return new DownloadResult(false, "Obsidian is already up to date ✓");
            }

            ReleaseAsset latestReleaseZipAsset = latestRelease.Assets
                .FirstOrDefault(a => a.Name.EndsWith(".zip") && a.Name.StartsWith($"{latestRelease.TagName}"));

            if (latestReleaseZipAsset == null)
            {
                return new DownloadResult(false, "No suitable release package found");
            }

            string latestReleaseUrl = latestReleaseZipAsset.BrowserDownloadUrl;
            string localZipFilePath = Path.Combine(ObsidianDirectory, $"Obsidian_{latestRelease.TagName}.zip");

            try
            {
                progress?.Report(new DownloadProgress
                {
                    Percentage = 25,
                    Message = "Preparing download...",
                    Status = "Downloading Obsidian..."
                });

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(10);

                    using (var response = await httpClient.GetAsync(latestReleaseUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            return new DownloadResult(false, "Failed to download from server");
                        }

                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = totalBytes != -1;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(localZipFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var totalRead = 0L;
                            var buffer = new byte[8192];
                            var isMoreToRead = true;

                            do
                            {
                                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                                if (read == 0)
                                {
                                    isMoreToRead = false;
                                }
                                else
                                {
                                    await fileStream.WriteAsync(buffer, 0, read);
                                    totalRead += read;

                                    if (canReportProgress)
                                    {
                                        var progressPercentage = 25 + ((double)totalRead / totalBytes * 50);
                                        var downloadedMB = totalRead / 1024.0 / 1024.0;
                                        var totalMB = totalBytes / 1024.0 / 1024.0;

                                        progress?.Report(new DownloadProgress
                                        {
                                            Percentage = progressPercentage,
                                            Message = $"{downloadedMB:F1} MB / {totalMB:F1} MB",
                                            Status = "Downloading Obsidian..."
                                        });
                                    }
                                }
                            }
                            while (isMoreToRead);
                        }
                    }
                }

                progress?.Report(new DownloadProgress
                {
                    Percentage = 80,
                    Message = "Extracting files...",
                    Status = "Installing Obsidian..."
                });

                ZipFile.ExtractToDirectory(localZipFilePath, ObsidianDirectory, true);

                progress?.Report(new DownloadProgress
                {
                    Percentage = 95,
                    Message = "Finalizing...",
                    Status = "Completing installation..."
                });

                await File.WriteAllTextAsync(versionFilePath, latestRelease.TagName);

                progress?.Report(new DownloadProgress
                {
                    Percentage = 100,
                    Message = "Complete!",
                    Status = "Installation complete ✓"
                });

                return new DownloadResult(true, "Obsidian installed successfully ✓");
            }
            catch (Exception ex)
            {
                return new DownloadResult(false, $"Installation failed: {ex.Message}");
            }
            finally
            {
                if (File.Exists(localZipFilePath))
                {
                    try { File.Delete(localZipFilePath); } catch { }
                }
            }
        }
    }
}