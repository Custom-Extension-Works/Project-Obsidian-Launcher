using System.IO;
using System.Text.Json;

namespace ObsidianLauncher.ViewModels
{
    public class Config
    {
        private const string ConfigFilePath = "./Assets/Config.json";

        public string LauncherArguments { get; set; }
        public string CustomInstallDir { get; set; }
        public bool IsDarkMode { get; set; } = true;
        public string Language { get; set; } = "en"; // NEW

        public static Config LoadConfig()
        {
            try
            {
                string json = File.ReadAllText(ConfigFilePath);
                return JsonSerializer.Deserialize<Config>(json) ?? new Config();
            }
            catch (FileNotFoundException)
            {
                return new Config();
            }
            catch
            {
                return new Config();
            }
        }

        public void SaveConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch
            {
                // Silently fail if we can't save config
            }
        }
    }
}