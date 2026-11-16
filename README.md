# ADB 连接助手 · ADB Keeper

Windows 上的 Android 真机调试连接助手：保持亮屏、定期检查 ADB、针对所选设备重连，并提供 scrcpy 投屏。浅天蓝色界面，普通用户权限运行。

## 下载与使用

从 [Releases](https://github.com/jinmu666666-cmd/adb-keeper/releases/latest) 下载 `AdbKeeper-v1.0.0-win-x64.zip`，**完整解压到可写文件夹**。

1. 首次运行 `Install-Dependencies.cmd`，从 scrcpy 官方 GitHub 下载经过 SHA-256 校验的 **5.0.1 Windows x64 原包**，解压到旁边的 `scrcpy` 文件夹（包含 ADB）。已有自己的 ADB 可以直接在界面填写路径；投屏仍需该文件夹。
2. 双击 `AdbKeeper.exe`。手机启用开发者选项和 USB 调试，连接数据线，在手机上确认授权。
3. 刷新设备，按 **手机型号 + Android ID** 区分手机，选择后点击“开始保持连接”。同一按钮可停止并恢复手机原设置。
4. “打开投屏 / 关闭投屏”为独立开关；关闭投屏不停止连接监测。默认不传输音频。
5. 窗口右上角 **X 隐藏到本工具托盘**；双击托盘恢复，右键“退出”真正关闭。无需工具箱。

系统：Windows 10/11 x64，.NET Framework 4.5+（Windows 10/11 通常自带兼容的4.8运行时），Windows PowerShell 5.1。手机投屏要求以 [scrcpy 官方说明](https://github.com/Genymobile/scrcpy/tree/v5.0.1#prerequisites) 为准。个别手机需要额外开启“USB调试（安全设置）”才能控制。

## 保持连接的范围

- 每5秒检查，失败后最多每15秒对所选设备重连；不会调用 `adb kill-server`，不主动重启全局ADB服务。
- 充电常亮、30分钟熄屏超时，每约30秒发送唤醒；保持期间阻止电脑自动休眠。不会解锁手机，不能阻止用户主动睡眠、断电或线缆/驱动故障。
- USB使用原ADB传输序列号；IPv4:端口无线设备尝试 `adb connect`。无线配对请先用官方ADB完成，本版不提供配对向导。
- 从在线变为断线，右下角出现8秒非抢焦点提示。持续断线不重复，恢复后再次断线再提示。刚开始就是离线不会误报“刚断开”。
- Android ID用于显示，可能因重置/用户配置变化，不是永久硬件标识。**不保证AI聊天会话永不断线**，只辅助本机ADB传输。

## 设置恢复与本地状态

数据：`%LOCALAPPDATA%\AdbKeeper`，独立于工具箱版本。

| 文件 | 用途 |
|---|---|
| `restore.json` | 修改前保存手机序列号、充电常亮和超时原值；恢复成功后删除 |
| `status.json` | AI可读的设备、连接状态、active和ISO8601更新时间；读取方必须判断是否过期 |
| `connection.log` | 本地连接/投屏日志，可能含设备标识 |

异常退出后重新连接**原手机**，启动助手点击“重试恢复设置”。手机离线导致无法恢复时，退出会提示，可选择保留记录退出；下次仍需恢复。不要在恢复前删数据或卸载。损坏的恢复记录会禁止开始新的保持连接，先备份并检查该文件。

不联网收集日志、不上传设备数据。提Issue时请隐藏设备标识、路径和个人信息；不要提交ADB授权私钥。公共单机版与工具箱版不要同时对同一台手机执行保持/恢复，以免各自保存了对方修改后的设置。

## 从源码构建

```powershell
powershell -NoProfile -File .\build.ps1 -Test -Package
```

使用Windows自带.NET Framework `csc.exe`，不需要Node、Python、NuGet或工具箱源码。输出 `bin\AdbKeeper.exe` 和 `dist\AdbKeeper-v1.0.0-win-x64.zip`。测试使用独立临时目录和假ADB，覆盖设备识别、提示状态、亮屏修改与恢复、断线恢复、异常退出记录恢复、托盘关闭行为；不修改真实手机。

环境变量 `ADBKEEPER_DATA_DIR` 可覆盖数据目录，用于隔离测试；正常用户无需配置。开发工具可使用当前用户命名事件 `Local\AdbKeeper.Show` / `Local\AdbKeeper.Exit` 请求显示/退出，进程内仍先尝试恢复设置。

## 版本与验证

首个独立公开版 **v1.0.0**，来自现用助手代码并移除工具箱框架依赖。发布验证范围见 [VALIDATION.md](VALIDATION.md)。历史工具箱版曾在真实手机使用，不等于独立版全部真机/长时间测试通过。

## 开源与第三方

本仓库自己的代码与手机图标为MIT许可；**不包含工具箱框架、工具登记、运行日志、设备数据或授权密钥**。scrcpy/ADB/FFmpeg/SDL等来自独立上游，未上传到本仓库或本项目发布ZIP；首次准备脚本直接下载未改动的官方原包，保留其中全部文件及许可证。详见 [THIRD_PARTY.md](THIRD_PARTY.md)。
