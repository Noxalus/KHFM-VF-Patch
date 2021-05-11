﻿using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace KHFM_VF_Patch
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly string TOOLS_PATH = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "tools");
        private static readonly string PATCH_FOLDER = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "patch");

        private const string KH1_PATCH_ZIP_NAME = "KH1FM-VF.patch";
        private const string DEFAULT_GAME_FOLDER = @"C:\Program Files\Epic Games\KH_1.5_2.5";
        private const string SAVE_FOLDER_NAME = "VFPatch/Saves";
        private const string PATCHED_FILES_FOLDER_NAME = "VFPatch/Patch";
        private static readonly List<string> REQUIRED_FILES = new List<string>()
        {
            "Image/en/kh1_first.pkg", "Image/en/kh1_first.hed",
            "Image/en/kh1_third.pkg", "Image/en/kh1_third.hed",
            "Image/en/kh1_fourth.pkg", "Image/en/kh1_fourth.hed",
            "Image/en/kh1_fifth.pkg", "Image/en/kh1_fifth.hed",
        };

        public event PropertyChangedEventHandler PropertyChanged;
        private float _patchState = 0f;
        private string _selectedGameFolder;

        public float PatchState
        {
            get { return _patchState; }
            set
            {
                _patchState = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("PatchState"));
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            SearchGameFolderState();

            if (CheckGameFolder(DEFAULT_GAME_FOLDER))
            {
                _selectedGameFolder = DEFAULT_GAME_FOLDER;
                ReadyToPatchState();
            }
            else
            {
                Debug.WriteLine("Default game folder not found.");
                GameNotFoundWarningMessage.Visibility = Visibility.Collapsed;
                //GameNotFoundWarningMessage.Text =
                //    "Le dossier d'installation de Kingdom Hearts HD 1.5 + 2.5 ReMIX n'a pas été trouvé automatiquement. " +
                //    "Cliquez sur le bouton ci-dessous pour indiquer où vous avez installé le jeu sur votre machine.";
            }
        }

        private void SearchGameFolderState()
        {
            GameNotFoundWarningMessage.Visibility = Visibility.Visible;
            BrowseButton.Visibility = Visibility.Visible;
            PatchButton.Visibility = Visibility.Collapsed;
            GameFoundMessage.Visibility = Visibility.Collapsed;
            PatchProgressionMessage.Visibility = Visibility.Collapsed;
            PatchProgressBar.Visibility = Visibility.Collapsed;
        }

        private void ReadyToPatchState()
        {
            // TODO(bth): Show a button to unpatch the game

            if (CheckRemainingSpace(_selectedGameFolder))
            {
                GameNotFoundWarningMessage.Visibility = Visibility.Collapsed;
                BrowseButton.Visibility = Visibility.Collapsed;
                PatchButton.Visibility = Visibility.Visible;
                GameFoundMessage.Visibility = Visibility.Visible;
            }
            else
            {
                var freeSpace = GetFreeSpace(_selectedGameFolder);
                GameNotFoundWarningMessage.Visibility = Visibility.Visible;
                GameNotFoundWarningMessage.Text = "Attention: ce patch s'assure de sauvegarder tous les fichiers originaux avant de les modifier afin que votre jeu ne soit pas cassé s'il y a un problème pendant le processus. " +
                    "Mais ces fichiers sont gros, 4 Go en tout et il semblerait que vous n'ayez pas suffisament d'espace pour pouvoir effectuer cette sauvegarde correctement." +
                    "Assurez-vous donc d'avoir suffisament d'espace libre avant de patcher votre jeu !";
            }
        }

        private void PatchingState()
        {
            GameFoundMessage.Visibility = Visibility.Collapsed;
            PatchProgressionMessage.Visibility = Visibility.Visible;
            PatchProgressBar.Visibility = Visibility.Visible;
            PatchButton.Visibility = Visibility.Collapsed;
        }

        private void BrowsFolderButtonClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Debug.WriteLine($"Selected: {dialog.SelectedPath}");

                    if (CheckGameFolder(dialog.SelectedPath))
                    {
                        _selectedGameFolder = dialog.SelectedPath;
                        ReadyToPatchState();
                    }
                    else
                    {
                        GameNotFoundWarningMessage.Visibility = Visibility.Visible;

                        GameNotFoundWarningMessage.Text =
                            "Le dossier d'installation de Kingdom Hearts HD 1.5 + 2.5 ReMIX que vous avez spécifié n'est pas valide.\n" +
                            "Il doit s'agir du dossier dans lequel l'Epic Game Store a téléchargé les fichiers du jeu, son nom est \"KH_1.5_2.5\".\n" +
                            $"Le dossier que vous avez donné: ";
                        GameNotFoundWarningMessage.Inlines.Add(new Italic(new Run($"\"{dialog.SelectedPath}\"")));

                        Debug.WriteLine("The selected folder is not valid!");
                    }
                }
            }
        }

        private void PatchGameButtonClick(object sender, RoutedEventArgs e)
        {
            PatchingState();
            _ = Patch(_selectedGameFolder);
        }

        private bool CheckGameFolder(string folder)
        {
            var directoryName = Path.GetFileName(folder);

            // Check directory name
            if (directoryName != "KH_1.5_2.5")
            {
                return false;
            }

            // Check PKG/HED files
            foreach (var requiredFile in REQUIRED_FILES)
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
            Debug.WriteLine("Patch the game!!!");

            try
            {
                await ExtractPatch();
                var patchedFilesBaseFolder = Path.Combine(gameFolder, PATCHED_FILES_FOLDER_NAME);

                if (Directory.Exists(patchedFilesBaseFolder))
                    Directory.Delete(patchedFilesBaseFolder, true);

                Directory.CreateDirectory(patchedFilesBaseFolder);

                foreach (var requiredFile in REQUIRED_FILES)
                {
                    await SaveOrRestore(gameFolder, requiredFile);

                    if (Path.GetExtension(requiredFile) != ".pkg")
                        continue;

                    var pkgFile = Path.Combine(gameFolder, requiredFile);
                    var patchFolder = Path.Combine(PATCH_FOLDER, Path.GetFileNameWithoutExtension(KH1_PATCH_ZIP_NAME), Path.GetFileNameWithoutExtension(pkgFile));
                    var patchedPKGFile = Path.Combine(patchedFilesBaseFolder, Path.GetFileName(pkgFile));

                    PatchProgressionMessage.Text = $"Modification de {pkgFile}...";

                    Patcher.PatchProgress += PatchProgress;
                    await Task.Run(() => Patcher.Patch(pkgFile, patchFolder, patchedFilesBaseFolder));

                    // Copy patched PKG to the right location
                    var progress = new Progress<List<object>>(value =>
                    {
                        var copiedSize = (long)value[0];
                        var totalFileSize = (long)value[1];
                        var filename = (string)value[2];

                        var progress = ((double)copiedSize / totalFileSize) * 100;

                        PatchState = (float)progress;
                        Debug.WriteLine("{1} {0:N2}%", progress, filename);
                    });

                    PatchProgressionMessage.Text = $"Copie du PKG patché ({Path.GetFileName(pkgFile)}) dans le dossier du jeu...";

                    await CopyToAsync(patchedPKGFile, pkgFile, progress, default, 0x1000000);
                    await CopyToAsync(Path.ChangeExtension(patchedPKGFile, ".hed"), Path.ChangeExtension(pkgFile, ".hed"), progress, default, 0x1000000);
                }

                PatchProgressionMessage.Text = $"Votre jeu a correctement été patché ! Bonne partie ;)";
            }
            catch (Exception e)
            {
                PatchProgressionMessage.Foreground = Brushes.Red;
                PatchProgressionMessage.Text = $"Une erreur s'est produite: {e.Message}";

                Debug.WriteLine(e.Message);
            }

        }

        private void PatchProgress(object sender, PatchProgressEventArgs e)
        {
            if (e.EntriesTotal > 0)
            {
                PatchState = Convert.ToInt32(100 * ((float)e.EntriesPatched / e.EntriesTotal));
            }
        }

        // fileToSaveOrRestore folder should be relative to game folder
        private async Task SaveOrRestore(string gameFolder, string fileToSaveOrRestore)
        {
            // Save the original files
            var saveFolder = Path.Combine(gameFolder, SAVE_FOLDER_NAME);
            var savedFileCompletePath = Path.Combine(saveFolder, fileToSaveOrRestore);

            var progress = new Progress<List<object>>(value =>
            {
                var copiedSize = (long)value[0];
                var totalFileSize = (long)value[1];
                var filename = (string)value[2];

                var progress = ((double)copiedSize / totalFileSize) * 100;

                PatchState = (float)progress;
                Debug.WriteLine("{1} {0:N2}%", progress, filename);
            });

            string source;
            string destination;
            var filename = Path.GetFileName(fileToSaveOrRestore);

            if (!File.Exists(savedFileCompletePath))
            {
                Directory.CreateDirectory(saveFolder);

                source = Path.Combine(gameFolder, fileToSaveOrRestore);
                destination = Path.Combine(saveFolder, filename);

                PatchProgressionMessage.Text = $"Sauvegarde du fichier {filename} original...";

                await CopyToAsync(source, destination, progress, default, 0x100000);
            }
            else
            {
                // Copy saved files in the original folder back, to make sure we patch the original files
                source = Path.Combine(saveFolder, filename);
                destination = Path.Combine(gameFolder, fileToSaveOrRestore);

                PatchProgressionMessage.Text = $"Restauration de {destination}";

            }

            await CopyToAsync(source, destination, progress, default, 0x100000);
        }

        private async Task ExtractPatch()
        {
            var patchFile = Path.Combine(PATCH_FOLDER, KH1_PATCH_ZIP_NAME);
            var extractionFolder = Path.Combine(PATCH_FOLDER, Path.GetFileNameWithoutExtension(KH1_PATCH_ZIP_NAME));

            if (!Directory.Exists(extractionFolder))
                Directory.CreateDirectory(extractionFolder);

            using (ZipFile zip = ZipFile.Read(patchFile))
            {
                // Make sure to extract patch files only if necessary
                var alreadyExtractedFiles = Directory.GetFiles(extractionFolder, "*.*", SearchOption.AllDirectories);
                var alreadyExtractedFolders = Directory.GetDirectories(extractionFolder, "*.*", SearchOption.AllDirectories);

                if (alreadyExtractedFiles.Length + alreadyExtractedFolders.Length != zip.Count)
                {
                    PatchProgressionMessage.Text = $"Extraction des fichiers du patch...";
                    zip.ExtractProgress += new EventHandler<ExtractProgressEventArgs>(ZipExtractProgress);
                    await Task.Run(() => zip.ExtractAll(extractionFolder, ExtractExistingFileAction.OverwriteSilently));
                }
            }
        }

        void ZipExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.EntriesTotal > 0)
            {
                PatchState = Convert.ToInt32(100 * ((float)e.EntriesExtracted / e.EntriesTotal));
            }
        }

        public static async Task CopyToAsync(string sourceFile, string destinationFile, IProgress<List<object>> progress, CancellationToken cancellationToken = default(CancellationToken), int bufferSize = 0x1000)
        {
            try
            {
                var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                var buffer = new byte[bufferSize];
                int bytesRead;
                long totalRead = 0;

                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    totalRead += bytesRead;
                    progress.Report(new List<object>() { totalRead, sourceStream.Length, Path.GetFileName(sourceFile) });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
