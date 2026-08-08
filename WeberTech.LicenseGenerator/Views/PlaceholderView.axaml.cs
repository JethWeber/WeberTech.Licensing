using Avalonia.Controls;

namespace WeberTech.LicenseGenerator.Views;

public partial class PlaceholderView : UserControl
{
    /// <summary>Construtor sem parâmetros — só para o designer/preview do Avalonia.</summary>
    public PlaceholderView() : this("Placeholder", string.Empty) { }

    public PlaceholderView(string title, string note)
    {
        InitializeComponent();
        TitleText.Text = title;
        NoteText.Text = note;
    }
}
