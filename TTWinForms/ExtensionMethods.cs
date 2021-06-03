using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TTWinForms
{
    public static class ExtensionMethods
    {
        public static void ModifyContextMenu(this ZedGraph.ZedGraphControl zed)
        {
            if (zed != null)
                zed.ContextMenuBuilder += Zed_ContextMenuBuilder;
        }

        private static void Zed_ContextMenuBuilder(ZedGraph.ZedGraphControl sender, ContextMenuStrip menuStrip, System.Drawing.Point mousePt,
            ZedGraph.ZedGraphControl.ContextMenuObjectState objState)
        {
            ToolStripItem newMenuItem = new ToolStripMenuItem("Show/hide legend");
            newMenuItem.Click += NewMenuItem_Click;
            newMenuItem.Tag = sender;
            menuStrip.Items.Insert(0, newMenuItem);
        }

        private static void NewMenuItem_Click(object sender, EventArgs e)
        {
            ZedGraph.ZedGraphControl zed = (sender as ToolStripMenuItem).Tag as ZedGraph.ZedGraphControl;
            zed.GraphPane.Legend.IsVisible = !zed.GraphPane.Legend.IsVisible;
            zed.Invalidate();
        }
    }
}
