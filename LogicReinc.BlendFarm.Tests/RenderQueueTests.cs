using LogicReinc.BlendFarm.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogicReinc.BlendFarm.Tests
{
    /// <summary>
    /// Tests for the server-side render queue, no Blender required
    /// </summary>
    [TestClass]
    public class RenderQueueTests
    {
        private const int WAIT_MS = 8000;

        /// <summary>
        /// Enters the queue on a background thread, signals once it is actually waiting
        /// </summary>
        private static Task<RenderQueue.Slot> EnterAsync(RenderQueue queue, string owner, object connection, ManualResetEventSlim waiting)
        {
            return Task.Run(() => queue.Enter(owner, connection, owner, (message) => waiting?.Set()));
        }

        [TestMethod]
        public void DifferentOwnersRunInArrivalOrder()
        {
            RenderQueue queue = new RenderQueue(TimeSpan.Zero);
            RenderQueue.Slot first = queue.Enter("a", new object(), "a", null);

            ManualResetEventSlim bWaiting = new ManualResetEventSlim();
            ManualResetEventSlim cWaiting = new ManualResetEventSlim();
            Task<RenderQueue.Slot> b = EnterAsync(queue, "b", new object(), bWaiting);
            Assert.IsTrue(bWaiting.Wait(WAIT_MS), "b should be told it is waiting");
            Task<RenderQueue.Slot> c = EnterAsync(queue, "c", new object(), cWaiting);
            Assert.IsTrue(cWaiting.Wait(WAIT_MS), "c should be told it is waiting");

            Assert.IsFalse(b.Wait(300), "b must wait while a renders");

            first.Dispose();
            Assert.IsTrue(b.Wait(WAIT_MS), "b is next in line");
            Assert.IsFalse(c.Wait(300), "c must wait while b renders");

            b.Result.Dispose();
            Assert.IsTrue(c.Wait(WAIT_MS), "c goes last");
            c.Result.Dispose();
        }

        [TestMethod]
        public void SameOwnerKeepsItsTurnBetweenFrames()
        {
            RenderQueue queue = new RenderQueue(TimeSpan.FromSeconds(30));
            object connectionA = new object();
            RenderQueue.Slot frame1 = queue.Enter("a", connectionA, "a", null);

            ManualResetEventSlim bWaiting = new ManualResetEventSlim();
            Task<RenderQueue.Slot> b = EnterAsync(queue, "b", new object(), bWaiting);
            Assert.IsTrue(bWaiting.Wait(WAIT_MS));

            frame1.Dispose();

            //Next frame of the same animation jumps ahead of b
            Task<RenderQueue.Slot> frame2 = EnterAsync(queue, "a", connectionA, null);
            Assert.IsTrue(frame2.Wait(WAIT_MS), "a's next frame should run right away");
            Assert.IsFalse(b.IsCompleted, "b must keep waiting while a's animation is going");

            frame2.Result.Dispose();
            Assert.IsFalse(b.Wait(300), "b still waits inside a's lease");
        }

        [TestMethod]
        public void LeaseLapsesWhenOwnerGoesQuiet()
        {
            RenderQueue queue = new RenderQueue(TimeSpan.FromMilliseconds(1500));
            RenderQueue.Slot frame = queue.Enter("a", new object(), "a", null);

            ManualResetEventSlim bWaiting = new ManualResetEventSlim();
            Task<RenderQueue.Slot> b = EnterAsync(queue, "b", new object(), bWaiting);
            Assert.IsTrue(bWaiting.Wait(WAIT_MS));

            frame.Dispose();
            Assert.IsFalse(b.Wait(300), "b waits for the lease first");
            Assert.IsTrue(b.Wait(WAIT_MS), "b runs once a has been quiet long enough");
            b.Result.Dispose();
        }

        [TestMethod]
        public void PromoteMovesJobToFrontAndSnapshotShowsOrder()
        {
            RenderQueue queue = new RenderQueue(TimeSpan.FromSeconds(1));
            RenderQueue.Slot running = queue.Enter("a", new object(), "a", null);

            ManualResetEventSlim bWaiting = new ManualResetEventSlim();
            ManualResetEventSlim cWaiting = new ManualResetEventSlim();
            Task<RenderQueue.Slot> b = EnterAsync(queue, "b", new object(), bWaiting);
            Assert.IsTrue(bWaiting.Wait(WAIT_MS));
            Task<RenderQueue.Slot> c = EnterAsync(queue, "c", new object(), cWaiting);
            Assert.IsTrue(cWaiting.Wait(WAIT_MS));

            var snapshot = queue.GetSnapshot();
            Assert.AreEqual(3, snapshot.Count);
            Assert.IsTrue(snapshot[0].IsRunning);
            Assert.AreEqual("b", snapshot[1].Label);
            Assert.AreEqual("c", snapshot[2].Label);

            Assert.IsTrue(queue.Promote(snapshot[2].Id));
            snapshot = queue.GetSnapshot();
            Assert.AreEqual("c", snapshot[1].Label);
            Assert.IsTrue(snapshot[1].Promoted);
            Assert.IsFalse(queue.Promote(-1), "unknown id");

            //a's lease would otherwise hold the line, a promoted job goes right after the current render
            running.Dispose();
            Assert.IsTrue(c.Wait(WAIT_MS), "c was promoted");
            Assert.IsFalse(b.Wait(300), "b is still behind c");
            c.Result.Dispose();
            Assert.IsTrue(b.Wait(WAIT_MS));
            b.Result.Dispose();
        }

        [TestMethod]
        public void CancelOnlyAffectsOwnConnection()
        {
            RenderQueue queue = new RenderQueue(TimeSpan.Zero);
            object connectionA = new object();
            object connectionB = new object();
            RenderQueue.Slot running = queue.Enter("a", connectionA, "a", null);

            ManualResetEventSlim bWaiting = new ManualResetEventSlim();
            Task<RenderQueue.Slot> b = EnterAsync(queue, "b", connectionB, bWaiting);
            Assert.IsTrue(bWaiting.Wait(WAIT_MS));

            //b cancelling must not stop a's render
            bool stoppedRunning = false;
            queue.Cancel(connectionB, true, () => stoppedRunning = true);
            Assert.IsFalse(stoppedRunning, "b's cancel must not kill a's render");

            AggregateException ex = Assert.ThrowsException<AggregateException>(() => b.Wait(WAIT_MS));
            Assert.IsInstanceOfType(ex.InnerException, typeof(RenderQueueCancelledException));

            //a cancelling does stop its own render
            queue.Cancel(connectionA, true, () => stoppedRunning = true);
            Assert.IsTrue(stoppedRunning);
            running.Dispose();
        }
    }
}
