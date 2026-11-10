using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LogicReinc.BlendFarm.Client;
using LogicReinc.BlendFarm.Objects;
using LogicReinc.BlendFarm.Server;
using LogicReinc.BlendFarm.Shared;
using LogicReinc.BlendFarm.Shared.Communication.RenderNode;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LogicReinc.BlendFarm.Controls
{
    /// <summary>
    /// Who is rendering and who is waiting, on this computer's own server (which can be rearranged) and on
    /// every connected server as it reports it (read only), including where this computer stands in them.
    /// </summary>
    public class QueuePanel : UserControl
    {
        public ObservableCollection<ServerQueueItem> ServerQueue { get; } = new ObservableCollection<ServerQueueItem>();

        private TextBlock _summary = null;

        public QueuePanel()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _summary = this.Find<TextBlock>("queueSummary");
        }

        /// <summary>
        /// Updates the list, called every second by the sidebar
        /// </summary>
        /// <returns>The text for the header: how many are in line and where this computer stands</returns>
        public string Refresh()
        {
            try
            {
                List<ServerQueueItem> rows = new List<ServerQueueItem>();
                List<string> summary = new List<string>();
                bool hasSource = false;

                RenderQueue local = LocalServer.Available ? LocalServer.Manager?.Queue : null;
                if (local != null)
                {
                    hasSource = true;
                    int waiting = 0;
                    foreach (RenderQueue.RenderQueueEntryInfo entry in local.GetSnapshot())
                    {
                        if (!entry.IsRunning)
                            waiting++;
                        rows.Add(new ServerQueueItem($"local:{entry.Id}", Loc.T("This computer"), entry.Label, entry.IsRunning, false,
                            entry.Promoted, waiting, entry.Elapsed, !entry.IsRunning && !(entry.Promoted && waiting == 1), local, entry.Id));
                    }
                }

                foreach (RenderNode node in ServerHub.Nodes.ToList())
                {
                    //The local server is shown above, straight from its source
                    if (!node.Connected || node.Name == BlendFarmManager.LocalNodeName)
                        continue;
                    hasSource = true;

                    int waiting = 0;
                    foreach (QueueEntryModel entry in node.QueueEntries.ToList())
                    {
                        if (!entry.IsRunning)
                            waiting++;
                        rows.Add(new ServerQueueItem($"{node.Name}:{entry.Id}", node.Name, entry.Label, entry.IsRunning, entry.IsYou,
                            entry.Promoted, waiting, TimeSpan.FromSeconds(entry.ElapsedSeconds), false, null, entry.Id));
                    }

                    if (node.QueuePosition == 0)
                        summary.Add(Loc.T($"Your job is rendering on {node.Name}"));
                    else if (node.QueuePosition > 0)
                        summary.Add(Loc.T($"You are #{node.QueuePosition} in line on {node.Name}"));
                }

                foreach (ServerQueueItem gone in ServerQueue.Where(x => !rows.Any(r => r.Key == x.Key)).ToList())
                    ServerQueue.Remove(gone);
                for (int i = 0; i < rows.Count; i++)
                {
                    ServerQueueItem existing = ServerQueue.FirstOrDefault(x => x.Key == rows[i].Key);
                    if (existing == null)
                        ServerQueue.Insert(i, rows[i]);
                    else
                    {
                        existing.CopyFrom(rows[i]);
                        int index = ServerQueue.IndexOf(existing);
                        if (index != i)
                            ServerQueue.Move(index, i);
                    }
                }

                string text = summary.Count > 0 ? string.Join("\n", summary)
                    : (!hasSource ? Loc.T("Not connected to any server.") : (rows.Count == 0 ? Loc.T("No one is waiting.") : ""));
                if (_summary.Text != text)
                    _summary.Text = text;

                return Loc.T($"Current Queue ({rows.Count})") + (summary.Count > 0 ? " · " + summary[0] : "");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to refresh queue view: " + ex.Message);
                return Loc.T("Current Queue");
            }
        }
    }
}
