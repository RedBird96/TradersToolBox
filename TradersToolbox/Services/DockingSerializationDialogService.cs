using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xaml;
using DevExpress.Mvvm.POCO;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Docking;
using Microsoft.Win32;

namespace TradersToolbox
{
    public interface IDockingSerializationDialogService
    {
        void SaveLayout(object o);
        List<T> LoadLayout<T>();

        void RestoreLastLoadedLayout();
    }
    public class DockingSerializationDialogService : ServiceBase, IDockingSerializationDialogService
    {
        const string filter = "Configuration (*.xml)|*.xml|All files (*.*)|*.*";
        public DockLayoutManager DockLayoutManager { get; set; }


        public List<T> LoadLayout<T>()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = filter };
            var openResult = openFileDialog.ShowDialog();
            if (openResult.HasValue && openResult.Value)
            {
                LastLayoutXML = openFileDialog.FileName;
                return DeserializePOCOViewModelFromFile<T>(openFileDialog.FileName + ".bin");
            }
            return default(List<T>);

        }
        public void SaveLayout(object o)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = filter };
            var saveResult = saveFileDialog.ShowDialog();
            if (saveResult.HasValue && saveResult.Value)
            {
                DockLayoutManager.SaveLayoutToXml(saveFileDialog.FileName);
                SerializeToFile(saveFileDialog.FileName + ".bin", o);
            }
        }

        private string LastLayoutXML;

        public void RestoreLastLoadedLayout()
        {
            DockLayoutManager.RestoreLayoutFromXml(LastLayoutXML);
        }
        protected override void OnAttached()
        {
            base.OnAttached();
            DockLayoutManager = AssociatedObject as DockLayoutManager;
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();
            DockLayoutManager = null;
        }

        private void SerializeToFile(string fileName, object o)
        {
            XamlServices.Save(fileName, o);
        }

        private static List<T> DeserializePOCOViewModelFromFile<T>(string fileName)
        {
            try
            {
                var xaml = File.ReadAllText(fileName);

                var m = Regex.Matches(xaml, @"<([\w]*:)?(?<dxTypeName>\w+_[\w]{8}_[\w]{4}_[\w]{4}_[\w]{4}_[\w]{12})");

                var uniqueList = new HashSet<string>();
                foreach (Match item in m)
                {
                    uniqueList.Add(item.Groups["dxTypeName"].Value);
                }

                foreach (var item in uniqueList)
                {
                    var baseTypeName = item.Substring(0, item.Length - 37);
                    var t = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(f => f.Name == baseTypeName);

                    if (t != null)
                    {
                        xaml = xaml.Replace(item, ViewModelSource.GetPOCOType(t).Name);
                    }
                }

                var assemblyName = ViewModelSource.GetPOCOType(typeof(T)).Assembly.GetName().Name;
                xaml = Regex.Replace(xaml, @"(DevExpress\.Mvvm\.v\d+\.*\d*\.DynamicTypes\.[\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12})", assemblyName);

                return (List<T>)XamlServices.Parse(xaml);
            }
            catch (Exception ex)
            {
                // Add Log  
                return default(List<T>);
            }
        }
    }
}