using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace WeberTech.LicenseGenerator.Services;

/// <summary>
/// Encapsula os diálogos nativos de escolher/gravar ficheiro (Avalonia
/// <c>IStorageProvider</c>). Recebe o <see cref="TopLevel"/> via delegate
/// em vez de guardar uma referência direta — assim o ViewModel que usa
/// isto não precisa de saber qual janela está ativa, só passa
/// <c>() =&gt; mainWindow</c> (ou equivalente) uma vez, na construção.
/// </summary>
public sealed class FileDialogService
{
    private readonly Func<TopLevel?> _topLevelProvider;

    public FileDialogService(Func<TopLevel?> topLevelProvider)
    {
        _topLevelProvider = topLevelProvider ?? throw new ArgumentNullException(nameof(topLevelProvider));
    }

    /// <summary>Devolve o caminho local do ficheiro escolhido, ou <c>null</c> se o utilizador cancelou.</summary>
    public async Task<string?> PickPrivateKeyFileAsync()
    {
        TopLevel? topLevel = _topLevelProvider();
        if (topLevel?.StorageProvider is not { } storageProvider)
            return null;

        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecionar chave privada (.pem)",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Chave PEM") { Patterns = ["*.pem"] }]
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    /// <summary>Devolve o caminho local onde gravar, ou <c>null</c> se o utilizador cancelou.</summary>
    public async Task<string?> PickSaveWtaFileAsync(string suggestedFileName)
    {
        TopLevel? topLevel = _topLevelProvider();
        if (topLevel?.StorageProvider is not { } storageProvider)
            return null;

        IStorageFile? file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Gravar ficheiro de licença",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "wta",
            FileTypeChoices = [new FilePickerFileType("Licença WeberTech (.wta)") { Patterns = ["*.wta"] }]
        });

        return file?.Path.LocalPath;
    }
}
