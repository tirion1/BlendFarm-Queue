using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogicReinc.BlendFarm.Shared
{
    /// <summary>
    /// Optional interface translation. All texts in code and xaml stay English and double as the lookup key,
    /// anything without a translation is shown as is, so nothing breaks when a text is missing here.
    /// </summary>
    public static class Loc
    {
        public const string English = "en";
        public const string Chinese = "zh";

        /// <summary>
        /// Active language, set once at startup (changing it requires a restart)
        /// </summary>
        public static string Language { get; set; } = English;

        public static bool IsChinese => Language == Chinese;

        /// <summary>
        /// Language to use when the user never picked one
        /// </summary>
        public static string DetectDefault()
        {
            return CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? Chinese : English;
        }

        /// <summary>
        /// Translates a text, returns it unchanged when there is no translation
        /// </summary>
        public static string T(string text)
        {
            if (!IsChinese || string.IsNullOrWhiteSpace(text))
                return text;

            string key = Normalize(text);
            if (Exact.TryGetValue(key, out string translated))
                return translated;

            foreach (Pattern pattern in Patterns)
            {
                Match match = pattern.Regex.Match(key);
                if (match.Success)
                    return string.Format(pattern.Format, match.Groups.Cast<Group>().Skip(1)
                        .Select((x, index) => (object)(pattern.Translate[index] ? T(x.Value) : x.Value)).ToArray());
            }
            return text;
        }

        private static string Normalize(string text)
        {
            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        private class Pattern
        {
            public Regex Regex;
            public string Format;
            public bool[] Translate;
        }

        private static readonly Dictionary<string, string> Exact = BuildExact();
        private static readonly List<Pattern> Patterns = BuildPatterns();

        private static Dictionary<string, string> BuildExact()
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] pairs = new[]
            {
                //Project window
                "BlendFarm by LogicReinc", "BlendFarm 队列版",
                "Select your .blend Project", "选择你的 .blend 项目",
                "Select your file", "选择文件",
                "Add Custom Version", "添加自定义版本",
                "Connect Local", "连接本机",
                "Import Settings", "导入设置",
                "Use Asset Sync (Under Construction)", "使用资源同步（开发中）",
                "Recent Projects", "最近的项目",
                "Load Project", "加载项目",
                "Last Announcement", "最新公告",
                "Announcements", "公告",
                "Loading project", "正在加载项目",
                "Starting", "正在启动",
                "Select your Blendfile", "选择 .blend 文件",
                "Select a Blendfile", "选择 .blend 文件",

                //Render window
                "Help!", "帮助",
                "Version:", "版本：",
                "File:", "文件：",
                "Use Queue", "使用任务队列",
                "Render", "渲染",
                "Render Nodes", "渲染节点",
                "Connect All", "全部连接",
                "Sync All", "全部同步",
                "New Node", "新建节点",
                "Name", "名称",
                "Address (eg. 192.168.1.53:1234)", "地址（例如 192.168.1.53:15000）",
                "Connect New", "连接新节点",
                "Use Automatic Performance", "自动分配性能",
                "Use Sync Compression", "同步时压缩",
                "Image", "图像",
                "Width:", "宽度：",
                "Height:", "高度：",
                "Samples", "采样数",
                "Denoiser", "降噪器",
                "Denoiser to use, Inherit means the one selected in Blendfile.", "使用的降噪器，Inherit 表示沿用 .blend 文件里的设置。",
                "Scene", "场景",
                "Camera", "摄像机",
                "Inherit/Main", "沿用文件设置/默认",
                "Last Render Time:", "上次渲染用时：",
                "Workaround", "兼容处理",
                "Workaround for a Blender bug, once fixed, it may cause odd tile order, then disable this", "针对 Blender 某个 bug 的兼容处理；bug 修复后可能导致分块顺序异常，届时请关闭。",
                "Import Scenes", "导入场景",
                "Cancel", "取消",
                "Add to Queue (Replace)", "加入队列（替换）",
                "Add to Queue (New)", "加入队列（新建）",
                "Save Image", "保存图像",
                "Save Current Settings as Default", "将当前设置保存为默认",
                "Test", "测试",
                "Animation", "动画",
                "Render an animation to a directory", "将动画渲染到指定目录",
                "File Format", "文件格式",
                "Frame Start", "起始帧",
                "Frame End", "结束帧",
                "Frame to start at", "从这一帧开始",
                "Frame to end at", "到这一帧结束",
                "Output", "输出格式",
                "File format Blender should render in", "Blender 渲染输出使用的文件格式",
                "Render Animation", "渲染动画",
                "Queue Animation Render (New)", "将动画加入队列（新建）",
                "Live Render", "实时渲染",
                "Live render will re-render the file whenever it changes (with settings from Image tab), it can only be used with a single project open and no queue.", "实时渲染会在文件发生变化时自动重新渲染（使用“图像”页的设置），只能在仅打开一个项目且不使用队列时使用。",
                "Start Live Render", "开始实时渲染",
                "Stop Live Render", "停止实时渲染",
                "System", "系统",
                "Render Strategy", "渲染策略",
                "The strategy to render the image. Splitted is fastest, but less real-time preview. SplitChunked has real-time tiles updating but has some overhead. Chunked is slow, but allows rendernodes to consume work as available.", "图像的渲染策略。Splitted 最快，但没有实时预览；SplitChunked 可实时更新分块，但有额外开销；Chunked 最慢，但允许节点有空就领任务。",
                "Order", "顺序",
                "Order to render tiles, only relevant for Chunked and SplitChunked", "分块的渲染顺序，仅对 Chunked 和 SplitChunked 有效",
                "ChunkSize", "分块大小",
                "Chunksize for Chunked and SplitChunked, may be modified in certain scenarios", "Chunked 和 SplitChunked 使用的分块大小，某些情况下会被自动调整",
                "Engine", "渲染引擎",
                "Which engine to use. Optix will adhere to CUDA settings. Otherwise CPU is used.", "使用的渲染引擎。Optix 遵循 CUDA 设置，否则使用 CPU。",
                "Use Networked Path", "使用网络路径",
                "Use a network location instead of transfering over TCP (Allows for shared resources)", "使用网络共享路径代替 TCP 传输文件（可共享资源）",
                "Windows Path", "Windows 路径",
                "Linux Path", "Linux 路径",
                "MacOS Path", "macOS 路径",
                "Open", "打开",
                "Delete", "删除",
                "Queued", "排队中",

                //Node status (generated in code)
                "Connecting", "连接中",
                "Downloading Version", "正在下载版本",
                "Syncing", "同步中",
                "Render Loading..", "渲染加载中..",
                "Peeking...", "读取文件信息中...",
                "Compressing..", "压缩中..",
                "Failed Version Download", "版本下载失败",
                "Version Failure", "版本错误",

                //Dialog buttons and common texts
                "Ok", "确定",
                "Yes", "是",
                "No", "否",
                "Don't ask again", "不再询问",
                "Save", "保存",
                "Done", "完成",
                "Continue", "继续",

                //Dialogs
                "Frame Save Error", "帧保存失败",
                "Name missing", "缺少名称",
                "No version name was provided", "没有填写版本名称",
                "Name already exists", "名称已存在",
                "This version name already exists", "这个版本名称已存在",
                "Missing Installation", "未找到安装",
                "Node not saved yet", "节点尚未保存",
                "The node was not yet saved, would you like to save it?", "该节点尚未保存，是否保存？",
                "Local Server Failure", "本机服务端启动失败",
                "Local Broadcast Failure", "本机广播失败",
                "OSX Rendering", "macOS 渲染",
                "Rendering using Blender is disabled for OSX due to it not being implemented fully yet. You can however render using other machines in your network. (Local render node will not be available)", "macOS 上的 Blender 渲染尚未完整实现，已被禁用。你仍然可以使用网络里的其他电脑渲染。（本机渲染节点不可用）",
                "Exception retrieving versions", "获取版本信息失败",
                "File not found", "文件不存在",
                "Disclaimer", "免责声明",
                "Asset sync is an work in progress feature. Would you like to use it?", "资源同步功能仍在开发中，是否继续使用？",
                "Failed to prepare Blender version", "准备 Blender 版本失败",
                "Asset sync requires local Blender, but failed to download it", "资源同步需要本机安装 Blender，但下载失败",
                "No Local Node", "没有本机节点",
                "No local server is available..", "本机服务端不可用..",
                "Failed to sync Local Node", "同步本机节点失败",
                "Required synced node for importing but failed..", "导入需要一个已同步的节点，但同步失败..",
                "Invalid Path", "路径无效",
                "Node already exists", "节点已存在",
                "Node already exists, use a different name and address", "节点已存在，请使用不同的名称和地址",
                "Invalid Address", "地址无效",
                "No name or address", "缺少名称或地址",
                "A node requires both a name and an address", "节点需要同时填写名称和地址",
                "No Nodes", "没有节点",
                "Need at least one connected node to import", "导入至少需要一个已连接的节点",
                "No Synced Node", "没有已同步的节点",
                "Require atleast one synced node to import settings, would you like to sync?", "导入设置至少需要一个已同步的节点，是否现在同步？",
                "Failed Peek", "读取文件信息失败",
                "Test failed", "测试失败",
                "Unsynced nodes", "有节点尚未同步",
                "You have nodes that are not yet synced, would you like to sync them to use for rendering?", "有些节点尚未同步，是否同步它们并用于渲染？",
                "Failed Render", "渲染失败",
                "Invalid file format", "文件格式无效",
                "File format should contain a '#' for frame number", "文件格式需要包含 '#' 作为帧号占位符",
                "File name for animation frames contains illegal characters", "动画帧的文件名包含非法字符",
                "GUI Exception", "界面异常",
                "An error occured trying to load animation Bitmap in GUI. (Animation frame should still be saved)", "在界面中加载动画图像时出错。（动画帧应当已正常保存）",
                "Animation Rendered", "动画渲染完成",
                "Exception in Queue", "队列中出现异常",
                "Queue Save", "保存队列渲染结果",
                "Would you like to save this render to a specific path when it finishes?", "渲染完成后，是否将结果保存到指定路径？",
                "Cannot disable Queue", "无法关闭队列",
                "Your queue is not empty, and thus cannot be disabled", "队列中还有任务，无法关闭",

                //Custom Blender wizard
                "Custom Blender Wizard", "自定义 Blender 向导",
                "When using custom versions of Blender you work outside of BlendFarms automated versioning. It requires you to add this custom version of Blender on every render node. Or a \"Failed to prepare version\" error will occur.", "使用自定义 Blender 版本时，将脱离 BlendFarm 的自动版本管理。你需要在每个渲染节点上都添加这个自定义版本，否则会出现“准备版本失败”的错误。",
                "On the BlenderFarm.Server you can add custom versions manually by adding a VersionCustom file.", "在 BlendFarm 服务端上，可以通过添加 VersionCustom 文件来手动添加自定义版本。",
                "VersionCustom has to be located in the same directory as VersionCache, typically this is the app directory.", "VersionCustom 必须和 VersionCache 放在同一目录，通常就是程序所在目录。",
                "Each line being a version name, and installations being in the same location as described in this process.", "每一行是一个版本名称，安装目录与本向导中说明的位置一致。",
                "Enter the name of your custom build.", "请输入你的自定义版本名称。",
                "(Ensure this name is unique and does not match official names such as blender-2.91.0)", "（请确保名称唯一，不要与 blender-2.91.0 这类官方名称相同）",
                "(This name should match on all render nodes)", "（这个名称在所有渲染节点上必须一致）",
                "Your-Blender-Version-Name", "你的Blender版本名称",
                "Copy your Blender installation to the BlenderData directory.", "请把你的 Blender 安装目录复制到 BlenderData 目录下。",
                "The directory name is your version name with your operating system surfix.", "目录名是你的版本名称加上系统后缀。",
                "(Windows: -windows64, Linux: -linux64, Mac: -macOS)", "（Windows：-windows64，Linux：-linux64，Mac：-macOS）",
                "On this computer it should be located at:", "在这台电脑上，它应当位于：",
                "On Linux ensure your user has read, write and execute permissions on the directory/files (chmod u=rwx)", "在 Linux 上请确保当前用户对该目录/文件有读、写、执行权限（chmod u=rwx）",
                "Check Installation", "检查安装",
                "The custom Blender installation should now be available in version selection.", "自定义 Blender 现在应该可以在版本选择中看到了。",

                //Device settings
                "Cores:", "核心数：",
                "Auto-Performace:", "自动性能：",
                "RenderType:", "渲染类型：",
                "Performance:", "性能：",
                "Used when Auto-Performance disabled, use cores when zero or below", "关闭自动性能时使用；为 0 或更小时按核心数计算",
                "Password:", "密码：",
                "Used for basic authentication, NOT recommended as it is not very secure", "用于基础验证，不太安全，不建议依赖它",
                "Disclaimer: Password should not be your sole protection, it is not stored or transmitted securely", "免责声明：密码不应作为唯一的保护手段，它的存储和传输都不安全",

                //Server queue (this fork)
                "Queue Status", "排队情况",
                "Language", "语言",
                "Rendering now", "正在渲染",
                "(you)", "（你）",
                "Move to front", "置顶",
                "Moved to front", "已置顶",
                "No one is waiting.", "目前没有人在排队。",
                "This computer", "本机",
                "Only the computer hosting the server can move jobs to the front. Other computers can see the queue and their own position.", "只有运行服务端的电脑可以置顶任务；其他电脑可以查看队列和自己所在的位置。",
                "Not connected to any server.", "尚未连接任何服务端。",
                "No prepared and synced node (Blender might still be installing)", "没有已准备并已同步的节点（Blender 可能仍在安装中）",
                "Select Nodes", "节点选择",
                "Version source:", "版本来源：",
                "From server (installed versions)", "从服务端获取版本（已安装）",
                "From Blender website (downloads when needed)", "从 Blender 官网获取（需要时下载）",
                "No Blender version", "没有可用的 Blender 版本",
                "There is no Blender version to render with. Connect a server in the node selection first, or set the version source to the Blender website.", "没有可用于渲染的 Blender 版本。请先在“节点选择”里连接服务端，或把版本来源改为“从 Blender 官网获取”。",
                "Current Queue", "当前队列",
            };

            for (int i = 0; i + 1 < pairs.Length; i += 2)
                map[pairs[i]] = pairs[i + 1];
            return map;
        }

        private static List<Pattern> BuildPatterns()
        {
            List<Pattern> list = new List<Pattern>();
            //{} in the english text matches anything and becomes {0}, {1}.. in the translation
            string[] pairs = new[]
            {
                "BlendFarm by LogicReinc [{}]", "BlendFarm 队列版 [{0}]",
                "Queue ({})", "队列 ({0})",
                "Current Queue ({})", "当前队列 ({0})",
                "Rendering {}%", "渲染中 {0}%",
                "Queue Status ({})", "排队情况 ({0})",
                "Rendering ({}/{})", "渲染中 ({0}/{1})",
                "Queued #{} (ahead {}, now rendering: {})", "排队中：第 {0} 位（前面还有 {1} 个，正在渲染：{2}）",
                "Waiting #{}", "等待第 {0} 位",
                "{} elapsed", "已用时 {0}",
                "{} waiting", "已等待 {0}",
                "You are #{} in line on {}", "你在 {1} 排第 {0} 位",
                "Your job is rendering on {}", "你的任务正在 {0} 上渲染",
                "Preparing Blender {} (This might take a minute depending on your connection speed, only required once per version)", "正在准备 Blender {0}\n（取决于网速可能需要一分钟，每个版本只需要一次）",
                "Console Output from {} ({})", "{0} ({1}) 的控制台输出",
                "Animation frame {} failed to save due to:{}", "动画第 {0} 帧保存失败：{1}",
                "Expecting Blender executable on path {}", "需要在以下路径找到 Blender 可执行文件\n{0}",
                "Path {} does not exist, and is ignored.", "路径 {0} 不存在，已忽略。",
                "{} does not exist", "{0} 不存在",
                "Failed peek due to:{t}", "读取文件信息失败：{0}",
                "Failed render due to:{t}", "渲染失败：{0}",
                "Select Nodes ({}/{} connected)", "节点选择（已连接 {0}/{1}）",
                "Blender {} is installed in a folder that has no Blender executable ({}). Check where it was extracted.", "Blender {0} 的目录里找不到 Blender 可执行文件（{1}），请检查解压位置。",
                "Outdated protocol, update node before connecting (Protocol: {}, Found: {})", "协议版本不一致，请先把该节点上的程序更新到同一版本（本机协议：{0}，对方：{1}）",
                "Outdated protocol, exception during check ({})", "协议版本检查失败，对方可能还是旧版本，请先更新（{0}）",
                "Frames {} to {} rendered. Located at {}.", "已渲染第 {0} 到 {1} 帧。\n位置：{2}。",
                "Exception \"{}\" occured in queue. Continue queue process?", "队列中出现异常“{0}”。是否继续处理队列？",
                "Failed to retrieve versions due to {}, this may be due to no internet connection.An internet connection is required to retrieve version info and download Blender installations. Restart application with internet connection for Blender versions..", "获取版本信息失败：{0}。可能是没有联网。获取版本信息和下载 Blender 都需要网络，请联网后重新启动程序。",
                "The address provided seems to be invalid, expected format is {hostname}:{port} or {ip}{port}, eg. 192.168.1.123:15000", "地址似乎无效，格式应为 {主机名}:{端口} 或 {IP}:{端口}，例如 192.168.1.123:15000",
                "Local server failed to start, if you're already using a port, change it in settings. Or if you're running this program twice, ignore I guess. (TCP: {}, UDP: {})", "本机服务端启动失败。如果端口已被占用，请在设置里修改；如果你同时运行了两个本程序，可以忽略。\n（TCP：{0}，UDP：{1}）",
                "Local Server failed to broadcast or receive broadcasts for auto-discovery. It can be changed in settings. This may have to do with the port being in use. Note that to discover other pcs their broadcast port needs to be the same.. (TCP: {}, UDP: {})", "本机服务端无法发送或接收用于自动发现的广播，可在设置里修改，可能是端口被占用。注意：要发现其他电脑，双方的广播端口必须一致。\n（TCP：{0}，UDP：{1}）",
            };

            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                Regex tokens = new Regex(@"\{t?\}");
                string regex = "^" + string.Join("(.*?)", tokens.Split(pairs[i]).Select(Regex.Escape)) + "$";
                list.Add(new Pattern()
                {
                    Regex = new Regex(regex, RegexOptions.Singleline | RegexOptions.CultureInvariant),
                    Format = pairs[i + 1],
                    Translate = tokens.Matches(pairs[i]).Cast<Match>().Select(x => x.Value == "{t}").ToArray()
                });
            }
            return list;
        }
    }
}
