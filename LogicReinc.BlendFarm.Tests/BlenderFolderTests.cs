using LogicReinc.BlendFarm.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace LogicReinc.BlendFarm.Tests
{
    /// <summary>
    /// Blender folders that were extracted by hand: one folder too deep, or without Blender in them
    /// </summary>
    [TestClass]
    public class BlenderFolderTests
    {
        private const string VERSION = "blender-9.9.9";

        private static string CreateData(out BlenderManager manager)
        {
            string data = Path.Combine(Path.GetTempPath(), "bf-blender-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(data);
            manager = new BlenderManager(data, Path.Combine(data, "RenderData"));
            return data;
        }

        private static string ExecutableFor(string data) => BlenderManager.GetVersionExecutablePath(data, VERSION);

        private static bool Applies => SystemInfo.GetOSName() == SystemInfo.OS_WINDOWS64 || SystemInfo.GetOSName() == SystemInfo.OS_LINUX64;

        [TestMethod]
        public void VersionExtractedOneFolderTooDeepIsMovedUp()
        {
            if (!Applies)
                return;

            string data = CreateData(out BlenderManager manager);
            try
            {
                string versionDir = BlenderManager.GetVersionPath(data, VERSION, SystemInfo.GetOSName());
                string nested = Path.Combine(versionDir, VERSION + "-windows-x64");
                Directory.CreateDirectory(Path.Combine(nested, "4.4"));
                File.WriteAllText(Path.Combine(nested, Path.GetFileName(ExecutableFor(data))), "exe");
                File.WriteAllText(Path.Combine(nested, "4.4", "data.txt"), "data");

                Assert.IsTrue(manager.IsVersionAvailable(VERSION), "usable after the fix");
                Assert.IsTrue(File.Exists(ExecutableFor(data)), "executable is directly in the version folder");
                Assert.IsTrue(File.Exists(Path.Combine(versionDir, "4.4", "data.txt")), "nothing is lost on the way");
                Assert.IsFalse(Directory.Exists(nested), "the empty extra folder is gone");
            }
            finally
            {
                Directory.Delete(data, true);
            }
        }

        [TestMethod]
        public void FolderWithoutBlenderIsNotAVersionAndSaysWhy()
        {
            if (!Applies)
                return;

            string data = CreateData(out BlenderManager manager);
            try
            {
                Directory.CreateDirectory(BlenderManager.GetVersionPath(data, VERSION, SystemInfo.GetOSName()));

                Assert.IsFalse(manager.IsVersionAvailable(VERSION));
                Assert.IsFalse(manager.TryPrepare(VERSION, out string error));
                StringAssert.Contains(error, "no Blender executable");
            }
            finally
            {
                Directory.Delete(data, true);
            }
        }

        [TestMethod]
        public void NormalInstallIsLeftAlone()
        {
            if (!Applies)
                return;

            string data = CreateData(out BlenderManager manager);
            try
            {
                string versionDir = BlenderManager.GetVersionPath(data, VERSION, SystemInfo.GetOSName());
                Directory.CreateDirectory(Path.Combine(versionDir, "4.4"));
                File.WriteAllText(ExecutableFor(data), "exe");

                Assert.IsTrue(manager.IsVersionAvailable(VERSION));
                Assert.IsTrue(manager.TryPrepare(VERSION, out string error), error);
                Assert.IsTrue(Directory.Exists(Path.Combine(versionDir, "4.4")), "a real subfolder is not touched");
            }
            finally
            {
                Directory.Delete(data, true);
            }
        }
    }
}
