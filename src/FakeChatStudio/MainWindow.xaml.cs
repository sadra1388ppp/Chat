namespace FakeChatStudio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Project editor is the next step. The foundation is ready!",
            "FakeChat Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
