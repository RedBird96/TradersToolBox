using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Mvvm.UI;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Charts;

namespace TradersToolbox {
    public class IntervalTimer {
        readonly DispatcherTimer timer;

        ChartIntervalItem interval;        

        public IntervalTimer() {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTimerTick;
        }

        public event EventHandler OnTickChanged;

        void OnTimerTick(object sender, EventArgs e) {
            if (ActualIntervalChanged() && OnTickChanged != null)
                OnTickChanged.Invoke(sender, e);
        }

        bool ActualIntervalChanged() {
            DateTime now = DateTime.Now;
            switch (interval.MeasureUnit) {
                case DateTimeMeasureUnit.Second:
                    if (now.Second % interval.MeasureUnitMultiplier == 0)
                        return true;
                    break;
                case DateTimeMeasureUnit.Minute:
                    if (now.Minute % interval.MeasureUnitMultiplier == 0
                        && now.Second == 0)
                        return true;
                    break;
                case DateTimeMeasureUnit.Hour:
                    if (now.Hour % interval.MeasureUnitMultiplier == 0
                        && now.Second == 0
                        && now.Minute == 0)
                        return true;
                    break;
                case DateTimeMeasureUnit.Day:
                    if (now.Day % interval.MeasureUnitMultiplier == 0
                        && now.Second == 0
                        && now.Minute == 0
                        && now.Hour == 0)
                        return true;
                    break;
                case DateTimeMeasureUnit.Week:
                    if (now.DayOfWeek == DayOfWeek.Monday
                        && now.Second == 0
                        && now.Minute == 0
                        && now.Hour == 0)
                        return true;
                    break;
                case DateTimeMeasureUnit.Month:
                    if (now.Month % interval.MeasureUnitMultiplier == 0
                        && now.Second == 0
                        && now.Minute == 0
                        && now.Hour == 0
                        && now.Day == 1)
                        return true;
                    break;
            }
            return false;
        }

        public void SetInterval(ChartIntervalItem interval) {
            this.interval = interval;
            if (!timer.IsEnabled)
                timer.Start();
        }
    }

    public static class DateTimeHelper {
        public static TimeSpan ConvertInterval(ChartIntervalItem interval, int intervalsCount) {
            return GetInterval(interval.MeasureUnit, interval.MeasureUnitMultiplier * intervalsCount);
        }

        public static DateTime GetInitialDate(ChartIntervalItem interval) {
            DateTime now = DateTime.Now;
            switch (interval.MeasureUnit) {
                case DateTimeMeasureUnit.Second:
                    DateTime roundSeconds = now.AddMilliseconds(-now.Millisecond);
                    return roundSeconds.AddSeconds(-roundSeconds.Second % interval.MeasureUnitMultiplier);
                case DateTimeMeasureUnit.Minute:
                    return now.Date.AddHours(now.Hour).AddMinutes(now.Minute - now.Minute % interval.MeasureUnitMultiplier);
                case DateTimeMeasureUnit.Hour:
                    return now.Date.AddHours(now.Hour - now.Hour % interval.MeasureUnitMultiplier);
                case DateTimeMeasureUnit.Day:
                    return now.Date;
                case DateTimeMeasureUnit.Week:
                    return now.Date.AddDays(- (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7);
                case DateTimeMeasureUnit.Month:
                    return new DateTime(now.Year, now.Month, 1);
            }
            return DateTime.Now;
        }
        public static TimeSpan GetInterval(DateTimeMeasureUnit measureUnit, int multiplier) {
            switch (measureUnit) {
                case DateTimeMeasureUnit.Second:
                    return TimeSpan.FromSeconds(multiplier);
                case DateTimeMeasureUnit.Minute:
                    return TimeSpan.FromMinutes(multiplier);
                case DateTimeMeasureUnit.Hour:
                    return TimeSpan.FromHours(multiplier);
                case DateTimeMeasureUnit.Day:
                    return TimeSpan.FromDays(multiplier);
                case DateTimeMeasureUnit.Week:
                    return TimeSpan.FromDays(multiplier * 7);
                case DateTimeMeasureUnit.Month:
                    return TimeSpan.FromDays(multiplier * 30);
            }
            return TimeSpan.Zero;
        }
    }

    public class MouseDoubleClickArgsConverter : EventArgsConverterBase<MouseButtonEventArgs> {
        protected override object Convert(object sender, MouseButtonEventArgs args) {
            if (args.ClickCount == 2 && sender is ChartControl chart && args != null)
            {
                ChartHitInfo info = chart.CalcHitInfo(args.GetPosition(chart), 3);
                if (info.InIndicator)
                    return info.Indicator;
            }
            return null;
        }
    }

    public class AddIndicatorCustomCommand : ICommand {
        ChartControl chart;

        public ChartControl Chart {
            get { return chart; }
            set {
                this.chart = value;
                if (CanExecuteChanged != null)
                    CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }

        public event EventHandler CanExecuteChanged;

        public AddIndicatorCustomCommand() {
        }

        void SetupIndicatorPane() {
            XYDiagram2D diagram = (XYDiagram2D)chart.Diagram as XYDiagram2D;
            for (int i = 1; i < chart.Legends.Count; i++)
                chart.Legends[i].VerticalPosition = VerticalPosition.Top;
            for (int i = 1; i < diagram.Panes.Count; i++)
                diagram.Panes[i].Padding = new System.Windows.Thickness(0, -19, 0, 0);
        }

        public bool CanExecute(object parameter) {
            return chart != null;
        }
        public void Execute(object parameter) {
            chart.Commands.AddIndicatorCommand.Execute(parameter);
            SetupIndicatorPane();
        }
    }
}
