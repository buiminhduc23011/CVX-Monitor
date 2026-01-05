using System.Windows;

namespace CVX_QLSX.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.MainViewModel;
    }
}