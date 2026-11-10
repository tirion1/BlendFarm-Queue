using System;
using System.Collections.Generic;
using System.Text;

namespace LogicReinc.BlendFarm.Shared.Communication.RenderNode
{
    /// <summary>
    /// Asks a server which versions of Blender it can render with, so a client without
    /// internet access (or without a version list) can offer the right choices.
    /// </summary>
    [BlendFarmHeader("blenderVersions")]
    public class BlenderVersionsRequest : BlendFarmMessage
    {
    }

    [BlendFarmHeader("blenderVersionsResp")]
    public class BlenderVersionsResponse : BlendFarmMessage
    {
        /// <summary>
        /// Versions that are installed on the server and can be used right away
        /// </summary>
        public List<string> Installed { get; set; } = new List<string>();

        /// <summary>
        /// Other versions the server knows of and can download when needed
        /// </summary>
        public List<string> Others { get; set; } = new List<string>();
    }
}
