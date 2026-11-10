using Avalonia.Media;
using LogicReinc.BlendFarm.Server;
using LogicReinc.BlendFarm.Shared;
using System;
using System.ComponentModel;

namespace LogicReinc.BlendFarm.Objects
{
    /// <summary>
    /// One line in the server queue view: who is rendering, or who is waiting and for how long
    /// </summary>
    public class ServerQueueItem : INotifyPropertyChanged
    {
        private readonly RenderQueue _queue;
        private readonly int _id;

        /// <summary>
        /// Identifies the same entry between refreshes
        /// </summary>
        public string Key { get; }

        public string Title { get; private set; }
        public string Detail { get; private set; }
        public IBrush TitleBrush { get; private set; }
        /// <summary>
        /// Only set for the queue of this computer's own server, the only one that can be rearranged
        /// </summary>
        public bool CanPromote { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ServerQueueItem(string key, string source, string label, bool isRunning, bool isYou, bool promoted,
            int waitingPosition, TimeSpan elapsed, bool canPromote, RenderQueue queue, int id)
        {
            Key = key;
            _queue = queue;
            _id = id;
            CanPromote = canPromote;

            string state = isRunning ? Loc.T("Rendering now") : Loc.T($"Waiting #{waitingPosition}");
            Title = $"[{source}] {state} · {label}" + (isYou ? " " + Loc.T("(you)") : "");

            string time = FormatDuration(elapsed);
            Detail = isRunning ? Loc.T($"{time} elapsed") : Loc.T($"{time} waiting");
            if (promoted && !isRunning)
                Detail += " · " + Loc.T("Moved to front");

            TitleBrush = Brush.Parse(isRunning ? "#00CC66" : (isYou ? "#FFCC00" : "#CCCCCC"));
        }

        /// <summary>
        /// Takes over the values of a newer snapshot of the same entry
        /// </summary>
        public void CopyFrom(ServerQueueItem other)
        {
            Title = other.Title;
            Detail = other.Detail;
            TitleBrush = other.TitleBrush;
            CanPromote = other.CanPromote;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Detail)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleBrush)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanPromote)));
        }

        /// <summary>
        /// Moves the job to the front of this computer's queue
        /// </summary>
        public void MoveToFront()
        {
            if (CanPromote)
                _queue?.Promote(_id);
        }

        private static string FormatDuration(TimeSpan time)
        {
            if (time < TimeSpan.Zero)
                time = TimeSpan.Zero;
            return time.TotalHours >= 1
                ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
                : $"{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}
