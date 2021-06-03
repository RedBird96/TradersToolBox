using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class SettingsViewModel
    {
        public virtual string[] UpdatePeriods { get; set; } = { "Always", "Daily", "Weekly", "Monthly", "Never" };

        public virtual string DBupdatePeriod { get; set; }

        public virtual bool UseExtendedHours { get; set; }

        public void OnDBupdatePeriodChanged()
        {
            switch (DBupdatePeriod)
            {
                default:
                case "Always":  Properties.Settings.Default.DatabaseUpdatePeriod = -1;  break;
                case "Daily":   Properties.Settings.Default.DatabaseUpdatePeriod = 24;  break;
                case "Weekly":  Properties.Settings.Default.DatabaseUpdatePeriod = 168; break;
                case "Monthly": Properties.Settings.Default.DatabaseUpdatePeriod = 720; break;
                case "Never":   Properties.Settings.Default.DatabaseUpdatePeriod = 0;   break;
            }
        }

        public void OnUseExtendedHoursChanged()
        {
            if (Properties.Settings.Default.UseExtendedHours != UseExtendedHours)
            {
                Properties.Settings.Default.UseExtendedHours = UseExtendedHours;
                Messenger.Default.Send(new ExtendedHoursChangedMessage(UseExtendedHours));
            }
        }

        public static SettingsViewModel Create()
        {
            return ViewModelSource.Create(() => new SettingsViewModel());
        }
        protected SettingsViewModel()
        {
            switch (Properties.Settings.Default.DatabaseUpdatePeriod)
            {
                default:
                case -1:  DBupdatePeriod = "Always";  break;
                case 24:  DBupdatePeriod = "Daily";   break;
                case 168: DBupdatePeriod = "Weekly";  break;
                case 720: DBupdatePeriod = "Monthly"; break;
                case 0:   DBupdatePeriod = "Never";   break;
            }

            UseExtendedHours = Properties.Settings.Default.UseExtendedHours;
        }
    }
}