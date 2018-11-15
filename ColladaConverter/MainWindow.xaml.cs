using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private string FolderDialog(string description)
        {
            FolderSelect.FolderSelectDialog dlg = new FolderSelect.FolderSelectDialog()
            {
                Title = description,
                InitialDirectory = System.IO.Directory.GetCurrentDirectory(),
            };
            if (dlg.ShowDialog()) return dlg.FileName;
            return null;
        }

        private void SrcBtn_Click(object sender, RoutedEventArgs e)
        {
            string folder = FolderDialog("Source Folder");
            if (folder != null) SrcBox.Text = folder;
            SrcBox_LostFocus(SrcBox, null);
        }

        private void DestBtn_Click(object sender, RoutedEventArgs e)
        {
            string folder = FolderDialog("Destination Folder");
            if (folder != null) DestBox.Text = folder;
        }

        private void ConvBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog()
            {
                DefaultExt = ".png",
                Filter = "Exe Files (*.exe)|*.exe",
            };
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
                ConvBox.Text = dlg.FileName;
        }

        private void SrcBox_LostFocus(object sender, RoutedEventArgs e)
        {
            FileListStack.Children.RemoveRange(1, FileListStack.Children.Count - 1);
            ModelFileStack.Children.RemoveRange(1, ModelFileStack.Children.Count - 1);
            AnimFileStack.Children.RemoveRange(1, AnimFileStack.Children.Count - 1);
            if (SrcBox.Text == String.Empty || !System.IO.Directory.Exists(SrcBox.Text))
            {
                return;
            }
            string[] files = System.IO.Directory.GetFiles(SrcBox.Text, "*.dae", System.IO.SearchOption.AllDirectories);
            foreach (string file in files)
            {
                FileListStack.Children.Add(new TextBlock() { Text = file, });
                {
                    CheckBox check = new CheckBox() { IsChecked = true };
                    check.Unchecked += Model_UnChecked;
                    ModelFileStack.Children.Add(check);
                }
                {
                    CheckBox check = new CheckBox() { IsChecked = true };
                    check.Unchecked += Anim_UnChecked;
                    AnimFileStack.Children.Add(check);
                }
            }
        }

        private void Model_UnChecked(object sender, RoutedEventArgs e)
        {
            AllModel.IsChecked = false;
        }

        private void Anim_UnChecked(object sender, RoutedEventArgs e)
        {
            AllAnim.IsChecked = false;
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.Width = 0;
            OutputBlock.Text = String.Empty;
            OutputBlock.Text += "Generating...." + Environment.NewLine;
            if (!ConvBox.Text.Contains(".exe") || !System.IO.File.Exists(ConvBox.Text))
            {
                OutputBlock.Text += "[ERROR] Must have a valid .exe for the Collada Converter.";
                return;
            }
            if (SrcBox.Text == String.Empty || !System.IO.Directory.Exists(SrcBox.Text))
            {
                OutputBlock.Text += "[ERROR] Must have a valid Source Folder.";
                return;
            }
            string dest = DestBox.Text;
            if (dest == String.Empty || !System.IO.Directory.Exists(dest))
            {
                dest = SrcBox.Text;
                OutputBlock.Text += "[WARNING] Destination Folder not valid, using Source Folder.";
            }

            string overwrite = String.Empty;
            if (OverwriteMats.IsChecked == true) overwrite = "-overwriteMats";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            if (AllModel.IsChecked != true && AllAnim.IsChecked != true)
            {
                int cnt = FileListStack.Children.Count;
                double progress = 0.5 * ProgressBarWidth.Width / cnt;
                for (int i = 1; i < cnt; i++)
                {
                    ProgressBar.Width += progress;
                    if ((ModelFileStack.Children[i] as CheckBox).IsChecked == true)
                    {
                        string file = (FileListStack.Children[i] as TextBlock).Text;
                        process.StartInfo.Arguments = $"/C {ConvBox.Text} {file} -base {SrcBox.Text} -dest {dest} -type model {overwrite}";
                        process.Start();
                        OutputBlock.Text += process.StartInfo.Arguments + Environment.NewLine;
                        OutputBlock.Text += process.StandardOutput.ReadToEnd() + Environment.NewLine;
                        Debug.WriteLine(process.StandardError.ReadToEnd());
                        process.WaitForExit();
                    }
                    ProgressBar.Width += progress;
                    if ((AnimFileStack.Children[i] as CheckBox).IsChecked == true)
                    {
                        string file = (FileListStack.Children[i] as TextBlock).Text;
                        process.StartInfo.Arguments = $"/C {ConvBox.Text} {file} -base {SrcBox.Text} -dest {dest} -type anim {overwrite}";
                        process.Start();
                        OutputBlock.Text += process.StartInfo.Arguments + Environment.NewLine;
                        OutputBlock.Text += process.StandardOutput.ReadToEnd() + Environment.NewLine;
                        Debug.WriteLine(process.StandardError.ReadToEnd());
                        process.WaitForExit();
                    }
                }
            }
            else
            {
                if (AllModel.IsChecked == true)
                {
                    process.StartInfo.Arguments = $"/C {ConvBox.Text} . -base {SrcBox.Text} -dest {dest} -type model {overwrite}";
                    process.Start();
                    OutputBlock.Text += process.StartInfo.Arguments + Environment.NewLine;
                    OutputBlock.Text += process.StandardOutput.ReadToEnd() + Environment.NewLine;
                    Debug.WriteLine(process.StandardError.ReadToEnd());
                    process.WaitForExit();
                    ProgressBar.Width = ProgressBarWidth.Width;
                }
                else
                {
                    int cnt = ModelFileStack.Children.Count;
                    double progress = ProgressBarWidth.Width / cnt;
                    for (int i = 1; i < cnt; i++)
                    {
                        ProgressBar.Width += progress;
                        if ((ModelFileStack.Children[i] as CheckBox).IsChecked != true) continue;
                        string file = (FileListStack.Children[i] as TextBlock).Text;
                        process.StartInfo.Arguments = $"/C {ConvBox.Text} {file} -base {SrcBox.Text} -dest {dest} -type model {overwrite}";
                        process.Start();
                        OutputBlock.Text += process.StartInfo.Arguments + Environment.NewLine;
                        OutputBlock.Text += process.StandardOutput.ReadToEnd() + Environment.NewLine;
                        Debug.WriteLine(process.StandardError.ReadToEnd());
                        process.WaitForExit();
                    }
                }
                if (AllAnim.IsChecked == true)
                {
                    process.StartInfo.Arguments = $"/C {ConvBox.Text} . -base {SrcBox.Text} -dest {dest} -type anim {overwrite}";
                    process.Start();
                    OutputBlock.Text += process.StartInfo.Arguments + Environment.NewLine;
                    OutputBlock.Text += process.StandardOutput.ReadToEnd() + Environment.NewLine;
                    Debug.WriteLine(process.StandardError.ReadToEnd());
                    process.WaitForExit();
                    ProgressBar.Width = ProgressBarWidth.Width;
                }
                else
                {
                    int cnt = ModelFileStack.Children.Count;
                    double progress = ProgressBarWidth.Width / cnt;
                    for (int i = 1; i < cnt; i++)
                    {
                        ProgressBar.Width += progress;
                        if ((ModelFileStack.Children[i] as CheckBox).IsChecked != true) continue;
                        string file = (FileListStack.Children[i] as TextBlock).Text;
                        process.StartInfo.Arguments = $"/C {ConvBox.Text} {file} -base {SrcBox.Text} -dest {dest} -type anim {overwrite}";
                        process.Start();
                        OutputBlock.Text += process.StartInfo.Arguments + Environment.NewLine;
                        OutputBlock.Text += process.StandardOutput.ReadToEnd() + Environment.NewLine;
                        Debug.WriteLine(process.StandardError.ReadToEnd());
                        process.WaitForExit();
                    }
                }
            }
            if (ExplorerCheck.IsChecked == true) Process.Start(dest);
            OutputBlock.Text += "Done.";
        }

        private void AllModel_Checked(object sender, RoutedEventArgs e)
        {
            if (ModelFileStack == null) return;
            int cnt = ModelFileStack.Children.Count;
            for (int i = 1; i < cnt; i++)
            {
                CheckBox check = ModelFileStack.Children[i] as CheckBox;
                check.IsChecked = true;
            }
        }

        private void AllAnim_Checked(object sender, RoutedEventArgs e)
        {
            if (AnimFileStack == null) return;
            int cnt = AnimFileStack.Children.Count;
            for (int i = 1; i < cnt; i++)
            {
                CheckBox check = AnimFileStack.Children[i] as CheckBox;
                check.IsChecked = true;
            }
        }

        private void ClearOutput_Click(object sender, RoutedEventArgs e)
        {
            OutputBlock.Text = String.Empty;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox search = sender as TextBox;
            int cnt = FileListStack.Children.Count;
            for (int i = 1; i < cnt; i++)
            {
                TextBlock block = FileListStack.Children[i] as TextBlock;
                if (block.Text.Contains(search.Text))
                {
                    block.Visibility = Visibility.Visible;
                    ModelFileStack.Children[i].Visibility = Visibility.Visible;
                    AnimFileStack.Children[i].Visibility = Visibility.Visible;
                }
                else
                {
                    block.Visibility = Visibility.Collapsed;
                    ModelFileStack.Children[i].Visibility = Visibility.Collapsed;
                    AnimFileStack.Children[i].Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ExplorerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DestBox.Text == String.Empty || !System.IO.Directory.Exists(DestBox.Text))
            {
                Process.Start(SrcBox.Text);
            }
            else
            {
                Process.Start(DestBox.Text);
            }
        }

        private void DestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UserMessage.Visibility = Visibility.Hidden;
        }

        private void DestBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DestBox.Text == String.Empty) UserMessage.Visibility = Visibility.Visible;
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchMessage.Visibility = Visibility.Hidden;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchMessage.Text == String.Empty) SearchMessage.Visibility = Visibility.Visible;
        }
    }
}
