using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace KHFM_VF_Patch
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly string PROJECT_DIRECTORY;
        private const string PATCH_FOLDER_RELATIVE_PATH = "Resources/Patches";
        private static readonly string PATCH_FOLDER = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), PATCH_FOLDER_RELATIVE_PATH);
        private const int REQUIRED_RANDOM_QUOTES_COUNT = 3;

        private static readonly List<string> RANDOM_QUOTES = new()
        {
            "Cela pourrait être un peu long, je vous conseille d'aller chercher un café...",
            "Sinon ça a été votre journée ?",
            "Vous êtes toujours là ? Alors attendez, je vous prépare de quoi vous occuper...",
            "Saviez vous que Donald Reignoux, l'excellent doubleur de Sora, avait également doublé Titeuf, Lúcio dans Overwatch mais aussi Connor dans Detroit: Become Human ?",
            "Richard Darbois est connu pour avoir fait des voix emblématiques, dont celle d'Indiana Jones ou de Buzz l'Éclair, mais saviez-vous qu'il a participé à ce Kingdom Hearts en doublant le génie et Oogie Boogie ?",
            "Kingdom Hearts est le premier jeu de la série, mais ce n'est pas par lui que commence véritablement l'histoire.",
            "Si comme moi vous n'avez jamais fait la version Final Mix du jeu, sachez qu'un boss plus compliqué que Sephiroth vous attend ! J'ai tellement hâte !",
            "Ce patch a nécessité des dizaines d'heures de travail et totalise des centaines de commits sur 3 repos différents !",
            "Un patch pour rétablir les voix françaises sur Kingdom Hearts 2 a été créé par TieuLink sur le site Nexus Mods !",
            "Une modification future de ce patch permettra peut-être de supprimer les sous-titres pour renforcer l'immersion.",
            "Saviez-vous que Mickey était censé être le protagoniste principal du jeu à la place de Sora ? Cela expliquerait pourquoi Donald et Dingo sont ses compagnons.",
            "Disney ne voulait pas que Mickey soit dans le jeu, ils ont finalement accepté, mais à la condition qu'il ne soit présent que dans une seule scène du jeu, Nomura à choisi de le mettre à la fin.",
            "Yoko Shimomura, la compositrice du jeu, a également composé les musiques de Street Fighter 2, Legend of Mana, Xenoblade Chronicles et Final Fantasy XV.",
            "La musique One-Winged Angel présente dans le jeu vient de Final Fantasy VII et a été composée par Nobuo Uematsu.",
            "Disney a été très furieux quand ils ont vu que Ariel pouvait se battre. Pour s'excuser, Square Enix a été forcé d'en faire un monde musical dans Kingdom Hearts 2...",
            "Dans la Forteresse Oublié il y a une entrée pour un ascenseur près du sommet mais... pas d'ascenseur !",
            "L'Île du Destin dans la fin des mondes s'appelle Île du Souvenir.",
            "Quand on détruit la maison de Bourriquet elle apparaît dans une autre page !",
            "Dans le monde des merveilles on peut voir le four allumé même après avoir grandit !",
        };

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

        private static readonly List<string> REQUIRED_FILE_NAMES = new()
        {
            "kh1_first.pkg", "kh1_first.hed",
            "kh1_third.pkg", "kh1_third.hed",
            "kh1_fourth.pkg", "kh1_fourth.hed",
            "kh1_fifth.pkg", "kh1_fifth.hed",
        };

        private readonly Progress<List<object>> _progress;
        public event PropertyChangedEventHandler PropertyChanged;

        private float _patchState;
        private string _selectedGameFolder;
        private int _randomQuotesCounter;
        private bool _isSteamInstall;

        public float PatchState
        {
            get => _patchState;
            set
            {
                _patchState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PatchState)));
            }
        }

        public bool ShouldPatchMagic
        {
            get => (bool)GetValue(ShouldPatchMagicProperty);
            set => SetValue(ShouldPatchMagicProperty, value);
        }

        public bool ShouldPatchTexture
        {
            get => (bool)GetValue(ShouldPatchTextureProperty);
            set => SetValue(ShouldPatchTextureProperty, value);
        }

        public bool ShouldSaveOriginalFiles
        {
            get => (bool)GetValue(ShouldSaveOriginalFilesProperty);
            set => SetValue(ShouldSaveOriginalFilesProperty, value);
        }

        public static readonly DependencyProperty ShouldPatchMagicProperty =
            DependencyProperty.Register(nameof(ShouldPatchMagic), typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));

        public static readonly DependencyProperty ShouldPatchTextureProperty =
            DependencyProperty.Register(nameof(ShouldPatchTexture), typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));

        public static readonly DependencyProperty ShouldSaveOriginalFilesProperty =
            DependencyProperty.Register(nameof(ShouldSaveOriginalFiles), typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));

        public MainWindow()
        {
            // Determine if this program is executed from a build or from Visual Studio
            var assemblyPath = Assembly.GetEntryAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

            if (assemblyDirectory.Equals("net8.0-windows7.0"))
            {
                PROJECT_DIRECTORY = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName;
            }

            InitializeComponent();
            DataContext = this;

            SearchGameFolderState();

            _progress = new Progress<List<object>>(value =>
            {
                var copiedSize = (long)value[0];
                var totalFileSize = (long)value[1];
                var filename = (string)value[2];

                PatchState = (float)((double)copiedSize / totalFileSize * 100);
            });

            if (CheckGameFolder(DEFAULT_EPIC_GAMES_FOLDER))
            {
                _selectedGameFolder = DEFAULT_EPIC_GAMES_FOLDER;
                ReadyToPatchState();
            }
            else if (CheckGameFolder(DEFAULT_STEAM_FOLDER))
            {
                _selectedGameFolder = DEFAULT_STEAM_FOLDER;
                ReadyToPatchState();
            }
            else if (CheckGameFolder(DEFAULT_STEAM_DECK_FOLDER))
            {
                _selectedGameFolder = DEFAULT_STEAM_DECK_FOLDER;
                ReadyToPatchState();
            }
            else
            {
                Debug.WriteLine("Default game folder not found.");
            }
        }

        private void SearchGameFolderState()
        {
            SetUIVisibility(gameNotFound: true, browse: true);
        }

        private void ReadyToPatchState()
        {
            // TODO(bth): Show a button to unpatch the game

            RandomQuotes.Visibility = Visibility.Collapsed;

            if (!ShouldSaveOriginalFiles || CheckRemainingSpace(_selectedGameFolder))
            {
                SetUIVisibility(patch: true, gameFound: true, patchOptions: true, saveOriginalFiles: ShouldSaveOriginalFiles);
                ImageHeight.Height = new GridLength(75);
            }
            else
            {
                GameNotFoundWarningMessage.Text = "Attention: ce patch s'assure de sauvegarder tous les fichiers originaux avant de les modifier afin que votre jeu ne soit pas cassé s'il y a un problème pendant le processus. " +
                    "Mais ces fichiers sont gros, 4 Go en tout et il semblerait que vous n'ayez pas suffisament d'espace pour pouvoir effectuer cette sauvegarde correctement." +
                    "Assurez-vous donc d'avoir suffisament d'espace libre avant de patcher votre jeu !";
                SetUIVisibility(gameNotFound: true, ignoreSave: true);
            }
        }

        private void PatchingState()
        {
            SetUIVisibility(patchProgress: true, randomQuotes: true);
            ImageHeight.Height = new GridLength(250);
            StartRandomQuotes();
        }

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
                    RandomQuotes.Visibility = Visibility.Collapsed;
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

        private void FinishedState()
        {
            GameNotFoundWarningMessage.Visibility = Visibility.Collapsed;
            PatchProgressionMessage.Text = "Votre jeu a correctement été patché ! Profitez bien des voix françaises ;)";
            PatchProgressionMessage.FontWeight = FontWeights.Bold;
            PatchProgressionMessage.Visibility = Visibility.Visible;
            PatchProgressBar.Visibility = Visibility.Collapsed;
            Credits.Visibility = Visibility.Visible;
            ImageHeight.Height = new GridLength(0);
            BrowseButton.Visibility = Visibility.Collapsed;
            PatchOptions.Visibility = Visibility.Collapsed;
            RandomQuotes.Visibility = Visibility.Collapsed;
        }

        private void BrowseFolderButtonClick(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Debug.WriteLine($"Selected: {dialog.SelectedPath}");

                if (CheckGameFolder(dialog.SelectedPath))
                {
                    _selectedGameFolder = dialog.SelectedPath;
                    ReadyToPatchState();
                }
                else
                {
                    GameNotFoundWarningMessage.Text =
                        "Le dossier d'installation de Kingdom Hearts HD 1.5 + 2.5 ReMIX que vous avez spécifié n'est pas valide.\n" +
                        "Il doit s'agir du dossier dans lequel l'Epic Game Store ou Steam a téléchargé les fichiers du jeu.\n" +
                        $"Le dossier que vous avez donné: ";
                    GameNotFoundWarningMessage.Inlines.Add(new Italic(new Run($"\"{dialog.SelectedPath}\"")));
                }
            }
        }

        private List<string> GetRequiredFiles()
        {
            var pathname = _isSteamInstall ? "dt/" : "en/";
            var filepaths = new List<string>();

            foreach (var filename in REQUIRED_FILE_NAMES)
            {
                filepaths.Add($"Image/{pathname}{filename}");
            }

            return filepaths;
        }

        private void IgnoreSaveButtonClick(object sender, RoutedEventArgs e)
        {
            ShouldSaveOriginalFiles = false;
            ReadyToPatchState();
        }

        private void PatchGameButtonClick(object sender, RoutedEventArgs e)
        {
            PatchingState();
            _ = Patch(_selectedGameFolder);
        }

        private bool CheckGameFolder(string folder)
        {
            // Check if install is a Steam install
            _isSteamInstall = Directory.Exists(Path.Combine(folder, "STEAM"));

            // Check PKG/HED files
            foreach (var requiredFile in GetRequiredFiles())
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
            DriveInfo drive = new DriveInfo(folderInfo.Directory.Root.FullName);

            // Required at least 4GB to save original files
            return drive.AvailableFreeSpace;
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

                foreach (var requiredFile in GetRequiredFiles())
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

                    PatchProgressionMessage.Text = $"Copie du fichier {Path.GetFileName(patchedHEDFile)} patché dans le dossier du jeu...";

                    await CopyToAsync(patchedHEDFile, originalHEDFile, progress);

                    PatchProgressionMessage.Text = $"Copie du fichier {Path.GetFileName(patchedPKGFile)} patché dans le dossier du jeu...";

                    await CopyToAsync(patchedPKGFile, pkgFile, progress, default, 0x1000000);
                }

                Directory.Delete(patchedFilesBaseFolder, true);

                FinishedState();
            }
            catch (Exception e)
            {
                PatchProgressionMessage.Foreground = Brushes.Red;
                PatchProgressionMessage.Text = $"Une erreur s'est produite: {e.Message}";
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
                    throw new Exception($"Le dossier {gameVideosFolder} n'existe pas, il est pourtant nécessaire pour patcher les vidéos du jeu.");
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

        // fileToSaveOrRestore folder should be relative to game folder
        private async Task SaveOrRestore(string gameFolder, string fileToSaveOrRestore)
        {
            if (!ShouldSaveOriginalFiles)
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
                using var zip = ZipFile.Read(patchFile);
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            OpenURL(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void OnDonateClick(object sender, MouseButtonEventArgs e)
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
            bool credits = false, 
            bool patchOptions = false, 
            bool randomQuotes = false,
            bool ignoreSave = false, 
            bool saveOriginalFiles = false)
        {
            GameNotFoundWarningMessage.Visibility = gameNotFound ? Visibility.Visible : Visibility.Collapsed;
            BrowseButton.Visibility = browse ? Visibility.Visible : Visibility.Collapsed;
            PatchButton.Visibility = patch ? Visibility.Visible : Visibility.Collapsed;
            GameFoundMessage.Visibility = gameFound ? Visibility.Visible : Visibility.Collapsed;
            PatchProgressionMessage.Visibility = patchProgress ? Visibility.Visible : Visibility.Collapsed;
            PatchProgressBar.Visibility = patchProgress ? Visibility.Visible : Visibility.Collapsed;
            Credits.Visibility = credits ? Visibility.Visible : Visibility.Collapsed;
            PatchOptions.Visibility = patchOptions ? Visibility.Visible : Visibility.Collapsed;
            RandomQuotes.Visibility = randomQuotes ? Visibility.Visible : Visibility.Collapsed;
            IgnoreSaveButton.Visibility = ignoreSave ? Visibility.Visible : Visibility.Collapsed;
            SaveOriginalFilesCheckbox.Visibility = saveOriginalFiles ? Visibility.Visible : Visibility.Collapsed;
            SaveOriginalFilesDescription.Visibility = saveOriginalFiles ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
