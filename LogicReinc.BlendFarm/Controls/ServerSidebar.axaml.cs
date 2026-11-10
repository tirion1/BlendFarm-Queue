using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LogicReinc.BlendFarm.Shared;
using System;
using System.Linq;

namespace LogicReinc.BlendFarm.Controls
{
    /// <summary>
    /// The node selection and the queue next to the project loader / render window. Both are collapsed until
    /// opened, the headers keep showing what matters (how many nodes are connected, who is in line).
    /// </summary>
    public class ServerSidebar : UserControl
    {
        private TextBlock _nodesHeader = null;
        private TextBlock _queueHeader = null;
        private QueuePanel _queue = null;
        private DispatcherTimer _timer = null;

        public ServerSidebar()
        {
            AvaloniaXamlLoader.Load(this);

            _nodesHeader = this.Find<TextBlock>("nodesHeader");
            _queueHeader = this.Find<TextBlock>("queueHeader");
            _queue = this.Find<QueuePanel>("queuePanel");

            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (a, b) => Refresh());
            AttachedToVisualTree += (a, b) => { _timer.Start(); Refresh(); };
            DetachedFromVisualTree += (a, b) => _timer.Stop();
        }

        private void Refresh()
        {
            var nodes = ServerHub.Nodes.ToList();
            string nodesText = Loc.T($"Select Nodes ({nodes.Count(x => x.Connected)}/{nodes.Count} connected)");
            if (_nodesHeader.Text != nodesText)
                _nodesHeader.Text = nodesText;

            string queueText = _queue.Refresh();
            if (_queueHeader.Text != queueText)
                _queueHeader.Text = queueText;
        }
    }
}
