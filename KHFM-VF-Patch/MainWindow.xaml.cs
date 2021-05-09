using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
    public partial class MainWindow : Window
    {
        private const string DEFAULT_GAME_FOLDER = @"C:\Program Files\Epic Games\KH_1.5_2.5";
        private const string SAVE_FOLDER_NAME = "Saves";
        private static readonly List<string> REQUIRED_FILES = new List<string>()
        {
            "Image/en/kh1_first.pkg", "Image/en/kh1_first.hed",
            "Image/en/kh1_second.pkg", "Image/en/kh1_second.hed",
            "Image/en/kh1_third.pkg", "Image/en/kh1_third.hed",
            "Image/en/kh1_fourth.pkg", "Image/en/kh1_fourth.hed",
            "Image/en/kh1_fifth.pkg", "Image/en/kh1_fifth.hed",
        };

        public MainWindow()
        {
            InitializeComponent();

            if (CheckGameFolder(DEFAULT_GAME_FOLDER))
            {
                // TODO: Hide the "browse" button
                Patch(DEFAULT_GAME_FOLDER);
            }
            else
            {
                Debug.WriteLine("Default game folder not found.");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Debug.WriteLine($"Selected: {dialog.SelectedPath}");

                    if (CheckGameFolder(dialog.SelectedPath))
                    {
                        Patch(dialog.SelectedPath);
                    }
                    else
                    {
                        Debug.WriteLine("The selected folder is not valid!");
                    }
                }
            }
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

        private void Patch(string gameFolder)
        {
            Debug.WriteLine("Patch the game!!!");

            // Save the original files
            var saveFolder = Path.Combine(gameFolder, SAVE_FOLDER_NAME);

            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);

                foreach (var requiredFile in REQUIRED_FILES)
                {
                    var completePath = Path.Combine(gameFolder, requiredFile);
                    File.Copy(completePath, Path.Combine(saveFolder, Path.GetFileName(requiredFile)));
                }
            }
            else
            {
                // TODO(bth): Show a button to unpatch the game

                // Copy saved files in the original folder back, to make sure we patch the original files
                foreach (var requiredFile in REQUIRED_FILES)
                {
                    var completePath = Path.Combine(gameFolder, requiredFile);
                    File.Copy(completePath, Path.Combine(Path.GetFileName(requiredFile), saveFolder), true);
                }
            }
        }
    }
}
