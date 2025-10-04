using System;
using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace ObsidianLauncher.Localization
{
    public class LocalizeExtension : MarkupExtension
    {
        public string Key { get; set; }

        public LocalizeExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new ReflectionBindingExtension($"[{Key}]")
            {
                Mode = BindingMode.OneWay,
                Source = LocalizationProxy.Instance  // Use singleton instance
            };

            return binding.ProvideValue(serviceProvider);
        }
    }

    public class LocalizationProxy : INotifyPropertyChanged
    {
        private static LocalizationProxy? _instance;
        public static LocalizationProxy Instance => _instance ??= new LocalizationProxy();

        private LocalizationProxy()
        {
            LocalizationManager.Instance.OnLanguageChanged += () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            };
        }

        public string this[string key] => LocalizationManager.Instance.GetString(key);

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}