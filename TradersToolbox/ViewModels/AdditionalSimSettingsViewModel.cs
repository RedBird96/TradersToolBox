using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using TradersToolbox.Core.Serializable;
using DevExpress.Mvvm.POCO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TradersToolbox.Core;
using System.Linq;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class AdditionalSimSettingsViewModel : ISupportParameter
    {
        public ObservableCollection<Symbol> Symbols => Utils.SymbolsManager.Symbols;

        public virtual SimulationSettings SimSettings { get; set; }
        public virtual SymbolId Vs1SymbolId { get; set; }
        public virtual SymbolId Vs2SymbolId { get; set; }
        public virtual SymbolId Vs3SymbolId { get; set; }

        protected void OnVs1SymbolIdChanged()
        {
            Properties.Settings.Default.VsOther1Symbol = Vs1SymbolId.ToString();
        }

        protected void OnVs2SymbolIdChanged()
        {
            Properties.Settings.Default.VsOther2Symbol = Vs2SymbolId.ToString();
        }

        protected void OnVs3SymbolIdChanged()
        {
            Properties.Settings.Default.VsOther3Symbol = Vs3SymbolId.ToString();
        }

        public static AdditionalSimSettingsViewModel Create()
        {
            return ViewModelSource.Create(() => new AdditionalSimSettingsViewModel());
        }
        protected AdditionalSimSettingsViewModel() { }

        public virtual object Parameter { get; set; }

        public void OnParameterChanged()
        {
            if (Parameter is SimulationSettings settings)
            {
                SimSettings = settings;

                // Read VsOthers on SimSettings change (on window open or config change/reset)
                if (!string.IsNullOrEmpty(Properties.Settings.Default.VsOther1Symbol))
                {
                    Vs1SymbolId = Utils.SymbolsManager.Symbols.FirstOrDefault(x => x.Id.ToString() == Properties.Settings.Default.VsOther1Symbol)?.Id ?? SymbolId.Empty;
                }
                else
                    Vs1SymbolId = SymbolId.Empty;


                if (!string.IsNullOrEmpty(Properties.Settings.Default.VsOther2Symbol))
                {
                    Vs2SymbolId = Utils.SymbolsManager.Symbols.FirstOrDefault(x => x.Id.ToString() == Properties.Settings.Default.VsOther2Symbol)?.Id ?? SymbolId.Empty;
                }
                else
                    Vs2SymbolId = SymbolId.Empty;

                if (!string.IsNullOrEmpty(Properties.Settings.Default.VsOther3Symbol))
                {
                    Vs3SymbolId = Utils.SymbolsManager.Symbols.FirstOrDefault(x => x.Id.ToString() == Properties.Settings.Default.VsOther3Symbol)?.Id ?? SymbolId.Empty;
                }
                else
                    Vs3SymbolId = SymbolId.Empty;
            }
        }

        /*public void Unloaded()
        {
            Properties.Settings.Default.VsOther1Symbol = Vs1SymbolId.ToString();
            Properties.Settings.Default.VsOther2Symbol = Vs2SymbolId.ToString();
            Properties.Settings.Default.VsOther3Symbol = Vs3SymbolId.ToString();
        }*/
    }
}