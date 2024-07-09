using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Ionic.Zip;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace KHFM_VF_Patch;

public partial class MainWindow : Window
{
    #region Constants

    private readonly string? PROJECT_DIRECTORY;
    private const string PATCH_FOLDER_RELATIVE_PATH = "Resources/Patches";
    private static readonly string PATCH_FOLDER = Path.Combine(AppContext.BaseDirectory, PATCH_FOLDER_RELATIVE_PATH);

    private static readonly List<string> RANDOM_QUOTES =
    [
        "\"Kingdom Hearts\" est le premier jeu de la s�rie, mais ce n'est pas par lui que commence v�ritablement l'histoire.",
        "L'id�e originale de \"Kingdom Hearts\" est n�e d'une rencontre fortuite entre Shinji Hashimoto, producteur chez Square Enix, et un cadre de Disney dans un ascenseur. Les deux entreprises partageaient le m�me immeuble � Tokyo, ce qui a facilit� cette collaboration improbable entre Square et Disney.",
        "Pendant le d�veloppement, le jeu �tait connu sous le nom de code \"Project X\". Cela a permis de maintenir un certain myst�re autour du projet et de prot�ger l'enthousiasme jusqu'� ce que les d�tails officiels soient r�v�l�s.",
        "\"Kingdom Hearts\" int�gre des personnages de la s�rie \"Final Fantasy\". Tetsuya Nomura, qui avait travaill� sur la s�rie \"Final Fantasy\", a inclus des personnages comme Cloud Strife et Sephiroth pour attirer les fans de \"Final Fantasy\" et cr�er une exp�rience de jeu encore plus riche.",
        "Le d�veloppement du jeu a �t� particuli�rement difficile en raison de la diff�rence de culture d'entreprise entre Square Enix et Disney. Les �quipes ont d� surmonter de nombreux d�fis pour aligner leurs visions cr�atives et techniques.",
        "Le design initial de Sora le pr�sentait comme un personnage mi-humain, mi-lion, portant une tron�onneuse � la place de la Keyblade. Cependant, ce concept a �t� jug� trop radical et a �t� modifi� pour cr�er l'apparence actuelle de Sora et sa c�l�bre Keyblade.",
        "Yoko Shimomura, la compositrice de la musique du jeu, a cr�� une bande-son qui est devenue embl�matique pour la s�rie. Sa musique a jou� un r�le crucial dans la cr�ation de l'atmosph�re magique et �motionnelle du jeu. Elle a �galement a �galement compos� les musiques de jeux cultissimes comme Street Fighter 2, Super Mario RPG, Legend of Mana, Xenoblade Chronicles ou bien encore Final Fantasy XV.",
        "La chanson th�me \"Hikari\" (ou \"Simple and Clean\" en anglais) interpr�t�e par Utada Hikaru est devenue embl�matique pour la s�rie.",
        "Obtenir les autorisations n�cessaires pour utiliser les personnages Disney a �t� un processus complexe. Chaque personnage devait �tre approuv� individuellement par Disney, ce qui a ajout� une couche suppl�mentaire de d�fis administratifs pour les d�veloppeurs.",
        "Les s�quences cin�matiques de \"Kingdom Hearts\" ont �t� r�volutionnaires pour l'�poque, utilisant des graphismes avanc�s pour cr�er des sc�nes �motionnellement engageantes. Ces cin�matiques ont contribu� � la popularit� et au succ�s critique du jeu.",
        "Trouver le bon �quilibre entre les �l�ments de gameplay et les �l�ments narratifs a �t� un d�fi constant. Les d�veloppeurs voulaient s'assurer que le jeu soit � la fois amusant � jouer et fid�le aux histoires et personnages des univers Disney et Square Enix.",
        "Mickey n�appara�t qu'� la toute fin, dans une sc�ne tr�s courte. Il n'a �t� montr� que sous forme d'ombre jusqu'� la toute fin, car Disney voulait s'assurer que l'utilisation de leur personnage principal soit parfaitement contr�l�e.",
        "Au d�part, Disney souhaitait que Mickey Mouse soit le personnage principal du jeu. Cependant, Tetsuya Nomura, le r�alisateur, a r�ussi � convaincre Disney de cr�er un personnage original, Sora, pour mieux correspondre � l'univers qu'ils souhaitaient cr�er. Mickey a finalement �t� inclus, mais seulement dans un r�le limit�.",
        "Un monde bas� sur \"Le Livre de la Jungle\" de Disney devait � l'origine �tre inclus dans le jeu. Cependant, il a �t� coup� avant la sortie du jeu. Des restes de ce monde peuvent encore �tre trouv�s dans les fichiers du jeu.",
        "� l'origine, il y avait beaucoup de scepticisme quant � savoir si la combinaison des univers Disney et Final Fantasy fonctionnerait. Cependant, le jeu a �t� largement acclam� par la critique et est devenu un �norme succ�s commercial, ouvrant la voie � de nombreuses suites et spin-offs.",
        "Saviez-vous que Donald Reignoux, l'excellent doubleur de Sora, a �galement doubl� Titeuf, L�cio dans Overwatch, mais aussi Connor dans Detroit: Become Human ?",
        "Richard Darbois est connu pour avoir fait des voix embl�matiques, dont celle d'Indiana Jones ou de Buzz l'�clair, mais saviez-vous qu'il a particip� � ce Kingdom Hearts en doublant le G�nie et Oogie Boogie ?",
        "Si vous n'avez jamais fait la version Final Mix du jeu, sachez qu'un boss plus compliqu� que Sephiroth vous attend !",
        "La musique One-Winged Angel pr�sente dans le jeu vient de Final Fantasy VII et a �t� compos�e par Nobuo Uematsu.",
        "Disney �tait tr�s furieux quand ils ont vu qu'Ariel pouvait se battre. Pour s'excuser, Square Enix a �t� forc� d'en faire un monde musical dans Kingdom Hearts 2...",
        "Dans la Forteresse Oubli�e, il y a une entr�e pour un ascenseur pr�s du sommet mais... pas d'ascenseur !",
        "L'�le du Destin dans la fin des mondes s'appelle �le du Souvenir.",
        "Quand on d�truit la maison de Bourriquet, elle appara�t dans une autre page !",
        "Dans le monde des merveilles, on peut voir le four allum� m�me apr�s avoir grandi !",
        "Ce patch a n�cessit� plusieurs dizaines d'heures de travail et totalise des centaines de commits sur trois repos diff�rents !",
        "Un patch pour r�tablir les voix fran�aises sur Kingdom Hearts 2 a �t� cr�� par TieuLink sur le site Nexus Mods !",
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

    private static readonly List<string> REQUIRED_FILE_NAMES =
    [
        "kh1_first.pkg", "kh1_first.hed",
        "kh1_third.pkg", "kh1_third.hed",
        "kh1_fourth.pkg", "kh1_fourth.hed",
        "kh1_fifth.pkg", "kh1_fifth.hed",
    ];

    #endregion

    #region Private fields

    private readonly Progress<List<object>> _progress;
    private string? _selectedGameFolder;
    private int _randomQuotesCounter;
    private bool _isSteamInstall;
    private bool _shouldPatchMagic;
    private bool _shouldPatchTexture;
    private bool _shouldPatchVideos;
    private bool _shouldSaveOriginalFiles;
    private Random _random = new Random();


    #endregion

    #region Properties

    public bool ShouldPatchMagic
    {
        get { return _shouldPatchMagic; }
        set
        {
            if (_shouldPatchMagic != value)
            {
                _shouldPatchMagic = value;
                PatchMagicOption.IsChecked = value;
            }
        }
    }

    public bool ShouldPatchTexture
    {
        get { return _shouldPatchTexture; }
        set
        {
            if (_shouldPatchTexture != value)
            {
                _shouldPatchTexture = value;
                PatchTextureOption.IsChecked = value;
            }
        }
    }

    public bool ShouldPatchVideos
    {
        get { return _shouldPatchVideos; }
        set
        {
            if (_shouldPatchVideos != value)
            {
                _shouldPatchVideos = value;
                PatchVideosOption.IsChecked = value;
            }
        }
    }

    public bool ShouldSaveOriginalFiles
    {
        get { return _shouldSaveOriginalFiles; }
        set
        {
            if (_shouldSaveOriginalFiles != value)
            {
                _shouldSaveOriginalFiles = value;
                SaveOriginalFilesOption.IsChecked = value;
            }
        }
    }

    #endregion

    #region Constructor

    public MainWindow()
    {
        InitializeComponent();

        // Enable all options by default
        ShouldPatchVideos = true;
        ShouldPatchMagic = true;
        ShouldPatchTexture = true;
        ShouldSaveOriginalFiles = true;

        // Don't use data binding as I don't want to use MVVM pattern for a such simple app
        PatchVideosOption.IsCheckedChanged += PatchVideosOption_IsCheckedChanged;
        PatchMagicOption.IsCheckedChanged += OnPatchMagicOption_IsCheckedChanged;
        PatchTextureOption.IsCheckedChanged += PatchTextureOption_IsCheckedChanged;
        SaveOriginalFilesOption.IsCheckedChanged += SaveOriginalFilesOption_IsCheckedChanged;

        // Determine if this program is executed from a build or from Visual Studio
        var isUsingVisualStudio = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSAPPIDDIR"));

        if (isUsingVisualStudio)
        {
            PROJECT_DIRECTORY = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent?.FullName;
        }

        SearchGameFolderState();

        _progress = new Progress<List<object>>(value =>
        {
            var copiedSize = (long)value[0];
            var totalFileSize = (long)value[1];
            var filename = (string)value[2];

            SetPatchProgressUI((float)((double)copiedSize / totalFileSize * 100));
        });

        // Check default installation folder for Epic Games
        if (CheckGameFolder(DEFAULT_EPIC_GAMES_FOLDER, false))
        {
            _selectedGameFolder = DEFAULT_EPIC_GAMES_FOLDER;
        }
        // Check default installation folder for Steam
        else if (CheckGameFolder(DEFAULT_STEAM_FOLDER, true))
        {
            _isSteamInstall = true;
            _selectedGameFolder = DEFAULT_STEAM_FOLDER;
        }
        // Check default installation folder for the Steam Deck
        else if (CheckGameFolder(DEFAULT_STEAM_DECK_FOLDER, true))
        {
            _isSteamInstall = true;
            _selectedGameFolder = DEFAULT_STEAM_DECK_FOLDER;
        }

        if (!string.IsNullOrEmpty(_selectedGameFolder))
        {
            ReadyToPatchState();
        }
        else
        {
            Debug.WriteLine("Default game folder not found.");
        }
    }

    #region Checkbox callbacks

    // These should not be needed if we can use data binding...

    private void PatchVideosOption_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox)
        {
            _shouldPatchVideos = checkbox.IsChecked.HasValue && checkbox.IsChecked.Value;
        }
    }

    private void OnPatchMagicOption_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox)
        {
            _shouldPatchMagic = checkbox.IsChecked.HasValue && checkbox.IsChecked.Value;
        }
    }

    private void PatchTextureOption_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox)
        {
            _shouldPatchTexture = checkbox.IsChecked.HasValue && checkbox.IsChecked.Value;
        }
    }

    private void SaveOriginalFilesOption_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox)
        {
            _shouldSaveOriginalFiles = checkbox.IsChecked.HasValue && checkbox.IsChecked.Value;
        }
    }

    #endregion

    #endregion

    #region Random Quotes

    private void StartRandomQuotes()
    {
        UpdateRandomQuotes(null, null);

        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(10)
        };

        timer.Tick += UpdateRandomQuotes;
        timer.Start();
    }

    private void UpdateRandomQuotes(object? sender, EventArgs? e)
    {
        var randomQuoteIndex = _random.Next(0, RANDOM_QUOTES.Count);
        RandomQuotes.Text = RANDOM_QUOTES[randomQuoteIndex];
        RANDOM_QUOTES.RemoveAt(randomQuoteIndex);

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

        if (!ShouldSaveOriginalFiles || CheckRemainingSpace(_selectedGameFolder))
        {
            SetUIVisibility(patch: true, gameFound: true, patchOptions: true, saveOriginalFiles: ShouldSaveOriginalFiles);
            SetImageHeight(0);
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
            var path = result[0].Path.LocalPath;

            // Check if install is a Steam install
            var steamFolder = Path.Combine(path, "STEAM");
            var isSteamInstall = Directory.Exists(steamFolder);

            if (CheckGameFolder(path, isSteamInstall))
            {
                _selectedGameFolder = path;
                _isSteamInstall = isSteamInstall;
                ReadyToPatchState();
            }
            else
            {
                GameNotFoundWarningMessage.Text =
                    "Le dossier d'installation de Kingdom Hearts HD 1.5 + 2.5 ReMIX que vous avez sp�cifi� n'est pas valide.\n" +
                    "Il doit s'agir du dossier dans lequel l'Epic Game Store ou Steam a t�l�charg� les fichiers du jeu.\n" +
                    "Le dossier que vous avez donn�: ";
                GameNotFoundWarningMessage.Inlines?.Add(new TextBlock { Text = path });
            }
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

    private async Task Patch(string? gameFolder)
    {
        if (gameFolder == null)
        {
            Console.Error.WriteLine("No game folder...");
            return;
        }

        try
        {
            var patchesExtractionFolder = Path.Combine(PATCH_FOLDER, KH1_PATCH_EXTRACTION_FOLDER_NAME);

            if (Directory.Exists(patchesExtractionFolder))
                Directory.Delete(patchesExtractionFolder, true);

            // Update videos if the corresponding patch is found
            if (ShouldPatchVideos)
            {
                await PatchVideos();
            }

            // Extract VF patch files
            await ExtractPatch(KH1_PATCH_VOICES_ZIP_NAME);

            // Extract "Magic" to "Magie" fix patch
            if (ShouldPatchMagic)
            {
                var magicPatchName = ShouldPatchTexture
                    ? KH1_PATCH_MAGIC_ZIP_NAME.Replace("{LANG}", "FR")
                    : KH1_PATCH_MAGIC_ZIP_NAME.Replace("{LANG}", "EN");

                await ExtractPatch(magicPatchName);
            }

            if (ShouldPatchTexture)
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

                    SetPatchProgressUI((float)((double)copiedSize / totalFileSize * 100));
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
        if (_selectedGameFolder == null)
        {
            Console.Error.WriteLine("No selected game folder...");
            return;
        }

        // If found, extract video patch
        if (File.Exists(Path.Combine(PATCH_FOLDER, KH1_PATCH_VIDEOS_ZIP_NAME)) || !string.IsNullOrEmpty(PROJECT_DIRECTORY))
        {
            await ExtractPatch(KH1_PATCH_VIDEOS_ZIP_NAME);

            var openingVideoFile = Path.Combine(PATCH_FOLDER, KH1_PATCH_EXTRACTION_FOLDER_NAME, KH1_OPENING_VIDEO_FILENAME);
            var endingVideoFile = Path.Combine(PATCH_FOLDER, KH1_PATCH_EXTRACTION_FOLDER_NAME, KH1_ENDING_VIDEO_FILENAME);
            var movieFolderPath = _isSteamInstall ? @"STEAM/dt/KH1Movie" : @"EPIC/en/KH1Movie";
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

    private void PatchProgress(object? sender, PatchProgressEventArgs e)
    {
        if (e.EntriesTotal > 0)
        {
            SetPatchProgressUI(100f * e.EntriesPatched / e.EntriesTotal);
        }
    }

    private void SetPatchProgressUI(float progress)
    {
        // Make sure the change is made on the UI thread
        Dispatcher.UIThread.InvokeAsync(() => PatchProgressBar.Value = progress);
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

    public static string? GetMountPoint(string path)
    {
        var fullPath = Path.GetFullPath(path);
        string? mountPoint = fullPath;

        while (!Directory.Exists(mountPoint))
        {
            mountPoint = Path.GetDirectoryName(mountPoint);

            if (string.IsNullOrEmpty(mountPoint))
            {
                throw new InvalidOperationException("Could not find the mount point.");
            }
        }

        if (File.Exists("/proc/mounts"))
        {
            var lines = File.ReadAllLines("/proc/mounts");
            var mountPoints = lines.Select(line => line.Split(' ')[1]).ToList();

            while (!mountPoints.Contains(mountPoint) && mountPoint != "/")
            {
                mountPoint = Path.GetDirectoryName(mountPoint);

                if (string.IsNullOrEmpty(mountPoint))
                {
                    throw new InvalidOperationException("Could not find the mount point.");
                }
            }
        }

        return mountPoint;
    }

    private static bool CheckRemainingSpace(string? folder)
    {
        if (folder == null)
        {
            return false;
        }

        // Required at least 4GB to save original files
        return GetFreeSpace(folder) > 4e+9;
    }

    private static long GetFreeSpace(string folder)
    {
        var mountPoint = GetMountPoint(folder);

        if (mountPoint == null)
        {
            return 0L;
        }

        var drive = new DriveInfo(mountPoint);

        // Required at least 4GB to save original files
        return drive.AvailableFreeSpace;
    }

    // fileToSaveOrRestore folder should be relative to game folder
    private async Task SaveOrRestore(string gameFolder, string fileToSaveOrRestore)
    {
        if (!_shouldSaveOriginalFiles)
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
            SetPatchProgressUI(100f * eventData.EntriesExtracted / eventData.EntriesTotal);
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
            // Ensure files[i] is not null
            if (files[i] == null)
            {
                continue;
            }

            string directoryName = files[i].DirectoryName ?? throw new InvalidOperationException("Directory name cannot be null.");
            string outputPath = directoryName.Replace(source.FullName, destination.FullName);

            // Ensure the output directory exists
            Directory.CreateDirectory(outputPath);

            // Ensure PatchProgressionMessage is properly declared and accessible
            PatchProgressionMessage.Text = $"Copie du fichier {files[i].Name}...";

            // Await the asynchronous copy operation
            await CopyToAsync(
                files[i].FullName,
                Path.Combine(outputPath, files[i].Name),
                progress,
                cancellationToken,
                bufferSize,
                false
            );

            // Report progress
            progress.Report(new List<object> { (long)i, (long)files.Length, files[i].Name });
        }
    }

    public static async Task CopyToAsync(string sourceFile, string destinationFile, IProgress<List<object>> progress, CancellationToken cancellationToken = default, int bufferSize = 0x1000, bool updateProgress = true)
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
        if (sender is TextBlock textBlock && textBlock.Tag != null)
        {
            var url = textBlock.Tag.ToString();
            if (!string.IsNullOrEmpty(url))
            {
                OpenURL(url);
            }
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