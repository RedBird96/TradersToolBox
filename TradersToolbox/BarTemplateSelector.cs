using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TradersToolbox.ViewModels;

namespace TradersToolbox {
    public class BarItemTemplateSelector : DataTemplateSelector {
        public DataTemplate BarCheckItemTemplate { get; set; }
        public DataTemplate BarItemTemplate { get; set; }
        public DataTemplate BarSubItemTemplate { get; set; }
        public DataTemplate BarItemSeparatorTemplate { get; set; }
        public DataTemplate BarComboBoxTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is CommandViewModel commandViewModel)
            {
                DataTemplate template = null;
                if (commandViewModel.Owner != null)
                    template = BarCheckItemTemplate;
                if (commandViewModel.IsSubItem)
                    template = BarSubItemTemplate;
                if (commandViewModel.IsSeparator)
                    template = BarItemSeparatorTemplate;
                if (commandViewModel.IsComboBox)
                    template = BarComboBoxTemplate;
                return template ?? BarItemTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
    public class BarTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MainMenuTemplate { get; set; }
        public DataTemplate ToolbarTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            BarModel barModel = item as BarModel;
            if(barModel != null) {
                return barModel.IsMainMenu ? MainMenuTemplate : ToolbarTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
