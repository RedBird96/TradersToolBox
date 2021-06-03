using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using DevExpress.Mvvm.POCO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using System.IO;
using System.Threading.Tasks;
using TradersToolbox.Core;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class TextOutputViewModel : IDocumentContent
    {
        /// <summary>
        /// Plain text with some formatting like <b></b>, <bold></bold>, <italic></italic>
        /// </summary>
        public virtual string MainFormattedText { get; set; }

        public virtual List<KeyValuePair<string, string>> ExtraCode { get; set; }


        public virtual double WindowWidth { get; set; }
        public virtual double WindowHeight { get; set; }

        protected void OnWindowWidthChanged()
        {
            Properties.Settings.Default.TextOutWindowWidth = WindowWidth;
        }

        protected void OnWindowHeightChanged()
        {
            Properties.Settings.Default.TextOutWindowHeight = WindowHeight;
        }

        public List<string> MenuModel { get; private set; } = new List<string>() { "Save" };
        //public BarModel MenuModel { get; private set; }
        //public ReadOnlyCollection<BarModel> Bars { get; private set; }
        //public ReadOnlyCollection<CommandViewModel> MenuButtons { get; private set; }

        #region IDocumentContent
        public object Title { get; set; } = "Text output";
        public IDocumentOwner DocumentOwner { get; set; }

        public void OnClose(CancelEventArgs e)
        {
        }

        public void OnDestroy()
        {
        }
        #endregion

        protected ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }

        public static TextOutputViewModel Create()
        {
            return ViewModelSource.Create(() => new TextOutputViewModel());
        }

        protected TextOutputViewModel()
        {
            WindowWidth = Properties.Settings.Default.TextOutWindowWidth;
            WindowHeight = Properties.Settings.Default.TextOutWindowHeight;

            //CommandViewModel saveCommand = new CommandViewModel("Save", new DelegateCommand(ShowSave))
            //{ /*Glyph = Images.Save,*/ KeyGesture = new KeyGesture(Key.S, ModifierKeys.Control) };
            //
            //CommandViewModel saveCommand2 = new CommandViewModel("Upfsrd", new DelegateCommand(ShowSave))
            //{ /*Glyph = Images.Save,*/ KeyGesture = new KeyGesture(Key.E, ModifierKeys.Control) };

            // MenuButtons = new ReadOnlyCollection<CommandViewModel>(new List<CommandViewModel>() { saveCommand, saveCommand2 });

            //MenuModel = new BarModel("") { IsMainMenu = true, Commands = new List<CommandViewModel>() { saveCommand, saveCommand2 } };

            //var fileCommand = new CommandViewModel("File", new List<CommandViewModel>() { saveCommand });
            //var bar = new BarModel("MainMenu")
            //{
            //    IsMainMenu = true,
            //    Commands = new List<CommandViewModel>() { fileCommand }
            //};
            //var list = new List<BarModel>() { bar };
            //
            //Bars = new ReadOnlyCollection<BarModel>(list);
        }

        public async Task Save()
        {
            SaveFileDialogService.DefaultExt = "txt";
            SaveFileDialogService.DefaultFileName = "code";
            SaveFileDialogService.Filter = "Text Files (.txt)|*.txt|All Files (*.*)|*.*";
            SaveFileDialogService.FilterIndex = 1;

            if (SaveFileDialogService.ShowDialog())
            {
                string plainText = MainFormattedText;

                await Task.Run(() =>
                {
                    plainText = plainText.Replace("<b>", "");
                    plainText = plainText.Replace("</b>", "");

                    plainText = plainText.Replace("<bold>", "");
                    plainText = plainText.Replace("</bold>", "");

                    plainText = plainText.Replace("<italic>", "");
                    plainText = plainText.Replace("</italic>", "");
                });

                try
                {
                    using (var stream = new StreamWriter(SaveFileDialogService.OpenFile()))
                    {
                        await stream.WriteAsync(plainText);
                    }
                }
                catch(Exception ex)
                {
                    Logger.Current.Warn(ex, "Unable to save data to file");
                }
            }
        }
    }
}