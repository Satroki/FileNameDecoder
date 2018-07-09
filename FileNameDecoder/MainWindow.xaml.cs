using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace FileNameDecoder
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Encoding defaultEnc = Encoding.Default;
        private Encoding sourceEnc;

        public ObservableCollection<FileItem> Items { get; set; } = new ObservableCollection<FileItem>();

        public MainWindow()
        {
            InitializeComponent();

            var encs = Encoding.GetEncodings();
            CbEncoding.ItemsSource = encs;
            var jis = encs.FirstOrDefault(enc => enc.CodePage == 932);
            if (jis != null)
            {
                CbEncoding.SelectedItem = jis;
                sourceEnc = jis.GetEncoding();
            }

            LvItems.ItemsSource = Items;
            LvNewItems.ItemsSource = Items;
        }

        private void TbPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetItems();
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            if (RbAll.IsChecked == true)
            {
                var files = Items.Where(i => i.FileSystemInfo is FileInfo).ToList();
                var dirs = Items.Where(i => i.FileSystemInfo is DirectoryInfo).ToList();
                foreach (var f in files)
                {
                    if (f.DestName == f.SourceName)
                        continue;
                    var dest = Path.Combine(f.Root, f.DestName);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    (f.FileSystemInfo as FileInfo).MoveTo(dest);
                }
                foreach (var d in dirs)
                {
                    if (d.DestName == d.SourceName)
                        continue;
                    if (Directory.Exists(d.FileSystemInfo.FullName))
                        (d.FileSystemInfo as DirectoryInfo).Delete(true);
                }
            }
            else
            {
                foreach (var i in Items)
                {
                    if (i.DestName == i.SourceName)
                        continue;
                    var dest = Path.Combine(i.Root, i.DestName);
                    switch (i.FileSystemInfo)
                    {
                        case DirectoryInfo di: di.MoveTo(dest); break;
                        case FileInfo fi: fi.MoveTo(dest); break;
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private string NewName(string old)
        {
            var bytes = defaultEnc.GetBytes(old);
            return sourceEnc.GetString(bytes);
        }

        private void GetItems()
        {
            var p = TbPath.Text.TrimEnd('\\');
            FileSystemInfo fsi = null;
            if (Directory.Exists(p))
            {
                fsi = new DirectoryInfo(p);
            }
            else if (File.Exists(p))
            {
                fsi = new FileInfo(p);
            }
            else
                return;
            Items.Clear();
            if (RbSelf.IsChecked == true)
            {
                string root = "";
                switch (fsi)
                {
                    case DirectoryInfo di:
                        root = di.Parent.FullName; break;
                    case FileInfo fi:
                        root = fi.DirectoryName; break;
                }
                Items.Add(new FileItem
                {
                    FileSystemInfo = fsi,
                    Root = root,
                    SourceName = fsi.Name,
                });
            }
            else if (fsi is DirectoryInfo di)
            {
                if (RbChildren.IsChecked == true)
                {
                    foreach (var i in di.GetFileSystemInfos())
                    {
                        Items.Add(new FileItem
                        {
                            FileSystemInfo = i,
                            Root = di.FullName,
                            SourceName = i.Name
                        });
                    }
                }
                else
                {
                    var len = di.FullName.Length + 1;
                    foreach (var i in di.GetFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        Items.Add(new FileItem
                        {
                            FileSystemInfo = i,
                            Root = di.FullName,
                            SourceName = i.FullName.Substring(len)
                        });
                    }
                }
            }
            Preview();
        }

        private void Preview()
        {
            foreach (var item in Items)
            {
                item.DestName = NewName(item.SourceName);
            }
        }

        private void CbEncoding_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ei = CbEncoding.SelectedItem as EncodingInfo;
            sourceEnc = ei.GetEncoding();
            Preview();
        }

        private void Rb_Checked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbPath.Text))
                return;
            GetItems();
        }
    }

    public class FileItem : INotifyPropertyChanged
    {
        private string _destName;

        public FileSystemInfo FileSystemInfo { get; set; }
        public string Root { get; set; }
        public string SourceName { get; set; }
        public string DestName
        {
            get => _destName;
            set
            {
                if (_destName != value)
                {
                    _destName = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
