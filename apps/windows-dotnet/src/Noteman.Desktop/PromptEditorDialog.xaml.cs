using System.Windows;

namespace Noteman.Desktop;

public partial class PromptEditorDialog : Window
{
    public string PromptTitle => TitleBox.Text.Trim();
    public string PromptBody => BodyBox.Text.Trim();
    public bool KeepAfterClosing => PersistentBox.IsChecked == true;

    public PromptEditorDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => TitleBox.Focus();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (PromptTitle.Length == 0 || PromptBody.Length == 0)
        {
            MessageBox.Show(this, "Enter both a prompt name and prompt text.", "NoteMan");
            return;
        }

        DialogResult = true;
    }
}
