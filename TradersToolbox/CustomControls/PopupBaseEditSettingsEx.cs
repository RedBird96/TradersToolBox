using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Helpers;
using DevExpress.Xpf.Editors.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TradersToolbox.CustomControls
{
    public class PopupBaseEditSettingsEx : PopupBaseEditSettings
    {
        public PopupBaseEdit Popup => _Popup;
        private PopupBaseEdit _Popup;

        protected override void AssignToEditCore(IBaseEdit edit)
        {
            _Popup = edit as PopupBaseEdit;
            base.AssignToEditCore(edit);
        }

    }
}
