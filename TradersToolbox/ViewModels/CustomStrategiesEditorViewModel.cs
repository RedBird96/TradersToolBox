using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class CustomStrategiesEditorViewModel
    {
        public class CustomStratItem
        {
            public bool IsEnabled { get; set; }
            public string FileName { get; set; }
            public bool IsExists { get; set; }
        }

        public virtual ObservableCollection<string> CustomStratList { get; set; } = new ObservableCollection<string>();
        public virtual ObservableCollection<CustomStratItem> CustomStratFiles { get; set; }
        public virtual ObservableCollection<CustomStratItem> SelectedStratFiles { get; set; } = new ObservableCollection<CustomStratItem>();
        public virtual CustomStratItem SelectedStratFile { get; set; }

        protected void OnSelectedStratFileChanged()
        {
            CustomStratList.Clear();

            if (SelectedStratFile == null)
            {
                return;
            }

            Task.Run(() =>
            {
                string filename = SelectedStratFile.FileName;
                List<string> list = new List<string>();
                try
                {
                    using (StreamReader reader = new StreamReader(File.OpenRead(filename)))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            if (line.Length > 0 && line[0] == '#')
                                list.Add(line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBoxService.Show("Can't read custom strategy file!\n\nError: " + ex.Message, "Custom strategy file read error");
                    });
                }

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    CustomStratList.Clear();
                    foreach (var s in list)
                        CustomStratList.Add(s);
                });
            });
        }
        

        protected IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }
        protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(); } }
        protected ICurrentWindowService CurrentWindowService { get { return this.GetService<ICurrentWindowService>(); } }

        public static CustomStrategiesEditorViewModel Create()
        {
            return ViewModelSource.Create(() => new CustomStrategiesEditorViewModel());
        }
        protected CustomStrategiesEditorViewModel() { }

        #region Commands
        public void Loaded()
        {
            CustomStratFiles = new ObservableCollection<CustomStratItem>();

            if (Properties.Settings.Default.CustomStrategies != null)
                foreach (string s in Properties.Settings.Default.CustomStrategies)
                {
                    string name = s.Substring(1);

                    CustomStratItem item = new CustomStratItem()
                    {
                        FileName = name,
                        IsEnabled = (s[0] == '1'),
                        IsExists = System.IO.File.Exists(name)
                    };
                    item.IsEnabled &= item.IsExists;

                    CustomStratFiles.Add(item);
                }
        }

        public void OK()
        {
            if (Properties.Settings.Default.CustomStrategies == null)
                Properties.Settings.Default.CustomStrategies = new System.Collections.Specialized.StringCollection();
            else
                Properties.Settings.Default.CustomStrategies.Clear();
            foreach (var i in CustomStratFiles)
                Properties.Settings.Default.CustomStrategies.Add($"{(i.IsEnabled ? "1" : "0")}{i.FileName}");

            CurrentWindowService.Close();
        }

        public void Cancel()
        {
            CurrentWindowService.Close();
        }

        public void Open()
        {
            if (OpenFileDialogService.ShowDialog())
            {
                foreach (IFileInfo file in OpenFileDialogService.Files)
                {
                    string fullName = file.GetFullName();
                    if (!CustomStratFiles.Any(x => x.FileName == fullName))
                    {
                        CustomStratItem item = new CustomStratItem()
                        {
                            FileName = fullName,
                            IsEnabled = true,
                            IsExists = true
                        };
                        CustomStratFiles.Add(item);
                    }
                }
            }
        }
        public void Delete()
        {
            var toDelete = SelectedStratFiles.ToList();
            SelectedStratFiles.Clear(); // prevent OnSelectedStratFileChanged running multiple times
            foreach (var i in toDelete)
                CustomStratFiles.Remove(i);
        }
        public void DeleteAll()
        {
            CustomStratFiles.Clear();
        }
        #endregion
    }
}