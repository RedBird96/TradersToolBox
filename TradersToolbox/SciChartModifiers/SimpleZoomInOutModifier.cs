using SciChart.Charting.ChartModifiers;
using SciChart.Core.Utility.Mouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace TradersToolbox.SciChartModifiers
{
    public class SimpleZoomInOutModifier : ChartModifierBase
    {
        public static readonly DependencyProperty ZoomFractionProperty = DependencyProperty.Register("ZoomFraction", typeof(double), typeof(SimpleZoomInOutModifier), new PropertyMetadata(0.1));

        public double ZoomFraction
        {
            get { return (double)GetValue(ZoomFractionProperty); }
            set { SetValue(ZoomFractionProperty, value); }
        }

        public override void OnModifierKeyDown(ModifierKeyArgs e)
        {
            base.OnModifierKeyDown(e);

            double factor = 0;

            if (e.Key == Key.Up)
            {
                factor = -ZoomFraction;
            }
            if (e.Key == Key.Down)
            {
                factor = ZoomFraction;
            }

            using (ParentSurface.SuspendUpdates())
            {
                // Zoom the XAxis by the required factor
                XAxis.ZoomBy(factor, factor, TimeSpan.FromMilliseconds(500));

                // Zoom the YAxis by the required factor
                YAxis.ZoomBy(factor, factor, TimeSpan.FromMilliseconds(500));

                e.Handled = true;
            }
        }
    }
}
