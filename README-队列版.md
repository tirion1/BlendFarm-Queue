# BlendFarm 队列版（Queue Edition）

这是 [LogicReinc.BlendFarm](https://github.com/LogicReinc/LogicReinc.BlendFarm) 的分支，在原版基础上加了**多人排队渲染**：
多台电脑同时连到同一台渲染机时，按提交先后自动排队，不再需要"等别人渲完才能点开始"。

原作者是 LogicReinc，本分支遵循原项目的 GPL-3.0 许可证。

> **和原版不能互连。** 通信协议版本升到了 8，原版（协议 7）和本分支的程序不能互相连接，
> 渲染机和所有提交任务的电脑都要换成本分支的版本。

## 用法（和原版一样，打开即用，不用安装）

从 Releases 下载 zip，解压即用，目标电脑不需要安装 .NET。

| 电脑 | 运行什么 | 做什么 |
|---|---|---|
| A（渲染机） | `LogicReinc.BlendFarm.Server.exe`（无界面），或 `LogicReinc.BlendFarm.exe`（GUI 自带同样的服务端） | 双击运行并保持开着；防火墙放行 TCP **15000** |
| B、C（提交任务） | `LogicReinc.BlendFarm.exe` | 在"节点选择"里连接 A（或在"新建节点"里填 `A的IP:15000`），照常渲染 |

## 排队

- **按项目排队，不按单帧**：B 的整段动画会连续渲完，再轮到 C，不会 B、C 的帧交替（交替会让 Blender 反复重载文件，很慢）。
- 持有者两帧之间有 15 秒保留期，超过 15 秒没发下一帧就让给排队的人。可在渲染机的 `ServerSettings` 文件里改 `QueueLeaseSeconds`。
- 取消任务、关掉软件，只会取消**自己的**任务和排队，不会影响别人正在跑的渲染（原版会误杀）。
- 如果某台电脑还有别的空闲节点，会先用空闲节点；只有所有节点都忙时才排队。

## 当前队列（右侧侧栏，默认折叠，点击展开）

- 标题行显示摘要，例如 `当前队列 (2) · 你在 A 排第 2 位`。
- 展开后能看到谁在渲染、谁在排队、各自用时。
- 渲染机本机的 GUI 里，排队项旁有 **"置顶"** 按钮，把某个人的任务（连同整段动画）移到当前渲染结束后的第一位。
  只有运行服务端的那台电脑能置顶，其他电脑只能查看，网络协议里没有任何改队列的命令。

## 节点选择

- 打开程序就会连接**你以前连过的**节点；局域网自动发现的节点只列出，不会自动连接。
- 点"断开"后，该节点下次不再自动连接。
- 掉线的已选节点每 15 秒自动重连。

## Blender 版本

版本下拉框上方可选版本来源，选择会被记住：

- **从服务端获取版本（默认）**：列出各服务端**已安装**的版本，选了就能直接用，不会触发下载，也不需要联网。
  所以断网的电脑也可以提交任务，只要渲染机上装好了 Blender。
- **从 Blender 官网获取**：和原版一样，选了未安装的版本，任务开始后由服务端先去官网下载。

渲染机会自动识别 `BlenderData` 目录里已有的版本（目录名为 `版本名-windows64`，例如 `blender-4.5.10-windows64`），不需要再手动编辑 `VersionCustom`。
如果解压时多套了一层目录（`blender-x-windows64\blender-x-windows-x64\blender.exe`），程序会自动移到上一层；
目录里找不到 `blender.exe` 时会给出明确提示。

## 中文界面（可选）

界面默认跟随系统语言。窗口里有 **"中文 / English"** 按钮，切换后重启生效。没有翻译的文字会原样显示英文。

## 其他修复（相对原版）

- 修复 `4.5.10` 这类补丁号为两位数的版本永远解析不出来的问题。
- 修复版本缓存是否过期只比较"一年中的第几天"、不比年份，导致一年前的缓存被当成新的。
- 已安装但不在版本列表里的版本，现在也能正常准备并使用。
- 版本列表按数字排序（`4.5.10` 排在 `4.5.4` 前面）。
- 文本框里的中文文件名不再显示成方块。
- 断网时不再等请求超时、也不再弹错误框。

## 构建

需要 .NET SDK（构建机需要，目标电脑不需要）。

```powershell
.\build-win.ps1
```

产物在 `release\`（已被 git 忽略）：两个文件夹和两个 zip，都是自带运行时的单文件程序。

测试（不需要 Blender）：

```powershell
dotnet test LogicReinc.BlendFarm.Tests --filter "FullyQualifiedName~RenderQueueTests|FullyQualifiedName~QueueViewTests|FullyQualifiedName~VersionListTests|FullyQualifiedName~BlenderFolderTests"
```

## 改动了哪里

- 新增 `LogicReinc.BlendFarm.Server/RenderQueue.cs`：队列本体。
- `RenderServerClientTcp.cs`、`BlenderManager.cs`、`RenderServer.cs`：渲染前进队列；取消/断线只作用于自己；向客户端推送队列和版本列表；版本目录检查。
- `LogicReinc.BlendFarm.Shared/`：`Loc.cs`（翻译）、`QueueStatusResponse.cs`、`BlenderVersionsRequest.cs`（新增的只读网络包）、版本列表修复。
- `LogicReinc.BlendFarm.Client/`：忙节点不再被直接丢弃；接收队列状态。
- `LogicReinc.BlendFarm/`：`ServerHub.cs`（全局节点管理）、`Controls/`（折叠侧栏、节点选择、队列）、语言切换。
