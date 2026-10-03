# V1 外部原始文件只读核对：2026-10-03

用户已将四个 ZIP 放到 `g104engine/.cache/v1-preparation/`。本次只读取 ZIP 目录和条目的内存流，解析 PE/GLB/音频头及许可文本；没有解压落盘、复制部署、加载 DLL、播放声音、渲染模型、转换素材或安装工具，也没有写引擎/验证程序代码。

以下是输入文件静态证据，不等于 SharpGLTF 导入、GPU 蒙皮、OpenAL 初始化或最终效果验收。完整 ZIP 哈希用于固定本次收到的文件，不冒充官方签名验证。当前范围见 [V1](../plans/basic-training-ground-v1-draft.md)，准备接续见 [清单](../plans/v1-dependencies-and-assets.md)。

## 1. 四份原始 ZIP

| 文件 | 字节数 | ZIP 条目数 | 声明解压总字节数 |
| --- | ---: | ---: | ---: |
| openal-soft-1.25.2-bin.zip | 18,574,944 | 69 | 48,802,174 |
| Universal Animation Library[Standard].zip | 15,904,933 | 12 | 63,648,179 |
| kenney_impact-sounds.zip | 800,850 | 134 | 1,035,004 |
| kenney_interface-sounds.zip | 834,536 | 104 | 961,273 |

四包目录均可读；条目路径检查未见绝对路径、盘符或上级跳转。检查不是完整格式模糊测试，也没有声称所有压缩条目都已解码验证。四文件已由现有 `**/.cache/` 规则忽略，本轮未改 .gitignore。

| 文件 | SHA-256 |
| --- | --- |
| openal-soft-1.25.2-bin.zip | `67A0C4B800BD860C93C04F38CAF8CBE4875F9C84700AC430EFC451F70E265434` |
| Universal Animation Library[Standard].zip | `CC73FC4E495B82958207316596317A3F40B9FA38065BDE1027937452DA537724` |
| kenney_impact-sounds.zip | `029D734AF1582474EDF3A694D1B0CEBC97C1C152F2F39FA34D4C2BAFC5DE77F8` |
| kenney_interface-sounds.zip | `F2193D072726D6758A5F7871B2DCC54DCCE0D5C35C6F0A62F92549B327C81232` |

## 2. OpenAL Soft：实际实现库与版本差异

采用条目候选：`openal-soft-1.25.2-bin/bin/Win64/soft_oal.dll`，3,834,368 字节，AMD64 PE32+，Machine `0x8664`。SHA-256：

`3963B06E319180700BE0BCC0D027336664BB3A4975876B4A5442F10746FAB5B4`

包内还存在 `router/Win64/OpenAL32.dll`，它是路由器；不能按文件名直接拿来替代实现库。包内 readme 明确支持将上述 Win64 soft_oal.dll 重命名为 OpenAL32.dll 随应用直接部署。后续持久部署应以选定实现库为源，经 Sandbox 输出规则进入 Debug/Release 根目录；不能只手工放进 bin 或把缓存作为长期发布源。

需保留同包根目录的 `COPYING`、`LICENSE-pffft` 与 `readme.txt`。本轮只核对文件，没有复制、部署或扩大发布范围。

**版本元数据差异须保留：** 压缩包名为 1.25.2，DLL 内嵌字符串为 `ALSOFT 1.25.2`；PE 的 FileVersion/ProductVersion 与固定版本却为 1.25.1 / 1.25.1.0。可推测版本资源未同步，但不能把推测当已证实原因，也不能只凭该差异认定用户下错包。实际运行时版本与加载行为仍待代码获准后检查。

静态导入包括 AVRT、KERNEL32、msvcrt、ole32、SHELL32、USER32、WINMM；这不覆盖运行时动态加载，也不证明设备输出已经可用。

## 3. 角色与动画：Standard 包满足基础动作输入

推荐采用：`Universal Animation Library[Standard]/Unreal-Godot/UAL1_Standard.glb`。

| 项目 | 实际结果 |
| --- | --- |
| GLB 头 | 版本 2，声明长度与实际 7,618,436 字节一致 |
| 场景/网格 | 67 节点、1 mesh、2 个三角形 primitive；8,546 顶点、13,744 三角形，ushort 索引 |
| 骨架 | 1 skin、65 joints；不能用默认 64 骨骼上限截断 |
| 蒙皮属性 | 仅 JOINTS_0/WEIGHTS_0，每顶点实际最多 4 个正权重影响；权重非负、无零和，和偏差不超过约 1.2e-7 |
| 动画 | 43 clips；每个 195 个 TRS channel，目标为同一组 65 joints，全部 LINEAR |
| 依赖 | 无 required/used extensions、无外部 buffer、图片或纹理，无 morph |
| 材质 | 2 个 double-sided 金属度/粗糙度材质，纯 baseColorFactor；metallic=0、roughness=0.5 |
| 许可/说明 | ZIP 内 License.txt/README.txt 标注 CC0 1.0，README 区分 `_RM` 根运动版本 |

选定 GLB SHA-256：

`69591853D817488EDAA8FD9BF8FC1D821EAEAF789F8627B3CD23B41C4ED67997`

| 用途候选 | 包内实际 Clip 名 | 时长（秒） |
| --- | --- | ---: |
| 待机 | Idle_Loop | 2.500000 |
| 行走 | Walk_Loop | 1.333333 |
| 跑步 | Jog_Fwd_Loop | 0.933333 |
| 快跑备选 | Sprint_Loop | 0.666667 |
| 起跳 | Jump_Start | 1.333333 |
| 腾空 | Jump_Loop | 2.500000 |
| 落地 | Jump_Land | 1.266667 |
| 姿态/交互辅助 | A_TPose / Interact | 2.500000 / 2.000000 |

不存在名为 Run_Loop 的动作。项目 Run 状态需要明确映射至 Jog 或 Sprint；当前优先以 Jog 作为待验证候选，最终按速度/步幅和视觉效果定案。跳跃动作时长不自动决定角色物理跳高/滞空时间。

上述基础动作在非 `_RM` 文件中 root 平移均为零，旋转保持约 -90° X、缩放为 1；pelvis 仍有动作起伏。对照 `_RM` 文件，Walk/Jog/Sprint 根平移沿 +Z 累计约 1.3/5/5.5，默认网格高度约 1.829，脚趾朝 +Z；这些是面向 +Z、米级尺度的输入证据。实施时需与引擎默认 -Z 前方适配，并保留骨架原有根变换；尚未做独立尺寸标定或实际画面验收。

该模型没有 TANGENT/图片/纹理，不能覆盖贴图与法线贴图导入验收。后续可准备本项目自有的小型贴图测试资产，不必为此扩大角色素材范围。尚待 SharpGLTF 实际读取、骨架/palette、GPU 蒙皮、过渡、循环、跳跃节奏和脚滑验证。

## 4. 两个 Kenney 声音包：需要格式处理

| 包 | 音频数量 | 实际编码 | 声道/采样率 |
| --- | ---: | --- | --- |
| Impact Sounds 1.0 | 130 | 全部 .ogg，Ogg Vorbis | 全部双声道，44100 Hz |
| Interface Sounds 1.0 | 100 | 全部 .ogg，Ogg Vorbis | 77 单声道、23 双声道，均44100 Hz |

已读取全部230个音频条目的头：魔数 OggS，识别包为 Vorbis；两包 WAV 数量均为0。未播放，也未解码验证完整音频流。包内 License.txt 标注 Kenney/CC0 1.0；另有两个 .url 文件，没有执行其中链接。

可供后续试听筛选的真实条目（仅按名称推荐，不代表已验证用途）：

| 用途候选 | 包与条目 | 当前声道 |
| --- | --- | ---: |
| UI 点击/确认 | Interface：Audio/click_001.ogg、Audio/confirmation_001.ogg | 1 |
| 按钮/开关 | Interface：Audio/switch_001.ogg | 2 |
| 脚步变体 | Impact：Audio/footstep_concrete_000.ogg、Audio/footstep_concrete_001.ogg | 2 |
| 木门到位碰撞 | Impact：Audio/impactWood_medium_000.ogg | 2 |
| 金属到位碰撞 | Impact：Audio/impactMetal_light_000.ogg | 2 |

这些是现有 V1 PCM16 WAV 子集不能直接读取的输入；改扩展名不等于转换。**用户已选择保持 PCM16 WAV，离线转换选定声音（D50）**；3D 点声源候选转换/下混为单声道后试听。新增 Ogg Vorbis 运行时解码方案未采用，当前尚未转换或新增依赖。

只读 Get-Command 在当前 PATH 未找到 ffmpeg/ffprobe；不据此断言机器任何位置都没有转换工具。工具来源/准备和实际转换须另行明确，不自动安装。包内也未确认适合无缝循环的环境声音；循环能力可先用受控测试信号验收，最终听感另确认。

## 5. 当前结论与接续

- 四份原始文件齐全，记录了可重复核对的条目与哈希；继续保留在被忽略的缓存目录。
- OpenAL Win64 实现库已识别，角色 GLB 与所需同骨架动作已确认存在，满足进入后续部署/导入准备的输入条件。
- 声音需格式处理；角色仍需实际导入/显示验收，贴图和循环测试输入另补小范围素材。
- 本轮未部署运行库、未导入工程资源、未转换声音、未安装工具、未修改项目/代码或 Git 状态；检查记录按项目持久化规则保存到文档。
- 下一步按已选离线转换路线确定工具/条目并明确持久部署规则；完成准备后，开始写代码仍必须弹窗得到明确同意。
