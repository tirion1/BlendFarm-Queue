using LogicReinc.BlendFarm.Client;
using LogicReinc.BlendFarm.Server;
using LogicReinc.BlendFarm.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogicReinc.BlendFarm.Tests
{
    /// <summary>
    /// What clients get to see of the render queue, and the optional translation. No Blender required.
    /// </summary>
    [TestClass]
    public class QueueViewTests
    {
        private const int PORT = 18999;

        [TestMethod]
        public void SnapshotMarksOnlyTheViewersOwnEntries()
        {
            RenderQueue queue = new RenderQueue(TimeSpan.Zero);
            object connectionA = new object();
            object connectionB = new object();
            RenderQueue.Slot running = queue.Enter("a", connectionA, "a", null);

            ManualResetEventSlim waiting = new ManualResetEventSlim();
            Task<RenderQueue.Slot> b = Task.Run(() => queue.Enter("b", connectionB, "b", (m) => waiting.Set()));
            Assert.IsTrue(waiting.Wait(8000));

            var forB = queue.GetSnapshot(connectionB);
            Assert.IsFalse(forB[0].IsYou);
            Assert.IsTrue(forB[1].IsYou);
            Assert.IsTrue(queue.GetSnapshot(connectionA)[0].IsYou);
            Assert.IsFalse(queue.GetSnapshot()[1].IsYou, "nobody when no viewer is given");

            running.Dispose();
            Assert.IsTrue(b.Wait(8000));
            b.Result.Dispose();
        }

        [TestMethod]
        public async Task ConnectedClientsReceiveTheQueueOverTheNetwork()
        {
            RenderServer server = new RenderServer(PORT, -1, true);
            server.Start();
            RenderQueue.Slot running = server.Blender.Queue.Enter("someone", new object(), "someone-else (10.0.0.9)", null);
            RenderNode node = new RenderNode() { Name = "test", Address = $"127.0.0.1:{PORT}" };
            try
            {
                await node.Connect();
                Assert.IsTrue(node.Connected);

                DateTime end = DateTime.UtcNow.AddSeconds(10);
                while (node.QueueEntries.Count == 0 && DateTime.UtcNow < end)
                    await Task.Delay(100);

                Assert.AreEqual(1, node.QueueEntries.Count, "the queue state should be pushed to idle clients too");
                Assert.IsTrue(node.QueueEntries[0].IsRunning);
                Assert.IsFalse(node.QueueEntries[0].IsYou);
                Assert.AreEqual("someone-else (10.0.0.9)", node.QueueEntries[0].Label);
                Assert.AreEqual(-1, node.QueuePosition, "this client is not in the queue");

                //Empty again once the render is done
                running.Dispose();
                running = null;
                end = DateTime.UtcNow.AddSeconds(10);
                while (node.QueueEntries.Count != 0 && DateTime.UtcNow < end)
                    await Task.Delay(100);
                Assert.AreEqual(0, node.QueueEntries.Count);
            }
            finally
            {
                running?.Dispose();
                node.Disconnect();
                server.Stop();
            }
        }

        [TestMethod]
        public void TranslationIsOptionalAndLeavesUnknownTextAlone()
        {
            string previous = Loc.Language;
            try
            {
                Loc.Language = Loc.English;
                Assert.AreEqual("Connect All", Loc.T("Connect All"));
                Assert.AreEqual("Queued #2 (ahead 1, now rendering: PC)", Loc.T("Queued #2 (ahead 1, now rendering: PC)"));

                Loc.Language = Loc.Chinese;
                Assert.AreEqual("全部连接", Loc.T("Connect All"));
                Assert.AreEqual("全部连接", Loc.T("  Connect   All \n"), "whitespace in xaml must not matter");
                Assert.AreEqual("排队中：第 2 位（前面还有 1 个，正在渲染：PC-C (10.0.0.3)）", Loc.T("Queued #2 (ahead 1, now rendering: PC-C (10.0.0.3))"));
                Assert.AreEqual("渲染中 (3/10)", Loc.T("Rendering (3/10)"));
                Assert.AreEqual("读取文件信息失败：没有已准备并已同步的节点（Blender 可能仍在安装中）",
                    Loc.T("Failed peek due to:No prepared and synced node\n(Blender might still be installing)"), "known error texts inside a message are translated too");
                Assert.AreEqual("读取文件信息失败：Some unknown error", Loc.T("Failed peek due to:Some unknown error"), "unknown ones are left as they are");
                Assert.AreEqual("协议版本不一致，请先把该节点上的程序更新到同一版本（本机协议：8，对方：7）",
                    Loc.T("Outdated protocol, update node before connecting (Protocol: 8, Found: 7)"));
                Assert.AreEqual("节点选择（已连接 1/5）", Loc.T("Select Nodes (1/5 connected)"));
                Assert.AreEqual("排队中：第 2 位（前面还有 1 个，正在渲染：Test (10.0.0.3)）", Loc.T("Queued #2 (ahead 1, now rendering: Test (10.0.0.3))"), "names inside a message are not translated");
                Assert.AreEqual("我的项目.blend", Loc.T("我的项目.blend"));
                Assert.AreEqual("scene.blend", Loc.T("scene.blend"), "names and paths are never translated");
                Assert.IsNull(Loc.T(null));
            }
            finally
            {
                Loc.Language = previous;
            }
        }
    }
}
