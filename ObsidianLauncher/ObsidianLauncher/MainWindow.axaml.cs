using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ObsidianLauncher.ViewModels;

namespace ObsidianLauncher
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _viewModel = new MainWindowViewModel(this);
            DataContext = _viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnOverlayPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_viewModel != null && e.Source == sender)
            {
                _viewModel.IsMenuOpen = false;
            }
        }
    }
}