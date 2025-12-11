using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace CVX_QLSX.App.Views;

/// <summary>
/// Login dialog for accessing Settings.
/// </summary>
public partial class LoginDialog : UserControl
{
    public string Username => UsernameTextBox.Text;
    public string Password => PasswordBox.Password;

    public LoginDialog()
    {
        InitializeComponent();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        // Close dialog with result
        DialogHost.CloseDialogCommand.Execute(true, this);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogHost.CloseDialogCommand.Execute(false, this);
    }
}
