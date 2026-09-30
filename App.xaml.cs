<<<<<<< HEAD
using Microsoft.UI.Xaml;

namespace Spacey;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
=======
using Microsoft.UI.Xaml;

namespace Spacey;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
>>>>>>> 8c4b819f7c07a98dbcabcd93df0b6ea17e6b0162
}