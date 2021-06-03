using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using TradersToolbox.Core;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for TextOutput.xaml
    /// </summary>
    public partial class TextOutputView : UserControl
    {
        public TextOutputView()
        {
            InitializeComponent();
        }
    }

    public class TextFormatter
    {
        public static readonly DependencyProperty FormattedTextProperty = DependencyProperty.RegisterAttached(
            "FormattedText",
            typeof(string),
            typeof(TextFormatter),
            new FrameworkPropertyMetadata(null, OnFormattedTextChanged));

        public static void SetFormattedText(UIElement element, string value)
        {
            element.SetValue(FormattedTextProperty, value);
        }

        public static string GetFormattedText(UIElement element)
        {
            return (string)element.GetValue(FormattedTextProperty);
        }

        // todo: async
        private static void OnFormattedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textblock = (RichTextBox)d;
            var formatted = (string)e.NewValue;

            textblock.Document.Blocks.Clear();
            Paragraph p = new Paragraph();
            textblock.Document.Blocks.Add(p);

            Brush brush = new SolidColorBrush(Color.FromRgb(163, 21, 21));

            int stop = -1;
            int start = 0, i;
            do
            {
                i = formatted.IndexOf("<b>", start);
                if (i >= 0)
                {
                    p.Inlines.Add(formatted.Substring(stop + 1, i - stop - 1));
                    start = i + 3;
                    i = formatted.IndexOf("</b>", start);
                    if (i > 0)
                    {
                        p.Inlines.Add(new Run(formatted.Substring(start, i - start))
                        {
                            Foreground = brush,
                            FontWeight = FontWeights.Bold
                        });
                        stop = i + 3;
                    }
                    else
                    {
                        p.Inlines.Clear();
                        p.Inlines.Add(formatted.Replace("<b>", "").Replace("</b>", ""));

                        Logger.Current.Warn("TextOutput: Tag <b> ... </b> is not paired");
                    }
                }
                else
                {                    
                    p.Inlines.Add(formatted.Substring(stop + 1));
                }
            }
            while (i >= 0);



            /*if (string.IsNullOrEmpty(formatted))
                //textblock.Text = "";
                textblock.Document.Blocks.Clear();
            else
            {
                textblock.Document.Blocks.Clear();
                Paragraph p = new Paragraph();
                textblock.Document.Blocks.Add(p);
                try
                {
                    var nodeStack = new Stack<StyleStackNode>();
                    var root = XElement.Parse("<root>" + formatted + "</root>");
                    nodeStack.Push(new StyleStackNode(root.FirstNode));
                    while (nodeStack.Count > 0)
                    {
                        var format = nodeStack.Pop();
                        if (format.Node.NextNode != null)
                            nodeStack.Push(new StyleStackNode(format.Node.NextNode, copyFormats: format.Formatters));
                        if (format.Node is XElement tag && tag.FirstNode != null)
                        {
                            var adding = new StyleStackNode(tag.FirstNode, copyFormats: format.Formatters);
                            if (0 == string.Compare(tag.Name.LocalName, "bold", true))
                                adding.Formatters.Add(run => run.FontWeight = FontWeights.Bold);
                            else if (0 == string.Compare(tag.Name.LocalName, "b", true)) //red
                                adding.Formatters.Add(run => run.Foreground = new SolidColorBrush(Colors.Red));
                            else if (0 == string.Compare(tag.Name.LocalName, "italic", true))
                                adding.Formatters.Add(run => run.FontStyle = FontStyles.Italic);
                            nodeStack.Push(adding);
                        }
                        else if (format.Node is XText textNode)
                        {
                            var run = new Run();
                            foreach (var formatter in format.Formatters)
                                formatter(run);
                            run.Text = textNode.Value;
                            //textblock.Inlines.Add(run);
                            p.Inlines.Add(run);
                        }
                    }
                }
                catch(Exception ex)
                {
                    textblock.Document = new FlowDocument(new Paragraph(new Run(formatted)));
                    //textblock.Inlines.Add(formatted);
                }
            }*/
        }

        class StyleStackNode
        {
            public XNode Node;
            public List<Action<Run>> Formatters = new List<Action<Run>>();
            public StyleStackNode(XNode node, IEnumerable<Action<Run>> copyFormats = null)
            {
                Node = node;
                if (copyFormats != null)
                    Formatters.AddRange(copyFormats);
            }
        }
    }

    [ValueConversion(typeof(bool), typeof(GridLength))]
    public class BoolToGridRowHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value == true) ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {    // Don't need any convert back
            return null;
        }
    }
}
