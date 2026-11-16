# 第三方依赖

自有代码采用根LICENSE中的MIT许可，不把第三方组件改为MIT。

- scrcpy 5.0.1（Genymobile/Romain Vimont）：https://github.com/Genymobile/scrcpy/tree/v5.0.1 ，Apache-2.0；源代码及构建说明见上游。
- 官方Windows包内ADB、FFmpeg、SDL、libusb等按各自上游许可；使用/再分发时遵循其条款与源码提供要求：https://github.com/Genymobile/scrcpy/blob/v5.0.1/doc/windows.md 。
- 本项目发布包不捆绑这些二进制；`Install-Dependencies.ps1`只将官方原包下载到用户电脑并完整解压，不修改其中代码或资源，不向GitHub上传用户下载的副本。
- 固定来源：https://github.com/Genymobile/scrcpy/releases/download/v5.0.1/scrcpy-win64-v5.0.1.zip
- SHA-256：`b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5`，依据scrcpy v5.0.1官方Windows文档。

依赖更新时同时核对官方Release和哈希，不将其他下载站的同名文件直接替换。
