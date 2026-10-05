# OpenAL Soft 1.25.2 二进制与对应源码

本目录随G104Engine保留第三方动态库、原许可及同版本官方完整源码归档。OpenAL Soft及其版权属于原作者；G104Engine未修改库源码。

- `OpenAL32.dll`：官方Windows二进制包的`bin/Win64/soft_oal.dll`，按原readme允许仅重命名；不是router版本。
- 二进制来源：[openal-soft-1.25.2-bin.zip](https://openal-soft.org/openal-binaries/openal-soft-1.25.2-bin.zip)。SHA-256：`3963b06e319180700be0bcc0d027336664bb3a4975876b4a5442f10746fab5b4`。
- `openal-soft-1.25.2.tar.bz2`：与DLL在同一仓库提供的官方1.25.2源码归档，未修改、未解压构建。下载：2026-10-03。
- 源码来源：[官方源码包](https://openal-soft.org/openal-releases/openal-soft-1.25.2.tar.bz2)，由 [官网](https://openal-soft.org/) 的1.25.2下载入口指向。
- 源码归档SHA-256：`1dbaac44e7579d5bc8847ca8db4b2e8b9fd3961041f35ee20def4958301e1089`，1,132,689字节。
- 归档已只读检查：518个条目，根目录`openal-soft-1.25.2/`，含完整库模块、CMake构建文件和许可；CMake版本为1.25.2，COPYING与二进制包附带文本逐字节一致。
- `COPYING`为库许可，`LICENSE-pffft`为相应组件许可，`readme.txt`为原二进制说明，全部保留。

固定版本 [COPYING第4节](https://raw.githubusercontent.com/kcat/openal-soft/1.25.2/COPYING)要求库二进制分发同时提供对应源码；本仓库将归档放在相同目录供获取。这里不声称已经自行重编译或逐字节复现官方DLL；已实测运行报告为ALSOFT1.25.2，PE资源版本差异见项目输入核查。

源代码归档不是NuGet包、应用运行时依赖或构建输出，也不代表系统安装或升级。正常构建G104Engine使用此目录的现成DLL。将来单独发布应用二进制包时，应继续提供对应库源码和许可获取方式；DLL、许可与对应源码已随b91dfe4保存到源码仓库；后续应用包发布仍需按具体任务安排。
