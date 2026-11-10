using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LogicReinc.BlendFarm.Server
{
    /// <summary>
    /// Thrown to a waiting request when its client cancelled or disconnected before its turn.
    /// </summary>
    public class RenderQueueCancelledException : Exception
    {
        public RenderQueueCancelledException() : base("Cancelled while waiting in the render queue") { }
    }

    /// <summary>
    /// Server-wide FIFO queue shared by every connected client, so only one render runs at a time
    /// and the others wait their turn instead of being rejected.
    ///
    /// Ordering is per owner (the client's project session) rather than per request: once an owner has
    /// been served it keeps a short lease, so the next frames of the same animation run back to back
    /// instead of interleaving with other people's jobs (which would make Blender reload the file
    /// every frame). The lease lapses if the owner sends nothing for the grace period.
    /// </summary>
    public class RenderQueue
    {
        private const int POLL_MS = 1000;

        private readonly object _lock = new object();
        private readonly List<Entry> _waiting = new List<Entry>();
        private readonly TimeSpan _leaseGrace;

        private int _nextId = 0;
        private Entry _running = null;

        private string _leaseOwner = null;
        private object _leaseConnection = null;
        private string _leaseLabel = null;
        private DateTime _leaseExpiry = DateTime.MinValue;

        public RenderQueue(TimeSpan leaseGrace)
        {
            _leaseGrace = leaseGrace;
        }

        /// <summary>
        /// Blocks until it is this request's turn. Dispose the returned slot when the render is done.
        /// </summary>
        /// <param name="owner">Identifies the job, requests with the same owner are kept together</param>
        /// <param name="connection">Connection the request came in on, used to cancel on its behalf</param>
        /// <param name="label">Human readable name of the requester for status messages</param>
        /// <param name="notify">Called (outside the lock) whenever the status message for the waiter changes</param>
        /// <exception cref="RenderQueueCancelledException"></exception>
        public Slot Enter(string owner, object connection, string label, Action<string> notify)
        {
            Entry entry = new Entry()
            {
                Id = Interlocked.Increment(ref _nextId),
                EnqueuedAt = DateTime.UtcNow,
                Owner = owner ?? "",
                Connection = connection,
                Label = label
            };
            string lastMessage = null;
            bool waited = false;

            lock (_lock)
            {
                _waiting.Add(entry);
                Console.WriteLine($"[Queue] {label} queued ({_waiting.Count - 1 + (_running != null ? 1 : 0)} ahead)");
            }

            while (true)
            {
                string message = null;
                lock (_lock)
                {
                    if (entry.Cancelled)
                        throw new RenderQueueCancelledException();

                    if (CanRun(entry))
                    {
                        _waiting.Remove(entry);
                        entry.StartedAt = DateTime.UtcNow;
                        _running = entry;
                        _leaseOwner = entry.Owner;
                        _leaseConnection = connection;
                        _leaseLabel = label;
                        Console.WriteLine($"[Queue] {label} started");
                        return new Slot(this, entry) { Waited = waited };
                    }

                    waited = true;
                    message = Describe(entry);
                    if (message == lastMessage)
                    {
                        message = null;
                        //Wakes on any change, or periodically to notice an expired lease
                        Monitor.Wait(_lock, POLL_MS);
                    }
                    else
                        lastMessage = message;
                }

                if (message != null)
                {
                    try { notify?.Invoke(message); }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Drops everything the connection has waiting, and stops its render if it is the one running.
        /// </summary>
        /// <param name="releaseLease">True on an explicit user cancel (the job is over), false on a disconnect
        /// where the client may still recover and continue</param>
        /// <param name="cancelRunning">Stops the render that is currently running</param>
        public void Cancel(object connection, bool releaseLease, Action cancelRunning)
        {
            lock (_lock)
            {
                foreach (Entry entry in _waiting.Where(x => x.Connection == connection).ToList())
                {
                    entry.Cancelled = true;
                    _waiting.Remove(entry);
                }

                bool isRunning = _running != null && _running.Connection == connection;
                if (isRunning)
                    cancelRunning?.Invoke();

                if (releaseLease && (isRunning || _leaseConnection == connection))
                    _leaseOwner = null;

                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// What is rendering and who is waiting, in the order they will be served
        /// </summary>
        public List<RenderQueueEntryInfo> GetSnapshot(object viewer = null)
        {
            lock (_lock)
            {
                DateTime now = DateTime.UtcNow;
                List<RenderQueueEntryInfo> list = new List<RenderQueueEntryInfo>();
                if (_running != null)
                    list.Add(new RenderQueueEntryInfo()
                    {
                        Id = _running.Id,
                        Label = _running.Label,
                        IsRunning = true,
                        IsYou = viewer != null && _running.Connection == viewer,
                        Elapsed = now - _running.StartedAt
                    });
                foreach (Entry entry in _waiting)
                    list.Add(new RenderQueueEntryInfo()
                    {
                        Id = entry.Id,
                        Label = entry.Label,
                        IsRunning = false,
                        IsYou = viewer != null && entry.Connection == viewer,
                        Promoted = entry.Promoted,
                        Elapsed = now - entry.EnqueuedAt
                    });
                return list;
            }
        }

        /// <summary>
        /// Moves a waiting job (all of its waiting requests) to the front of the line.
        /// It runs as soon as the current render finishes, ahead of any lease held by someone else.
        /// Only reachable from inside this process, no client can call it over the network.
        /// </summary>
        /// <returns>False if the request is no longer waiting</returns>
        public bool Promote(int id)
        {
            lock (_lock)
            {
                Entry target = _waiting.FirstOrDefault(x => x.Id == id);
                if (target == null)
                    return false;

                List<Entry> job = _waiting.Where(x => x.Owner == target.Owner).ToList();
                foreach (Entry entry in job)
                {
                    _waiting.Remove(entry);
                    entry.Promoted = true;
                }
                _waiting.InsertRange(0, job);

                if (_leaseOwner != null && _leaseOwner != target.Owner)
                    _leaseOwner = null;

                Console.WriteLine($"[Queue] {target.Label} moved to the front");
                Monitor.PulseAll(_lock);
                return true;
            }
        }

        private void Release(Entry entry)
        {
            lock (_lock)
            {
                if (_running != entry)
                    return;
                _running = null;
                _leaseExpiry = DateTime.UtcNow + _leaseGrace;
                Console.WriteLine($"[Queue] {entry.Label} finished ({_waiting.Count} waiting)");
                Monitor.PulseAll(_lock);
            }
        }

        //Must be called while holding the lock
        private bool CanRun(Entry entry)
        {
            if (_running != null)
                return false;

            if (_leaseOwner != null && DateTime.UtcNow >= _leaseExpiry)
                _leaseOwner = null;

            //The owner served last gets first pick, to keep an animation together
            if (_leaseOwner != null)
                return entry.Owner == _leaseOwner && _waiting.First(x => x.Owner == _leaseOwner) == entry;

            return _waiting.Count > 0 && _waiting[0] == entry;
        }

        //Must be called while holding the lock
        private string Describe(Entry entry)
        {
            int ahead = _waiting.IndexOf(entry) + ((_running != null || _leaseOwner != null) ? 1 : 0);
            string current = _running?.Label ?? _leaseLabel ?? "?";
            //Translated by the receiving client
            return $"Queued #{ahead + 1} (ahead {ahead}, now rendering: {current})";
        }

        internal class Entry
        {
            public int Id;
            public DateTime EnqueuedAt;
            public DateTime StartedAt;
            public bool Promoted;
            public string Owner;
            public object Connection;
            public string Label;
            public bool Cancelled;
        }

        public class RenderQueueEntryInfo
        {
            public int Id { get; set; }
            public string Label { get; set; }
            public bool IsRunning { get; set; }
            /// <summary>
            /// If the entry belongs to the connection the snapshot was requested for
            /// </summary>
            public bool IsYou { get; set; }
            public bool Promoted { get; set; }
            /// <summary>
            /// Time spent rendering for a running entry, time spent waiting otherwise
            /// </summary>
            public TimeSpan Elapsed { get; set; }
        }

        public sealed class Slot : IDisposable
        {
            private readonly RenderQueue _queue;
            private readonly Entry _entry;
            private bool _disposed = false;

            /// <summary>
            /// If the request had to wait for its turn
            /// </summary>
            public bool Waited { get; internal set; }

            internal Slot(RenderQueue queue, Entry entry)
            {
                _queue = queue;
                _entry = entry;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                _queue.Release(_entry);
            }
        }
    }
}
