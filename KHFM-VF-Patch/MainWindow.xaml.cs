using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KHFM_VF_Patch
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // TODO: Find the EGS folder and hide the button if it's found
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            }


            //TaskDialog();

            //var folderDialog = new CommonOpenFileDialog { IsFolderPicker = true };
            //var result = folderDialog.ShowDialog();

            //if (result == CommonFileDialogResult.Ok)
            //{
            //    MessageBox.Show("OK");
            //}
        }
    }
}
