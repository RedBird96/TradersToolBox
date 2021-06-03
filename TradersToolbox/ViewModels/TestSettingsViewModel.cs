using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using TradersToolbox.Core.Serializable;
using DevExpress.Mvvm.POCO;
using System.Linq;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class TestSettingsViewModel
    {
        public virtual SymbolId IntraSymbolId { get; set; }

        public static TestSettingsViewModel Create()
        {
            return ViewModelSource.Create(() => new TestSettingsViewModel());
        }
        protected TestSettingsViewModel()
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.IntraSymbol))
            {
                var sym = Core.Utils.SymbolsManager.Symbols.FirstOrDefault(x => x.Id.ToString() == Properties.Settings.Default.IntraSymbol);
                if (sym != null)
                    IntraSymbolId = sym.Id;
            }
        }

        protected void OnIntraSymbolIdChanged()
        {
            Properties.Settings.Default.IntraSymbol = IntraSymbolId.ToString();
        }
    }
}