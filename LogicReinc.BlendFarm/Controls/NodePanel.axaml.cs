using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LogicReinc.BlendFarm.Client;
using LogicReinc.BlendFarm.Windows;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogicReinc.BlendFarm.Controls
{
    /// <summary>
    /// Selection of the servers to render with. Shown next to the project loader and next to the render window,
    /// both work on the same nodes (see ServerHub).
    /// </summary>
    public class NodePanel : UserControl
    {
        public ObservableCollection<RenderNode> Nodes => ServerHub.Nodes;

        public string InputClientName { get; set; }
        public string InputClientAddress { get; set; }

        private Button _syncAll = null;

        public NodePanel()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _syncAll = this.Find<Button>("syncAllButton");
            UpdateSyncAvailable();
            ServerHub.ManagerChanged += () => Dispatcher.UIThread.InvokeAsync(UpdateSyncAvailable);
        }

        //Syncing needs an open project
        private void UpdateSyncAvailable()
        {
            if (_syncAll != null)
                _syncAll.IsVisible = ServerHub.SyncAll != null;
        }

        private Window Owner => this.VisualRoot as Window;

        public async void ConnectNode(RenderNode node)
        {
            await ServerHub.ConnectNode(node);
        }

        public void DisconnectNode(RenderNode node)
        {
            ServerHub.DisconnectNode(node);
        }

        public async void ConnectAll()
        {
            await ServerHub.ConnectAll();
        }

        public async void SyncAll()
        {
            if (ServerHub.SyncAll != null)
                await ServerHub.SyncAll();
        }

        public void AddNewNode()
        {
            if (string.IsNullOrEmpty(InputClientAddress) || string.IsNullOrEmpty(InputClientName))
            {
                MessageWindow.Show(Owner, "No name or address", "A node requires both a name and an address");
                return;
            }
            if (BlendFarmSettings.Instance.PastClients.ContainsKey(InputClientName)
                || ServerHub.Nodes.Any(x => x.Name == InputClientName || x.Address == InputClientAddress))
            {
                MessageWindow.Show(Owner, "Node already exists", "Node already exists, use a different name and address");
                return;
            }
            if (!Regex.IsMatch(InputClientAddress, "^([a-zA-Z0-9\\.]*?):[0-9][0-9]?[0-9]?[0-9]?[0-9]?$"))
            {
                MessageWindow.Show(Owner, "Invalid Address", "The address provided seems to be invalid, expected format is {hostname}:{port} or {ip}{port}, eg. 192.168.1.123:15000");
                return;
            }

            ServerHub.AddNode(InputClientName, InputClientAddress);
        }

        public void DeleteNode(RenderNode node)
        {
            ServerHub.RemoveNode(node);
        }

        public void TerminalNode(RenderNode node)
        {
            DeviceLogWindow.Show(Owner, node);
        }

        public void ConfigureNode(RenderNode node)
        {
            DeviceSettingsWindow.Show(Owner, node);
        }
    }
}
