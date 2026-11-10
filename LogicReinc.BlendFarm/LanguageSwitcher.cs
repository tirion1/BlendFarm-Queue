using Avalonia.Controls;
using LogicReinc.BlendFarm.Shared;
using LogicReinc.BlendFarm.Windows;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LogicReinc.BlendFarm
{
    /// <summary>
    /// The language entry in the interface. The language is applied when the program starts,
    /// so a change is saved and the program is restarted on request.
    /// </summary>
    public static class LanguageSwitcher
    {
        public static async Task Switch(Window owner)
        {
            string next = Loc.IsChinese ? Loc.English : Loc.Chinese;
            BlendFarmSettings.Instance.Language = next;
            BlendFarmSettings.Instance.Save();

            //Shown in both languages on purpose, it has to be readable whichever one is being left
            string name = next == Loc.Chinese ? "中文" : "English";
            bool restart = await YesNoWindow.Show(owner, "Language / 语言",
                $"Language set to {name}. Restart now to apply it? (A render in progress will be stopped)\n" +
                $"语言已设置为 {name}。现在重启以生效吗？（正在进行的渲染会被中断）");
            if (!restart)
                return;

            try
            {
                Process.Start(new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = true });
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                await MessageWindow.Show(owner, "Language / 语言", "Restart manually to apply the language / 请手动重启程序以生效：" + ex.Message);
            }
        }
    }
}
