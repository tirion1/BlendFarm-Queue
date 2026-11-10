using Avalonia.Threading;
using LogicReinc.BlendFarm.Client;
using LogicReinc.BlendFarm.Server;
using LogicReinc.BlendFarm.Shared;
using LogicReinc.BlendFarm.Shared.Communication.RenderNode;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LogicReinc.BlendFarm
{
    /// <summary>
    /// The servers (render nodes) this computer knows of. It exists from the moment the program starts, so the
    /// node selection, the queue and the available Blender versions are there before any project is opened,
    /// and the same connected nodes are handed to the render window once one is.
    /// </summary>
    public static class ServerHub
    {
        /// <summary>
        /// Every known node: the local server, nodes from earlier sessions, discovered and added ones
        /// </summary>
        public static ObservableCollection<RenderNode> Nodes { get; } = new ObservableCollection<RenderNode>();

        /// <summary>
        /// Set once a project is opened
        /// </summary>
        public static BlendFarmManager Manager { get; private set; }

        /// <summary>
        /// Blender version currently selected, used when a node is connected and prepared
        /// </summary>
        public static string SelectedVersion { get; set; }

        /// <summary>
        /// Syncs the open project to the nodes, only available once a project is open
        /// </summary>
        public static Func<Task> SyncAll { get; set; }

        /// <summary>
        /// Blender versions the connected servers reported as installed
        /// </summary>
        public static List<string> RemoteVersions
        {
            get
            {
                lock (_versions)
                    return _versions.ToList();
            }
        }

        public static event Action VersionsChanged;
        public static event Action ManagerChanged;

        private static readonly HashSet<string> _versions = new HashSet<string>();
        private static bool _started = false;

        /// <summary>
        /// Loads the known nodes and connects to them. Call once, on the UI thread, after the local server started.
        /// </summary>
        public static void Start()
        {
            if (_started)
                return;
            _started = true;

            var past = BlendFarmSettings.Instance.PastClients;
            if (LocalServer.Available && !past.ContainsKey(BlendFarmManager.LocalNodeName))
                Add(BlendFarmManager.LocalNodeName, $"localhost:{LocalServer.ServerPort}", RenderType.CPU, ServerSettings.Instance.BasicSecurityPassword);
            foreach (var pair in past.ToList())
                Add(pair.Key, pair.Value.Address, pair.Value.RenderType, pair.Value.Pass);

            //Only the nodes the user picked before, the others are just listed
            foreach (RenderNode node in Nodes.Where(IsRemembered).ToList())
                _ = Connect(node);

            //A picked server that was not up yet (or got restarted) is connected again without anyone having to click
            new DispatcherTimer(TimeSpan.FromSeconds(15), DispatcherPriority.Background, (a, b) =>
            {
                foreach (RenderNode node in Nodes.Where(IsRemembered).ToList())
                    if (!node.Connected && string.IsNullOrEmpty(node.Activity))
                        _ = Connect(node);
            }).Start();
        }

        /// <summary>
        /// Adds a node entered by the user and remembers it for next time
        /// </summary>
        public static RenderNode AddNode(string name, string address)
        {
            RenderNode node = Add(name, address, RenderType.CPU, null);

            BlendFarmSettings.Instance.PastClients[name] = new BlendFarmSettings.HistoryClient()
            {
                Address = address,
                Name = name,
                RenderType = RenderType.CPU
            };
            BlendFarmSettings.Instance.Save();

            _ = Connect(node);
            return node;
        }

        /// <summary>
        /// Adds a server found through the network broadcast, unless it is already known
        /// </summary>
        public static void AddDiscovered(string name, string address, int port)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                string addressPort = $"{address}:{port}";
                if (Nodes.Any(x => x.Address == addressPort))
                    return;

                string unique = name;
                for (int i = 2; Nodes.Any(x => string.Equals(x.Name, unique, StringComparison.OrdinalIgnoreCase)); i++)
                    unique = $"{name} ({i})";

                //Listed only, connecting is up to the user
                Add(unique, addressPort, RenderType.CPU, null);
            });
        }

        /// <summary>
        /// Connects a node picked by the user (and prepares the Blender version once a project is open)
        /// </summary>
        public static async Task ConnectNode(RenderNode node)
        {
            try
            {
                if (Manager != null)
                    await node.ConnectAndPrepare(Manager.Version);
                else
                    await node.Connect();
            }
            catch { }
        }

        /// <summary>
        /// Disconnects a node on request of the user, it is not connected again on the next start
        /// </summary>
        public static void DisconnectNode(RenderNode node)
        {
            Remember(node, false);
            node.Disconnect();
        }

        public static void RemoveNode(RenderNode node)
        {
            Nodes.Remove(node);
            if (Manager != null)
                Manager.RemoveNode(node.Name);
            else
                node.Disconnect();

            if (BlendFarmSettings.Instance.PastClients.Remove(node.Name))
                BlendFarmSettings.Instance.Save();
        }

        /// <summary>
        /// Hands the nodes (with their connections) to the manager of the project that was opened
        /// </summary>
        public static void SetManager(BlendFarmManager manager)
        {
            Manager = manager;
            foreach (RenderNode node in Nodes.ToList())
                if (manager.GetNodeByName(node.Name) == null)
                    manager.AddNode(node);
            ManagerChanged?.Invoke();

            //Connected before the project was opened, so the version still has to be prepared
            _ = PrepareConnected(manager);
        }

        private static async Task PrepareConnected(BlendFarmManager manager)
        {
            await Task.WhenAll(Nodes.ToList().Where(x => x.Connected).Select(async node =>
            {
                try { await node.PrepareVersion(manager.Version); }
                catch (Exception ex)
                {
                    node.LastStatus = $"Version Failed: {ex.Message}";
                    node.UpdateException("Version Failure");
                }
            }));
        }

        public static async Task ConnectAll()
        {
            if (Manager != null)
            {
                try { await Manager.ConnectAndPrepareAll(); }
                catch { }
            }
            else
                await Task.WhenAll(Nodes.ToList().Select(Connect));
        }

        private static RenderNode Add(string name, string address, RenderType type, string pass)
        {
            RenderNode node = new RenderNode() { Name = name, Address = address, RenderType = type, Pass = pass };
            node.OnConnected += OnNodeConnected;

            if (name == BlendFarmManager.LocalNodeName)
                Nodes.Insert(0, node);
            else
                Nodes.Add(node);

            if (Manager != null && Manager.GetNodeByName(node.Name) == null)
                Manager.AddNode(node);
            return node;
        }

        private static void OnNodeConnected(RenderNode node)
        {
            Remember(node, true);
            FetchVersions(node);
        }

        private static bool IsRemembered(RenderNode node)
        {
            lock (BlendFarmSettings.Instance)
                return BlendFarmSettings.Instance.PastClients.TryGetValue(node.Name, out var entry) && entry.AutoConnect;
        }

        private static void Remember(RenderNode node, bool connect)
        {
            lock (BlendFarmSettings.Instance)
            {
                var clients = BlendFarmSettings.Instance.PastClients;
                if (!clients.TryGetValue(node.Name, out var entry))
                {
                    if (!connect)
                        return;
                    entry = new BlendFarmSettings.HistoryClient()
                    {
                        Name = node.Name,
                        Address = node.Address,
                        RenderType = node.RenderType,
                        Performance = node.Performance,
                        Pass = node.Pass
                    };
                    clients[node.Name] = entry;
                }
                if (entry.AutoConnect == connect)
                    return;
                entry.AutoConnect = connect;
                BlendFarmSettings.Instance.Save();
            }
        }

        //Failures are shown on the node itself
        private static async Task Connect(RenderNode node)
        {
            try { await node.Connect(); }
            catch { }
        }

        private static async void FetchVersions(RenderNode node)
        {
            try
            {
                BlenderVersionsResponse resp = await node.GetBlenderVersions();
                if (resp == null)
                    return;

                //Only what is installed: picking those never makes a server download anything
                List<string> names = (resp.Installed ?? new List<string>()).ToList();
                lock (_versions)
                    foreach (string name in names)
                        _versions.Add(name);

                //Remembered, so the versions are still there next time when the server is not
                lock (BlendFarmSettings.Instance)
                {
                    BlendFarmSettings.Instance.RemoteVersions = (BlendFarmSettings.Instance.RemoteVersions ?? new List<string>()).Union(names).ToList();
                    BlendFarmSettings.Instance.Save();
                }
                Dispatcher.UIThread.InvokeAsync(() => VersionsChanged?.Invoke());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not get versions from {node.Name} due to: {ex.Message}");
            }
        }
    }
}
