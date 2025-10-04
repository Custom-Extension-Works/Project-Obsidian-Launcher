using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI.Fody.Helpers;
using ObsidianLauncher.Localization;

namespace ObsidianLauncher.ViewModels
{
    public class LanguageOption
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class MainWindowViewModel : ReactiveObject
    {
        [Reactive]
        public string StatusText { get; set; } = "Ready to install";

        [Reactive]
        public string LauncherArguments { get; set; } = string.Empty;

        [Reactive]
        public bool InstallEnabled { get; set; } = true;

        [Reactive]
        public bool IsDarkMode { get; set; } = true;

        [Reactive]
        public double DownloadProgress { get; set; } = 0;

        [Reactive]
        public string ProgressText { get; set; } = string.Empty;

        [Reactive]
        public bool IsDownloading { get; set; } = false;

        [Reactive]
        public bool IsMenuOpen { get; set; } = false;

        [Reactive]
        public bool IsLauncherUpdateAvailable { get; set; } = false;

        [Reactive]
        public string CurrentVersion { get; set; } = "v1.2.0";

        [Reactive]
        public string LatestVersion { get; set; } = "";

        [Reactive]
        public string ObsidianVersion { get; set; } = "Not installed";

        [Reactive]
        public string InstallationPath { get; set; } = "Not set";
        [Reactive]
        public string SelectedLanguage { get; set; } = "en";

        [Reactive]
        public LanguageOption SelectedLanguageOption { get; set; }

        public List<LanguageOption> AvailableLanguages { get; } = new()
        {
            new LanguageOption { Code = "en", Name = "English" },
            new LanguageOption { Code = "de", Name = "Deutsch" },
        };

        // Theme Colors
        public SolidColorBrush WindowBackground => IsDarkMode
            ? new SolidColorBrush(Color.Parse("#1C1C1E"))
            : new SolidColorBrush(Color.Parse("#F5F5F7"));

        public SolidColorBrush CardBackground => IsDarkMode
            ? new SolidColorBrush(Color.Parse("#2C2C2E"))
            : new SolidColorBrush(Color.Parse("#FFFFFF"));

        public SolidColorBrush TextPrimary => IsDarkMode
            ? new SolidColorBrush(Color.Parse("#FFFFFF"))
            : new SolidColorBrush(Color.Parse("#1D1D1F"));

        public SolidColorBrush TextSecondary => IsDarkMode
            ? new SolidColorBrush(Color.Parse("#AEAEB2"))
            : new SolidColorBrush(Color.Parse("#6E6E73"));

        public SolidColorBrush BorderColor => IsDarkMode
            ? new SolidColorBrush(Color.Parse("#38383A"))
            : new SolidColorBrush(Color.Parse("#E5E5EA"));

        public SolidColorBrush InputBackground => IsDarkMode
            ? new SolidColorBrush(Color.Parse("#1C1C1E"))
            : new SolidColorBrush(Color.Parse("#F5F5F7"));

        public SolidColorBrush ProgressBackground => IsDarkMode
            ? new SolidColorBrush(Color.Parse("#38383A"))
            : new SolidColorBrush(Color.Parse("#E5E5EA"));

        public string ThemeButtonText => IsDarkMode
            ? LocalizationManager.Instance.GetString("Light")
            : LocalizationManager.Instance.GetString("Dark");

        public ReactiveCommand<Unit, Unit> InstallCommand { get; }
        public ReactiveCommand<Unit, Unit> LaunchCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleThemeCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleMenuCommand { get; }
        public ReactiveCommand<Unit, Unit> ChangePathCommand { get; }
        public ReactiveCommand<Unit, Unit> CheckUpdatesCommand { get; }
        public ReactiveCommand<Unit, Unit> UpdateLauncherCommand { get; }

        private MainWindow mainWindow;

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            // Load configuration
            Config config = Config.LoadConfig();
            LauncherArguments = config.LauncherArguments;
            IsDarkMode = config.IsDarkMode;
            SelectedLanguage = config.Language;

            // Set selected language option
            SelectedLanguageOption = AvailableLanguages.Find(l => l.Code == SelectedLanguage) ?? AvailableLanguages[0];

            // Initialize localization
            LocalizationManager.Instance.CurrentCulture = new CultureInfo(SelectedLanguage);
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

          
            // Initialize commands
            InstallCommand = ReactiveCommand.CreateFromTask(ExecuteInstall);
            LaunchCommand = ReactiveCommand.Create(LaunchProjectObsidian);
            ToggleThemeCommand = ReactiveCommand.Create(ToggleTheme);
            ToggleMenuCommand = ReactiveCommand.Create(ToggleMenu);
            ChangePathCommand = ReactiveCommand.CreateFromTask(ChangePath);
            CheckUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForLauncherUpdate);
            UpdateLauncherCommand = ReactiveCommand.Create(OpenLauncherReleasePage);

            // Subscribe to theme changes
            this.WhenAnyValue(x => x.IsDarkMode).Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ThemeButtonText));
                this.RaisePropertyChanged(nameof(WindowBackground));
                this.RaisePropertyChanged(nameof(CardBackground));
                this.RaisePropertyChanged(nameof(TextPrimary));
                this.RaisePropertyChanged(nameof(TextSecondary));
                this.RaisePropertyChanged(nameof(BorderColor));
                this.RaisePropertyChanged(nameof(InputBackground));
                this.RaisePropertyChanged(nameof(ProgressBackground));
            });

            // Subscribe to language changes
            this.WhenAnyValue(x => x.SelectedLanguageOption).Subscribe(languageOption =>
            {
                if (languageOption != null && languageOption.Code != SelectedLanguage)
                {
                    SelectedLanguage = languageOption.Code;
                    LocalizationManager.Instance.CurrentCulture = new CultureInfo(languageOption.Code);

                    Config cfg = Config.LoadConfig();
                    cfg.Language = languageOption.Code;
                    cfg.SaveConfig();

                    UpdateLocalizedStatusText();
                }
            });

            // Check for updates on startup
            _ = CheckForLauncherUpdate();
            UpdateInstallationPath();
            UpdateObsidianVersion();
            UpdateLocalizedStatusText();
        }

        private void OnLanguageChanged()
        {
            this.RaisePropertyChanged(nameof(ThemeButtonText));
            this.RaisePropertyChanged(nameof(WindowBackground));
            this.RaisePropertyChanged(nameof(CardBackground));
            this.RaisePropertyChanged(nameof(TextPrimary));
            this.RaisePropertyChanged(nameof(TextSecondary));
            this.RaisePropertyChanged(nameof(BorderColor));
            this.RaisePropertyChanged(nameof(InputBackground));
            this.RaisePropertyChanged(nameof(ProgressBackground));
            UpdateLocalizedStatusText();
        }

        private void UpdateLocalizedStatusText()
        {
            if (!IsDownloading && StatusText == "Ready to install")
            {
                StatusText = LocalizationManager.Instance.GetString("ReadyToInstall");
            }
        }

        private void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;

            Config config = Config.LoadConfig();
            config.IsDarkMode = IsDarkMode;
            config.SaveConfig();
        }

        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;

            // Save launcher arguments when closing menu
            if (!IsMenuOpen)
            {
                Config config = Config.LoadConfig();
                config.LauncherArguments = LauncherArguments;
                config.SaveConfig();
            }
        }

        private async Task ChangePath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Resonite Installation Directory",
                Directory = "."
            };
            var result = await dialog.ShowAsync(mainWindow);

            if (result != null)
            {
                Config config = Config.LoadConfig();
                config.CustomInstallDir = result;
                config.SaveConfig();
                UpdateInstallationPath();
                UpdateObsidianVersion();
            }
        }

        private void UpdateInstallationPath()
        {
            string[] paths = ObsdiianPathHelper.GetObsidianPath();
            InstallationPath = paths.Length > 0 ? paths[0] : LocalizationManager.Instance.GetString("NotFound");
        }

        private void UpdateObsidianVersion()
        {
            string[] paths = ObsdiianPathHelper.GetObsidianPath();
            if (paths.Length > 0)
            {
                string versionFile = Path.Combine(paths[0], "Libraries", "Obsidian", "version.txt");
                if (File.Exists(versionFile))
                {
                    ObsidianVersion = File.ReadAllText(versionFile).Trim();
                }
                else
                {
                    ObsidianVersion = LocalizationManager.Instance.GetString("NotInstalled");
                }
            }
            else
            {
                ObsidianVersion = LocalizationManager.Instance.GetString("NotInstalled");
            }
        }

        private async Task CheckForLauncherUpdate()
        {
            try
            {
                var result = await LauncherUpdateChecker.CheckForUpdate(CurrentVersion);
                IsLauncherUpdateAvailable = result.UpdateAvailable;
                if (result.UpdateAvailable)
                {
                    LatestVersion = result.LatestVersion;
                }
            }
            catch
            {
                // Silently fail if we can't check for updates
            }
        }

        private void OpenLauncherReleasePage()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/Custom-Extension-Works/Project-Obsidian-Launcher/releases/latest",
                    UseShellExecute = true
                });
            }
            catch
            {
                StatusText = LocalizationManager.Instance.GetString("FailedToOpenBrowser");
            }
        }

        private async Task ExecuteInstall()
        {
            try
            {
                InstallEnabled = false;
                IsDownloading = true;
                StatusText = LocalizationManager.Instance.GetString("CheckingForUpdates");
                DownloadProgress = 0;
                ProgressText = "";

                string[] projectObsidianPaths = ObsdiianPathHelper.GetObsidianPath();
                string projectObsidianPath = null;

                if (projectObsidianPaths.Length > 0)
                {
                    projectObsidianPath = projectObsidianPaths[0];
                }
                else
                {
                    var dialog = new OpenFolderDialog
                    {
                        Title = "Select Resonite Installation Directory",
                        Directory = "."
                    };
                    var result = await dialog.ShowAsync(mainWindow);

                    if (result == null)
                    {
                        StatusText = LocalizationManager.Instance.GetString("NoDirectorySelected");
                        InstallEnabled = true;
                        IsDownloading = false;
                        return;
                    }

                    projectObsidianPath = result;

                    Config config = Config.LoadConfig();
                    config.CustomInstallDir = projectObsidianPath;
                    config.SaveConfig();
                    UpdateInstallationPath();
                }

                string projectObsidianPlusDirectory = Path.Combine(projectObsidianPath, "Libraries", "Obsidian");

                bool downloadSuccess = await DownloadProjectObsidianPlus(projectObsidianPath, projectObsidianPlusDirectory);

                if (downloadSuccess)
                {
                    StatusText = LocalizationManager.Instance.GetString("InstallationComplete");
                    DownloadProgress = 100;
                    ProgressText = LocalizationManager.Instance.GetString("ReadyToLaunch");
                    UpdateObsidianVersion();

                    await Task.Delay(2000);
                }

                IsDownloading = false;
                InstallEnabled = true;
            }
            catch (Exception ex)
            {
                StatusText = $"{LocalizationManager.Instance.GetString("InstallationFailed")}: {ex.Message}";
                IsDownloading = false;
                InstallEnabled = true;
            }
        }

        private async Task<bool> DownloadProjectObsidianPlus(string projectObsidianPath, string projectObsidianPlusDirectory)
        {
            try
            {
                var progressCallback = new Progress<DownloadProgress>(progress =>
                {
                    DownloadProgress = progress.Percentage;
                    ProgressText = progress.Message;
                    StatusText = progress.Status;
                });

                DownloadResult res = await Download.DownloadAndInstallObsidian(
                    projectObsidianPath,
                    projectObsidianPlusDirectory,
                    progressCallback);

                if (!res.Success)
                {
                    StatusText = res.Message;
                }

                return res.Success;
            }
            catch (Exception ex)
            {
                StatusText = $"{LocalizationManager.Instance.GetString("DownloadError")}: {ex.Message}";
                return false;
            }
        }

        private void LaunchProjectObsidian()
        {
            string[] projectObsidianPaths = ObsdiianPathHelper.GetObsidianPath();

            if (projectObsidianPaths.Length == 0)
            {
                StatusText = LocalizationManager.Instance.GetString("NoInstallationFound");
                return;
            }

            var path = projectObsidianPaths[0];
            string projectObsidianPlusDirectory = Path.Combine(path, "Libraries", "Obsidian");

            LaunchProjectObsidian(path, projectObsidianPlusDirectory);
            StatusText = LocalizationManager.Instance.GetString("LaunchedSuccessfully");
        }

        private void LaunchProjectObsidian(string ObsidianPath, string projectObsidianPlusDirectory)
        {
            string projectObsidianExePath = Path.Combine(ObsidianPath, "Resonite.exe");
            string projectObsidianPlusDllPath = Path.Combine(projectObsidianPlusDirectory, "Project-Obsidian.dll");
            string arguments = $"-LoadAssembly \"{projectObsidianPlusDllPath}\"";

            string launcherArguments = LauncherArguments.Trim();
            if (!string.IsNullOrEmpty(launcherArguments))
            {
                arguments += $" {launcherArguments}";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(projectObsidianExePath, arguments)
            {
                WorkingDirectory = ObsidianPath
            };

            try
            {
                Process.Start(startInfo);

                Config config = Config.LoadConfig();
                config.LauncherArguments = launcherArguments;
                config.SaveConfig();
            }
            catch (Exception ex)
            {
                StatusText = $"{LocalizationManager.Instance.GetString("LaunchFailed")}: {ex.Message}";
            }
        }
    }
}