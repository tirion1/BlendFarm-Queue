using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LogicReinc.BlendFarm.Shared;
using System;
using System.Linq;

namespace LogicReinc.BlendFarm
{
    /// <summary>
    /// Translates the fixed texts of every open window when the interface language is not English.
    /// Texts are matched by their exact English content, so anything that is not a known label
    /// (names, paths, user input) is never touched. Texts generated in code are translated where they are made.
    /// </summary>
    public static class GuiLocalizer
    {
        public static void Start(IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!Loc.IsChinese)
                return;

            //Windows open and close at any time, a light periodic pass keeps new ones translated
            new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Background, (a, b) =>
            {
                foreach (Window window in desktop.Windows.ToList())
                {
                    try { Apply(window); }
                    catch (Exception ex) { Console.WriteLine("Failed to translate window: " + ex.Message); }
                }
            }).Start();
        }

        public static void Apply(Window window)
        {
            string title = Loc.T(window.Title);
            if (title != window.Title)
                window.Title = title;

            foreach (Control control in window.GetVisualDescendants().OfType<Control>().ToList())
            {
                //Lists show generated (bound) texts, translating those here would cut the binding
                if (control is TextBlock text)
                {
                    if (!text.GetVisualAncestors().Any(x => x is ListBoxItem))
                    {
                        string translated = Loc.T(text.Text);
                        if (translated != text.Text)
                            text.Text = translated;
                    }
                }
                else if (control is HeaderedContentControl headered && headered.Header is string header)
                {
                    string translated = Loc.T(header);
                    if (translated != header)
                        headered.Header = translated;
                }
                else if (control is ContentControl content && content.Content is string contentText)
                {
                    string translated = Loc.T(contentText);
                    if (translated != contentText)
                        content.Content = translated;
                }
                else if (control is TextBox box && !string.IsNullOrEmpty(box.Watermark))
                {
                    string translated = Loc.T(box.Watermark);
                    if (translated != box.Watermark)
                        box.Watermark = translated;
                }
                else if (control is AutoCompleteBox auto && !string.IsNullOrEmpty(auto.Watermark))
                {
                    string translated = Loc.T(auto.Watermark);
                    if (translated != auto.Watermark)
                        auto.Watermark = translated;
                }

                if (ToolTip.GetTip(control) is string tip)
                {
                    string translated = Loc.T(tip);
                    if (translated != tip)
                        ToolTip.SetTip(control, translated);
                }
            }
        }
    }
}
