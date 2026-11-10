using System;
using System.Collections.Generic;
using System.Text;

namespace LogicReinc.BlendFarm.Shared.Communication.RenderNode
{
    /// <summary>
    /// Pushed by the server to every connected client whenever the render queue changes.
    /// Read only: clients can look at the queue but have no way to change it.
    /// </summary>
    [BlendFarmHeader("queueStatusResp")]
    public class QueueStatusResponse : BlendFarmMessage
    {
        /// <summary>
        /// What is rendering followed by who is waiting, in the order they will be served
        /// </summary>
        public List<QueueEntryModel> Entries { get; set; } = new List<QueueEntryModel>();

        /// <summary>
        /// Where the receiving client is: -1 not in the queue, 0 rendering, 1 and up waiting (1 is next)
        /// </summary>
        public int YourPosition { get; set; } = -1;
    }

    public class QueueEntryModel
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public bool IsRunning { get; set; }
        public bool IsYou { get; set; }
        public bool Promoted { get; set; }
        public int ElapsedSeconds { get; set; }
    }
}
