using LogicReinc.BlendFarm.Client;
using LogicReinc.BlendFarm.Server;
using LogicReinc.BlendFarm.Shared;
using LogicReinc.BlendFarm.Shared.Communication.RenderNode;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LogicReinc.BlendFarm.Tests
{
    /// <summary>
    /// Blender version lists: installed versions are found on disk and shared with clients, no Blender required
    /// </summary>
    [TestClass]
    public class VersionListTests
    {
        private const int PORT = 18998;

        [TestMethod]
        public void VersionsAreOrderedByNumberNotByText()
        {
            string[] names = { "blender-4.5.4", "blender-4.5.10", "blender-5.0.0", "blender-4.2.9", "blender-4.5.1" };
            var sorted = names.OrderByDescending(x => x, Comparer<string>.Create(BlenderVersion.CompareNames)).ToList();
            CollectionAssert.AreEqual(new[] { "blender-5.0.0", "blender-4.5.10", "blender-4.5.4", "blender-4.5.1", "blender-4.2.9" }, sorted);
        }

        [TestMethod]
        public void InstalledVersionsAreFoundInBlenderData()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bf-versions-" + Guid.NewGuid().ToString("N"));
            try
            {
                foreach (string name in new[] { "blender-4.5.10-windows64", "blender-5.0.0-windows-x64", "blender-4.2.1-linux64", "not-a-version", "render.py" })
                    Directory.CreateDirectory(Path.Combine(dir, name));

                var found = BlenderVersion.GetInstalledVersions(dir);
                CollectionAssert.AreEqual(new[] { "blender-5.0.0", "blender-4.5.10", "blender-4.2.1" }, found);
                Assert.AreEqual(0, BlenderVersion.GetInstalledVersions(Path.Combine(dir, "missing")).Count);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [TestMethod]
        public async Task ConnectedNodeCanListTheVersionsOfItsServer()
        {
            string blenderData = SystemInfo.RelativeToApplicationDirectory(ServerSettings.Instance.BlenderData);
            string marker = Path.Combine(blenderData, "blender-9.9.9-windows64");
            bool created = !Directory.Exists(marker);
            Directory.CreateDirectory(marker);

            RenderServer server = new RenderServer(PORT, -1, true);
            server.Start();
            RenderNode node = new RenderNode() { Name = "test", Address = $"127.0.0.1:{PORT}" };
            try
            {
                await node.Connect();
                BlenderVersionsResponse result = await node.GetBlenderVersions();
                Assert.IsNotNull(result, "the server should answer");
                CollectionAssert.Contains(result.Installed, "blender-9.9.9", "versions installed on the server are reported");
            }
            finally
            {
                node.Disconnect();
                server.Stop();
                if (created)
                    Directory.Delete(marker, true);
            }
        }
    }
}
