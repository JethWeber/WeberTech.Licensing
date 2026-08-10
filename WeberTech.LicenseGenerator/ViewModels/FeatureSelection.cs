using CommunityToolkit.Mvvm.ComponentModel;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>Um módulo do produto selecionado + se está marcado para entrar na licença (M6).</summary>
public sealed partial class FeatureSelection : ObservableObject
{
    public FeatureSelection(string name, bool isSelected = false)
    {
        Name = name;
        _isSelected = isSelected;
    }

    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;
}
