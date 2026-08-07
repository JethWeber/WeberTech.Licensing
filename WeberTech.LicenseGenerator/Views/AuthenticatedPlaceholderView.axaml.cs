using Avalonia.Controls;
using WeberTech.LicenseGenerator.Entities;

namespace WeberTech.LicenseGenerator.Views;

public partial class AuthenticatedPlaceholderView : UserControl
{
    /// <summary>Construtor sem parâmetros — só para o designer/preview do Avalonia.</summary>
    public AuthenticatedPlaceholderView() : this(null, () => { }) { }

    public AuthenticatedPlaceholderView(User? user, Action onLogout)
    {
        InitializeComponent();

        SessionLabel.Text = user is not null
            ? $"Sessão: {user.DisplayName} ({user.Username})"
            : string.Empty;

        LogoutButton.Click += (_, _) => onLogout();
    }
}
