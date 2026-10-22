# OpenAL Soft 1.25.2 二进制与对应源码

本页记录参考G104Engine工程采用的第三方动态库、原许可及同版本官方完整源码归档。OpenAL Soft及其版权属于原作者；G104Engine未修改库源码。新电脑从空白工程开始时，按下方步骤自行取得这些文件，不需要clone仓库。

- `OpenAL32.dll`：官方Windows二进制包的`bin/Win64/soft_oal.dll`，按原readme允许仅重命名；不是router版本。
- 二进制来源：[openal-soft-1.25.2-bin.zip](https://openal-soft.org/openal-binaries/openal-soft-1.25.2-bin.zip)。**输入ZIP的SHA-256**：`67a0c4b800bd860c93c04f38caf8cbe4875f9c84700ac430efc451f70e265434`。
- **提取后OpenAL32.dll的SHA-256**：`3963b06e319180700be0bcc0d027336664bb3a4975876b4a5442f10746fab5b4`。ZIP与DLL是不同文件，应分别校验；仅重命名soft_oal.dll不改变DLL字节。
- `openal-soft-1.25.2.tar.bz2`：与DLL在同一仓库提供的官方1.25.2源码归档，未修改、未解压构建。下载：2026-10-03。
- 源码来源：[官方源码包](https://openal-soft.org/openal-releases/openal-soft-1.25.2.tar.bz2)，由 [官网](https://openal-soft.org/) 的1.25.2下载入口指向。
- 源码归档SHA-256：`1dbaac44e7579d5bc8847ca8db4b2e8b9fd3961041f35ee20def4958301e1089`，1,132,689字节。
- 归档已只读检查：518个条目，根目录`openal-soft-1.25.2/`，含完整库模块、CMake构建文件和许可；CMake版本为1.25.2，COPYING与二进制包附带文本逐字节一致。
- `COPYING`为库许可，`LICENSE-pffft`为相应组件许可，`readme.txt`为原二进制说明，全部保留。

固定版本 [COPYING第4节](https://raw.githubusercontent.com/kcat/openal-soft/1.25.2/COPYING)要求库二进制分发同时提供对应源码；本仓库将归档放在相同目录供获取。这里不声称已经自行重编译或逐字节复现官方DLL；已实测运行报告为ALSOFT1.25.2，PE资源版本差异见项目输入核查。

源代码归档不是NuGet包、应用运行时依赖或构建输出，也不代表系统安装或升级。正常构建G104Engine使用此目录的现成DLL。将来单独发布应用二进制包时，应继续提供对应库源码和许可获取方式；DLL、许可与对应源码已随b91dfe4保存到源码仓库；后续应用包发布仍需按具体任务安排。

## 不clone时从空白工程取得OpenAL文件

新项目尚无本目录。先在新g104engine根创建third_party/openal-soft/1.25.2，再按以下步骤取得同版本文件；完整素材主线见 [dependencies.md](../../../docs/guides/dependencies.md#standalone-assets)。

1. 从上方官方固定Windows包地址下载openal-soft-1.25.2-bin.zip，保存到新项目.cache/v1-preparation，按**ZIP哈希**校验，再解压到缓存内独立目录。
2. 从`openal-soft-1.25.2-bin/bin/Win64/soft_oal.dll`复制到新项目third_party/openal-soft/1.25.2，改名为OpenAL32.dll，再按**DLL哈希**核对。同包根目录的COPYING、LICENSE-pffft和readme.txt一并复制到该正式目录。不采用router/Win64/OpenAL32.dll或Win32文件，不复制到System32，不安装系统OpenAL。
3. 从上方官方源码地址下载openal-soft-1.25.2.tar.bz2，按源码归档SHA校验，保留压缩形式放到新项目同一正式目录。源码归档不需要解压编译；后续发布二进制仍需提供对应源码与许可获取方式。
4. 手动将本说明文件保存/复制为新项目同目录的SOURCE.md，保留版本、官方来源、ZIP/DLL/源码归档各自哈希、包内Win64来源及许可文件名。这只是单文件传输，不要求取得整个仓库。
5. 按 [environment.md](../../../docs/guides/environment.md#resource-copy)为新Sandbox添加复制配置；之后构建才会从正式目录复制DLL及许可到输出。音频加载/播放代码另行实现或手动取得，放好DLL不代表空白工程已具备音频功能。

如果已经取得整个原工程，本目录的现成文件可以直接保留，不需要重复下载或转换；缺失时仍按以上固定来源与哈希恢复。只改bin不能修复下次构建的正式来源。完整已有项目路线见 [reproduction.md](../../../docs/guides/reproduction.md#existing-repository-setup)。

2026-10-22补充新电脑说明时，已联网核对[OpenAL Soft官网](https://openal-soft.org/)仍列出1.25.2二进制与源码入口，并只读核对本地ZIP、DLL及源码归档哈希；本轮没有下载归档、替换DLL或编译。旧表述将二进制ZIP来源链接与DLL哈希写在同一项，本次明确拆开两种校验对象；原DLL及已记录的运行证据不变。

2026-10-23按用户纠正将新电脑主线改为自行创建空白工程、独立下载文件，已有工程改为可选路径。沿用上述2026-10-22来源核对与固定哈希，本批仅改说明，没有下载或替换动态库、安装包或运行引擎。
