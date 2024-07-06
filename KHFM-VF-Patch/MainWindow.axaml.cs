using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Ionic.Zip;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;

namespace KHFM_VF_Patch;

public partial class MainWindow : Window
{
    #region Events

    // This event is implemented by "INotifyPropertyChanged" and is all we need to inform 
    // our View about changes.
    public event PropertyChangedEventHandler? PropertyChanged;

    // For convenience we add a method which will raise the above event.
    private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Constants

    private readonly string PROJECT_DIRECTORY;
    private const string PATCH_FOLDER_RELATIVE_PATH = "Resources/Patches";
    private static readonly string PATCH_FOLDER = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), PATCH_FOLDER_RELATIVE_PATH);
    private const int REQUIRED_RANDOM_QUOTES_COUNT = 3;

    private static readonly List<string> RANDOM_QUOTES =
    [
        "Cela pourrait �tre un peu long, je vous conseille d'aller chercher un caf�...",
        "Sinon, votre journ�e s'est bien pass�e ?",
        "Vous �tes toujours l� ? Alors attendez, je vous pr�pare de quoi vous occuper...",
        "Saviez-vous que Donald Reignoux, l'excellent doubleur de Sora, a �galement doubl� Titeuf, L�cio dans Overwatch, mais aussi Connor dans Detroit: Become Human ?",
        "Richard Darbois est connu pour avoir fait des voix embl�matiques, dont celle d'Indiana Jones ou de Buzz l'�clair, mais saviez-vous qu'il a particip� � ce Kingdom Hearts en doublant le G�nie et Oogie Boogie ?",
        "Kingdom Hearts est le premier jeu de la s�rie, mais ce n'est pas par lui que commence v�ritablement l'histoire.",
        "Si comme moi vous n'avez jamais fait la version Final Mix du jeu, sachez qu'un boss plus compliqu� que Sephiroth vous attend ! J'ai tellement h�te !",
        "Ce patch a n�cessit� des dizaines d'heures de travail et totalise des centaines de commits sur trois repos diff�rents !",
        "Un patch pour r�tablir les voix fran�aises sur Kingdom Hearts 2 a �t� cr�� par TieuLink sur le site Nexus Mods !",
        "Une modification future de ce patch permettra peut-�tre de supprimer les sous-titres pour renforcer l'immersion.",
        "Saviez-vous que Mickey �tait cens� �tre le protagoniste principal du jeu � la place de Sora ? Cela expliquerait pourquoi Donald et Dingo sont ses compagnons.",
        "Disney ne voulait pas que Mickey soit dans le jeu. Ils ont finalement accept�, mais � la condition qu'il ne soit pr�sent que dans une seule sc�ne du jeu. Nomura a choisi de le mettre � la fin.",
        "Yoko Shimomura, la compositrice du jeu, a �galement compos� les musiques de Street Fighter 2, Legend of Mana, Xenoblade Chronicles et Final Fantasy XV.",
        "La musique One-Winged Angel pr�sente dans le jeu vient de Final Fantasy VII et a �t� compos�e par Nobuo Uematsu.",
        "Disney �tait tr�s furieux quand ils ont vu qu'Ariel pouvait se battre. Pour s'excuser, Square Enix a �t� forc� d'en faire un monde musical dans Kingdom Hearts 2...",
        "Dans la Forteresse Oubli�e, il y a une entr�e pour un ascenseur pr�s du sommet mais... pas d'ascenseur !",
        "L'�le du Destin dans la fin des mondes s'appelle �le du Souvenir.",
        "Quand on d�truit la maison de Bourriquet, elle appara�t dans une autre page !",
        "Dans le monde des merveilles, on peut voir le four allum� m�me apr�s avoir grandi !",
    ];

    private const string DONATE_URL = "https://www.paypal.com/donate/?business=QB2DD2YWXZ79E&currency_code=EUR";
    private const string KH1_PATCH_VOICES_ZIP_NAME = "KH1FM-Voices.patch";
    private const string KH1_PATCH_VIDEOS_ZIP_NAME = "KH1FM-Videos.patch";
    private const string KH1_PATCH_TEXTURES_ZIP_NAME = "KH1FM-Textures.patch";
    private const string KH1_PATCH_MAGIC_ZIP_NAME = "KH1FM-Magic-{LANG}.patch";
    private const string KH1_PATCH_STRANGER_ZIP_NAME = "KH1FM-Stranger.patch";
    private const string KH1_PATCH_EXTRACTION_FOLDER_NAME = "KH1_PATCH";
    private const string KH1_OPENING_VIDEO_FILENAME = "OPN.mp4";
    private const string KH1_ENDING_VIDEO_FILENAME = "END.mp4";
    private const string DEFAULT_EPIC_GAMES_FOLDER = @"C:\Program Files\Epic Games\KH_1.5_2.5";
    private const string DEFAULT_STEAM_FOLDER = @"C:\Program Files (x86)\Steam\steamapps\common\KINGDOM HEARTS -HD 1.5+2.5 ReMIX-";
    private const string DEFAULT_STEAM_DECK_FOLDER = "/home/deck/.local/share/Steam/steamapps/Common/KINGDOM HEARTS -HD 1.5+2.5 ReMIX-";
    private const string SAVE_FOLDER_NAME = "Patch/Saves";
    private const string PATCHED_FILES_FOLDER_NAME = "Patch/Temp";

    private static readonly List<string> REQUIRED_FILE_NAMES = new List<string>
        {
            "kh1_first.pkg", "kh1_first.hed",
            "kh1_third.pkg", "kh1_third.hed",
            "kh1_fourth.pkg", "kh1_fourth.hed",
            "kh1_fifth.pkg", "kh1_fifth.hed",
        };

    #endregion

    #region Private fields

    private readonly Progress<List<object>> _progress;
    private float _patchState;
    private string _selectedGameFolder;
    private int _randomQuotesCounter;
    private bool _isSteamInstall;

    #endregion

    #region Properties

    public float PatchState
    {
        get => _patchState;
        set
        {
            _patchState = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PatchState)));
        }
    }

    public bool? ShouldPatchMagic
    {
        get => (bool?)GetValue(ShouldPatchMagicProperty);
        set => SetValue(ShouldPatchMagicProperty, value);
    }

    public bool? ShouldPatchTexture
    {
        get => (bool?)GetValue(ShouldPatchTextureProperty);
        set => SetValue(ShouldPatchTextureProperty, value);
    }

    public bool? ShouldSaveOriginalFiles
    {
        get => (bool?)GetValue(ShouldSaveOriginalFilesProperty);
        set => SetValue(ShouldSaveOriginalFilesProperty, value);
    }

    public static readonly AvaloniaProperty ShouldPatchMagicProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(ShouldPatchMagic), true);

    public static readonly AvaloniaProperty ShouldPatchTextureProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(ShouldPatchTexture), true);

    public static readonly AvaloniaProperty ShouldSaveOriginalFilesProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(ShouldSaveOriginalFiles), true);

    #endregion

    #region Constructor

    public MainWindow()
    {
        InitializeComponent();
        //DataContext = this;

        PatchMagicOption.IsCheckedChanged += OnPatchMagicOption_IsCheckedChanged;

        //DataContext = new MainViewModel
        //{
        //    ShouldPatchMagic = true,
        //    ShouldPatchTexture = true,
        //    ShouldSaveOriginalFiles = true,
        //    RandomQuote = "Example random quote."
        //};

        // TODO: Find a better way to do that
        /*
        // Determine if this program is executed from a build or from Visual Studio
        var assemblyPath = Assembly.GetEntryAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

        if (assemblyDirectory.EndsWith("net8.0-windows7.0"))
        {
            PROJECT_DIRECTORY = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        }
        */

        SearchGameFolderState();

        _progress = new Progress<List<object>>(value =>
        {
            var copiedSize = (long)value[0];
            var totalFileSize = (long)value[1];
            var filename = (string)value[2];

            PatchState = (float)((double)copiedSize / totalFileSize * 100);
        });

        // Check default installation folder for Epic Games
        if (CheckGameFolder(DEFAULT_EPIC_GAMES_FOLDER, false))
        {
            _selectedGameFolder = DEFAULT_EPIC_GAMES_FOLDER;
            ReadyToPatchState();
        }
        // Check default installation folder for Steam
        else if (CheckGameFolder(DEFAULT_STEAM_FOLDER, true))
        {
            _selectedGameFolder = DEFAULT_STEAM_FOLDER;
            ReadyToPatchState();
        }
        // Check default installation folder for the Steam Deck
        else if (CheckGameFolder(DEFAULT_STEAM_DECK_FOLDER, true))
        {
            _selectedGameFolder = DEFAULT_STEAM_DECK_FOLDER;
            ReadyToPatchState();
        }
        else
        {
            Debug.WriteLine("Default game folder not found.");
        }
    }

    private void OnPatchMagicOption_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
    }

    #endregion

    #region Random Quotes

    private void StartRandomQuotes()
    {
        UpdateRandomQuotes(null, null);

        DispatcherTimer timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };

        timer.Tick += UpdateRandomQuotes;
        timer.Start();
    }

    private void UpdateRandomQuotes(object sender, EventArgs e)
    {
        var random = new Random();

        if (_randomQuotesCounter < REQUIRED_RANDOM_QUOTES_COUNT)
        {
            RandomQuotes.Text = RANDOM_QUOTES[_randomQuotesCounter];
        }
        else
        {
            if (RANDOM_QUOTES.Count <= REQUIRED_RANDOM_QUOTES_COUNT)
            {
                RandomQuotes.IsVisible = false;
            }
            else
            {
                var randomQuoteIndex = random.Next(REQUIRED_RANDOM_QUOTES_COUNT, RANDOM_QUOTES.Count);
                RandomQuotes.Text = RANDOM_QUOTES[randomQuoteIndex];
                RANDOM_QUOTES.RemoveAt(randomQuoteIndex);
            }
        }

        _randomQuotesCounter++;
    }

    #endregion

    #region States

    private void SearchGameFolderState()
    {
        SetUIVisibility(gameNotFound: true, browse: true);
    }

    private void ReadyToPatchState()
    {
        // TODO(bth): Show a button to unpatch the game

        var shouldSaveOriginalFiles = ShouldSaveOriginalFiles.HasValue && ShouldSaveOriginalFiles.Value;

        if (!shouldSaveOriginalFiles || CheckRemainingSpace(_selectedGameFolder))
        {
            _isSteamInstall = true;

            SetUIVisibility(patch: true, gameFound: true, patchOptions: true, saveOriginalFiles: shouldSaveOriginalFiles);
            SetImageHeight(75);
        }
        else
        {
            GameNotFoundWarningMessage.Text = "Attention: ce patch s'assure de sauvegarder tous les fichiers originaux avant de les modifier afin que votre jeu ne soit pas cass� s'il y a un probl�me pendant le processus. " +
                "Mais ces fichiers sont gros, 4 Go en tout et il semblerait que vous n'ayez pas suffisament d'espace pour pouvoir effectuer cette sauvegarde correctement." +
                "Assurez-vous donc d'avoir suffisament d'espace libre avant de patcher votre jeu !";

            SetUIVisibility(gameNotFound: true, ignoreSave: true);
        }
    }

    private void PatchingState()
    {
        SetUIVisibility(patchProgress: true, patchProgressBar: true, randomQuotes: true);
        SetImageHeight(250);
        StartRandomQuotes();
    }

    private void FinishedState()
    {
        PatchProgressionMessage.Text = "Votre jeu a correctement �t� patch� ! Profitez bien des voix fran�aises ;)";
        PatchProgressionMessage.FontWeight = FontWeight.Bold;

        SetUIVisibility(patchProgress: true, credits: true);
        SetImageHeight(0);
    }

    #endregion

    private void SetImageHeight(double value)
    {
        var imageHeightRow = MainGrid.RowDefinitions[0];
        imageHeightRow.Height = new GridLength(value);
    }

    #region Buttons

    private async void BrowseFolderButtonClick(object sender, RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "S�lection du dossier d'installation de KINGDOM HEARTS -HD 1.5+2.5 ReMIX-"
        });

        if (result.Count == 1)
        {
            var selectedDirectoryPath = result[0].Path;
            Debug.WriteLine($"Selected: {selectedDirectoryPath.AbsolutePath}");

            /*
            // Check if install is a Steam install
            var isSteamInstall = Directory.Exists(Path.Combine(result, "STEAM"));

            if (CheckGameFolder(result, isSteamInstall))
            {
                _selectedGameFolder = result;
                _isSteamInstall = isSteamInstall;
                ReadyToPatchState();
            }
            else
            {
                GameNotFoundWarningMessage.Text =
                    "Le dossier d'installation de Kingdom Hearts HD 1.5 + 2.5 ReMIX que vous avez sp�cifi� n'est pas valide.\n" +
                    "Il doit s'agir du dossier dans lequel l'Epic Game Store ou Steam a t�l�charg� les fichiers du jeu.\n" +
                    "Le dossier que vous avez donn�: ";
                GameNotFoundWarningMessage.Inlines.Add(new TextBlock { Text = $"\"{result}\"", FontStyle = FontStyle.Italic });
            }
            */
        }
    }


    private void PatchGameButtonClick(object sender, RoutedEventArgs e)
    {
        PatchingState();
        _ = Patch(_selectedGameFolder);
    }

    private void IgnoreSaveButtonClick(object sender, RoutedEventArgs e)
    {
        ShouldSaveOriginalFiles = false;
        ReadyToPatchState();
    }

    #endregion

    #region Patch

    private async Task ExtractPatch(string patchName)
    {
        var patchFile = Path.Combine(PATCH_FOLDER, patchName);
        var extractionFolder = Path.Combine(PATCH_FOLDER, KH1_PATCH_EXTRACTION_FOLDER_NAME);

        if (!Directory.Exists(extractionFolder))
            Directory.CreateDirectory(extractionFolder);

        // The .patch file is not found, it could already be decompressed (if we are launching this program from VS for example)
        if (!File.Exists(patchFile))
        {
            // If we are in Visual Studio, we want to copy the patch files to the destination folder
            if (!string.IsNullOrEmpty(PROJECT_DIRECTORY))
            {
                var patchFolder = Path.Combine(PROJECT_DIRECTORY, PATCH_FOLDER_RELATIVE_PATH, Path.GetFileNameWithoutExtension(patchName));
                await CopyFolderContentToAsync(patchFolder, extractionFolder, _progress, default, 0x100000);
            }
        }
        else
        {
            using var zip = Ionic.Zip.ZipFile.Read(patchFile);
            // Make sure to extract patch files only if necessary
            var alreadyExtractedFiles = Directory.GetFiles(extractionFolder, "*.*", SearchOption.AllDirectories);
            var alreadyExtractedFolders = Directory.GetDirectories(extractionFolder, "*.*", SearchOption.AllDirectories);

            if (alreadyExtractedFiles.Length + alreadyExtractedFolders.Length != zip.Count)
            {
                PatchProgressionMessage.Text = $"Extraction des fichiers de {patchName}...";
                zip.ExtractProgress += ZipExtractProgress;
                await Task.Run(() => zip.ExtractAll(extractionFolder, ExtractExistingFileAction.OverwriteSilently));
            }
        }
    }

    private async Task Patch(string gameFolder)
    {
        try
        {
            var patchesExtractionFolder = Path.Combine(PATCH_FOLDER, KH1_PATCH_EXTRACTION_FOLDER_NAME);

            if (Directory.Exists(patchesExtractionFolder))
                Directory.Delete(patchesExtractionFolder, true);

            // Update videos if the corresponding patch is found
            await PatchVideos();

            // Extract VF patch files
            await ExtractPatch(KH1_PATCH_VOICES_ZIP_NAME);

            var shouldPatchMagic = ShouldPatchMagic.HasValue && ShouldPatchMagic.Value;
            var shouldPatchTexture = ShouldPatchTexture.HasValue && ShouldPatchTexture.Value;

            // Extract "Magic" to "Magie" fix patch
            if (shouldPatchMagic)
            {
                var magicPatchName = shouldPatchTexture
                    ? KH1_PATCH_MAGIC_ZIP_NAME.Replace("{LANG}", "FR")
                    : KH1_PATCH_MAGIC_ZIP_NAME.Replace("{LANG}", "EN");

                await ExtractPatch(magicPatchName);
            }

            if (shouldPatchTexture)
            {
                await ExtractPatch(KH1_PATCH_TEXTURES_ZIP_NAME);
            }

            await ExtractPatch(KH1_PATCH_STRANGER_ZIP_NAME);

            // Create temporary folder to store patched files before to copy them in the actual game folder
            var patchedFilesBaseFolder = Path.Combine(gameFolder, PATCHED_FILES_FOLDER_NAME);
            if (Directory.Exists(patchedFilesBaseFolder))
                Directory.Delete(patchedFilesBaseFolder, true);
            Directory.CreateDirectory(patchedFilesBaseFolder);

            foreach (var requiredFile in GetRequiredFiles(_isSteamInstall))
            {
                var pkgFile = Path.Combine(gameFolder, requiredFile);
                var patchFolder = Path.Combine(patchesExtractionFolder, Path.GetFileNameWithoutExtension(pkgFile));
                var patchedPKGFile = Path.Combine(patchedFilesBaseFolder, Path.GetFileName(pkgFile));

                if (Path.GetExtension(requiredFile) != ".pkg" || !Directory.Exists(patchFolder))
                    continue;

                // Make sure to execute this for HED files too
                await SaveOrRestore(gameFolder, Path.ChangeExtension(requiredFile, ".hed"));
                await SaveOrRestore(gameFolder, requiredFile);

                PatchProgressionMessage.Text = $"Modification de {Path.GetFileName(pkgFile)}...";

                Patcher.PatchProgress += PatchProgress;
                await Task.Run(() => Patcher.Patch(pkgFile, patchFolder, patchedFilesBaseFolder));

                // Copy patched PKG to the right location
                var progress = new Progress<List<object>>(value =>
                {
                    var copiedSize = (long)value[0];
                    var totalFileSize = (long)value[1];
                    var filename = (string)value[2];

                    PatchState = (float)((double)copiedSize / totalFileSize * 100);
                });

                var patchedHEDFile = Path.ChangeExtension(patchedPKGFile, ".hed");
                var originalHEDFile = Path.ChangeExtension(pkgFile, ".hed");

                PatchProgressionMessage.Text = $"Copie du fichier {Path.GetFileName(patchedHEDFile)} patch� dans le dossier du jeu...";

                await CopyToAsync(patchedHEDFile, originalHEDFile, progress);

                PatchProgressionMessage.Text = $"Copie du fichier {Path.GetFileName(patchedPKGFile)} patch� dans le dossier du jeu...";

                await CopyToAsync(patchedPKGFile, pkgFile, progress, default, 0x1000000);
            }

            Directory.Delete(patchedFilesBaseFolder, true);

            FinishedState();
        }
        catch (Exception e)
        {
            PatchProgressionMessage.Foreground = Brushes.Red;
            PatchProgressionMessage.Text = $"Une erreur s'est produite: {e.Message}";

            SetUIVisibility(patchProgress: true);

            Debug.WriteLine(e.Message);
        }
    }

    private async Task PatchVideos()
    {
        // If found, extract video patch
        if (File.Exists(Path.Combine(PATCH_FOLDER, KH1_PATCH_VIDEOS_ZIP_NAME)) || !string.IsNullOrEmpty(PROJECT_DIRECTORY))
        {
            await ExtractPatch(KH1_PATCH_VIDEOS_ZIP_NAME);

            var openingVideoFile = Path.Combine(PATCH_FOLDER, KH1_PATCH_EXTRACTION_FOLDER_NAME, KH1_OPENING_VIDEO_FILENAME);
            var endingVideoFile = Path.Combine(PATCH_FOLDER, KH1_PATCH_EXTRACTION_FOLDER_NAME, KH1_ENDING_VIDEO_FILENAME);
            var movieFolderPath = _isSteamInstall ? @"STEAM\dt\KH1Movie" : @"EPIC\en\KH1Movie";
            var gameVideosFolder = Path.Combine(_selectedGameFolder, movieFolderPath);
            var originalOpeningVideoFile = Path.Combine(gameVideosFolder, KH1_OPENING_VIDEO_FILENAME);
            var originalEndingVideoFile = Path.Combine(gameVideosFolder, KH1_ENDING_VIDEO_FILENAME);

            if (!Directory.Exists(gameVideosFolder))
            {
                throw new Exception($"Le dossier {gameVideosFolder} n'existe pas, il est pourtant n�cessaire pour patcher les vid�os du jeu.");
            }

            // Save original files
            await SaveOrRestore(_selectedGameFolder, originalOpeningVideoFile);
            await SaveOrRestore(_selectedGameFolder, originalEndingVideoFile);

            PatchProgressionMessage.Text = $"Copie du fichier {Path.GetFileName(openingVideoFile)} dans le dossier du jeu...";

            await CopyToAsync(openingVideoFile, originalOpeningVideoFile, _progress);

            PatchProgressionMessage.Text = $"Copie du fichier {Path.GetFileName(endingVideoFile)} dans le dossier du jeu...";

            await CopyToAsync(endingVideoFile, originalEndingVideoFile, _progress);
        }
    }

    private void PatchProgress(object sender, PatchProgressEventArgs e)
    {
        if (e.EntriesTotal > 0)
        {
            PatchState = 100f * e.EntriesPatched / e.EntriesTotal;
        }
    }

    #endregion

    #region Utils

    private List<string> GetRequiredFiles(bool isSteamInstall)
    {
        var pathname = isSteamInstall ? "dt/" : "en/";
        var filepaths = new List<string>();

        foreach (var filename in REQUIRED_FILE_NAMES)
        {
            filepaths.Add($"Image/{pathname}{filename}");
        }

        return filepaths;
    }

    private bool CheckGameFolder(string folder, bool isSteamInstall)
    {
        // Check PKG/HED files
        foreach (var requiredFile in GetRequiredFiles(isSteamInstall))
        {
            var completePath = Path.Combine(folder, requiredFile);

            if (!File.Exists(completePath))
            {
                return false;
            }
        }

        return true;
    }

    private bool CheckRemainingSpace(string folder)
    {
        // Required at least 4GB to save original files
        return GetFreeSpace(folder) > 4e+9;
    }

    private long GetFreeSpace(string folder)
    {
        var folderInfo = new FileInfo(folder);
        var drive = new DriveInfo(folderInfo.Directory.Root.FullName);

        // Required at least 4GB to save original files
        return drive.AvailableFreeSpace;
    }

    // fileToSaveOrRestore folder should be relative to game folder
    private async Task SaveOrRestore(string gameFolder, string fileToSaveOrRestore)
    {
        var shouldSaveOriginalFiles = ShouldSaveOriginalFiles.HasValue && ShouldSaveOriginalFiles.Value;

        if (!shouldSaveOriginalFiles)
        {
            return;
        }

        // Save the original files
        var saveFolder = Path.Combine(gameFolder, SAVE_FOLDER_NAME);
        var filename = Path.GetFileName(fileToSaveOrRestore);
        var savedFileCompletePath = Path.Combine(saveFolder, filename);
        var fileToSaveOrRestoreCompletePath = Path.Combine(gameFolder, fileToSaveOrRestore);

        string source;
        string destination;

        if (!File.Exists(savedFileCompletePath))
        {
            Directory.CreateDirectory(saveFolder);

            source = fileToSaveOrRestoreCompletePath;
            destination = savedFileCompletePath;

            PatchProgressionMessage.Text = $"Sauvegarde du fichier {filename} original...";
        }
        else
        {
            // Copy saved files in the original folder back, to make sure we patch the original files
            source = savedFileCompletePath;
            destination = fileToSaveOrRestoreCompletePath;

            PatchProgressionMessage.Text = $"Restauration de {filename}...";
        }

        await CopyToAsync(source, destination, _progress, default, 0x100000);
    }

    private void ZipExtractProgress(object sender, ExtractProgressEventArgs eventData)
    {
        if (eventData.EntriesTotal > 0)
        {
            PatchState = 100f * eventData.EntriesExtracted / eventData.EntriesTotal;
        }
    }

    public async Task CopyFolderContentToAsync(string sourceFolder, string destinationFolder, IProgress<List<object>> progress, CancellationToken cancellationToken = default, int bufferSize = 0x1000)
    {
        var source = new DirectoryInfo(sourceFolder);
        var destination = new DirectoryInfo(destinationFolder);

        PatchProgressionMessage.Text = $"Copie le contenu du dossier {source.Name} dans le dossier {destination.FullName}...";

        var files = source.GetFiles("*", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            string outputPath = files[i].DirectoryName.Replace(source.FullName, destination.FullName);
            Directory.CreateDirectory(outputPath);

            PatchProgressionMessage.Text = $"Copie du fichier {files[i].Name}...";
            await CopyToAsync(files[i].FullName, Path.Combine(outputPath, files[i].Name), progress, cancellationToken, bufferSize, false);
            progress.Report(new List<object> { (long)i, (long)files.Length, files[i].Name });
        }
    }

    public async Task CopyToAsync(string sourceFile, string destinationFile, IProgress<List<object>> progress, CancellationToken cancellationToken = default, int bufferSize = 0x1000, bool updateProgress = true)
    {
        using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[bufferSize];
        int bytesRead;
        long totalRead = 0;

        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalRead += bytesRead;

            if (updateProgress)
            {
                progress.Report(new List<object> { totalRead, sourceStream.Length, Path.GetFileName(sourceFile) });
            }
        }
    }

    private void ClickToOpenURL(object sender, PointerPressedEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.Text != null)
        {
            OpenURL(textBlock.Text);
        }

        e.Handled = true;
    }

    private void OnDonateClick(object sender, PointerPressedEventArgs e)
    {
        OpenURL(DONATE_URL);
        e.Handled = true;
    }

    private void OpenURL(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void SetUIVisibility(
        bool gameNotFound = false,
        bool browse = false,
        bool patch = false,
        bool gameFound = false,
        bool patchProgress = false,
        bool patchProgressBar = false,
        bool credits = false,
        bool patchOptions = false,
        bool randomQuotes = false,
        bool ignoreSave = false,
        bool saveOriginalFiles = false)
    {
        GameNotFoundWarningMessage.IsVisible = gameNotFound;
        BrowseButton.IsVisible = browse;
        PatchButton.IsVisible = patch;
        GameFoundMessage.IsVisible = gameFound;
        PatchProgressionMessage.IsVisible = patchProgress;
        PatchProgressBar.IsVisible = patchProgressBar;
        Credits.IsVisible = credits;
        PatchOptions.IsVisible = patchOptions;
        RandomQuotes.IsVisible = randomQuotes;
        IgnoreSaveButton.IsVisible = ignoreSave;
        SaveOriginalFilesCheckbox.IsVisible = saveOriginalFiles;
        SaveOriginalFilesDescription.IsVisible = saveOriginalFiles;
    }

    #endregion
}