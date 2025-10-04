using System;
using System.Globalization;
using ReactiveUI;

namespace ObsidianLauncher.Localization
{
    public class LocalizationManager : ReactiveObject
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private CultureInfo _currentCulture;

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    Resources.Culture = value;
                    this.RaisePropertyChanged(nameof(CurrentCulture));
                    OnLanguageChanged?.Invoke();
                }
            }
        }

        public event Action? OnLanguageChanged;

        private LocalizationManager()
        {
            _currentCulture = CultureInfo.CurrentCulture;
        }

        public string GetString(string key)
        {
            var resourceManager = Resources.ResourceManager;
            return resourceManager.GetString(key, CurrentCulture) ?? key;
        }
    }
}