using System.Windows;

namespace FakeChatStudio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void AddCharacter_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Character creation will be connected to the project model in the next step.",
            "FakeChat Studio", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}