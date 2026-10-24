# Third-party notices

Replay Local fork notice — modified 2026-10-23. Derived from BasilForge/Kairos; original authorship and dependency notices are preserved. See [CREDITS.md](CREDITS.md) and [MODIFICATIONS.md](MODIFICATIONS.md).

Kairos's own source code is licensed under the GNU General Public License, version 2 or (at your option) any later version. See [LICENSE](LICENSE).

Release builds include the following components, copied unmodified from OBS Studio 32.2.2 (<https://github.com/obsproject/obs-studio>).

| Component | Files | License | Source |
|---|---|---|---|
| libobs and its graphics backends | `obs.dll`, `libobs-d3d11.dll`, `libobs-winrt.dll`, `w32-pthreads.dll`, `data/libobs` | GPL-2.0-or-later | obs-studio repository, tag 32.2.2 |
| OBS plugins | `win-capture`, `win-wasapi`, `obs-nvenc`, `obs-ffmpeg`, `obs-qsv11`, `obs-x264` | GPL-2.0-or-later | obs-studio repository, tag 32.2.2 |
| OBS helper programs | `obs-ffmpeg-mux.exe`, `obs-nvenc-test.exe`, `obs-amf-test.exe`, `obs-qsv-test.exe` | GPL-2.0-or-later | obs-studio repository, tag 32.2.2 |
| FFmpeg libraries | `avcodec`, `avformat`, `avutil`, `avfilter`, `avdevice`, `swscale`, `swresample` | GPL-3.0-or-later in this build | <https://ffmpeg.org> (OBS dependency build) |
| x264 | `libx264-*.dll` | GPL-2.0-or-later | <https://www.videolan.org/developers/x264.html> |
| zlib | `zlib.dll` | zlib License | <https://zlib.net> |
| libcurl | `libcurl.dll` | curl License (MIT-style) | <https://curl.se> |
| SRT, librist | `srt.dll`, `librist.dll` | MPL-2.0, BSD-2-Clause | OBS dependency build |
| NVIDIA Video Codec SDK headers | compiled into `obs-nvenc` | MIT | NVIDIA |
| AMD AMF headers | compiled into `obs-ffmpeg` | MIT | AMD GPUOpen |
| Intel oneVPL / VPL | compiled into `obs-qsv11` | MIT | Intel |

Smart Save (voice-triggered saving) uses these components, unmodified:

| Component | Files | License | Source |
|---|---|---|---|
| ONNX Runtime 1.30.0 | `onnxruntime.dll`, `onnxruntime_providers_shared.dll`, `Microsoft.ML.OnnxRuntime.dll` | MIT | <https://github.com/microsoft/onnxruntime> |
| openWakeWord feature models | `models/melspectrogram.onnx`, `models/embedding_model.onnx` | Apache-2.0; the embedding model is a re-implementation of Google's `speech_embedding` TF Hub module (Apache-2.0) | <https://github.com/dscripka/openWakeWord>, release v0.5.1 |
| Silero VAD | `models/silero_vad.onnx` | MIT | <https://github.com/snakers4/silero-vad> (as packaged in openWakeWord v0.5.1) |

Their license texts ship in the `licenses` folder: `ONNX-Runtime-LICENSE.txt` and `ONNX-Runtime-ThirdPartyNotices.txt` (Copyright (c) Microsoft Corporation), `Apache-2.0.txt` (openWakeWord and Google speech_embedding) and `Silero-VAD-LICENSE.txt` (Copyright (c) 2020-present Silero Team).

Local editing and conversion use the FFmpeg libraries listed above through:

| Component | Files | License | Source |
|---|---|---|---|
| FFmpeg.AutoGen 8.1.0 | `FFmpeg.AutoGen.dll` | MIT | <https://github.com/Ruslan-B/FFmpeg.AutoGen> |

FFmpeg.AutoGen license: `licenses/FFmpeg.AutoGen-LICENSE.txt`.

The editor's text tool ships these fonts, unmodified, from Google Fonts (<https://github.com/google/fonts>, `ofl` folder), in `fonts`:

| Font | File | License | Copyright |
|---|---|---|---|
| Almarai | `Almarai-ExtraBold.ttf` | SIL Open Font License 1.1 | Copyright 2019 The Almarai Project Authors |
| Lalezar | `Lalezar-Regular.ttf` | SIL Open Font License 1.1 | Copyright 2015 The Lalezar Project Authors |
| Rakkas | `Rakkas-Regular.ttf` | SIL Open Font License 1.1 | Copyright 2016 Zeynep Akay |
| Aref Ruqaa | `ArefRuqaa-Bold.ttf` | SIL Open Font License 1.1 | Copyright 2015-2020 The Aref Ruqaa Project Authors |
| Cairo | `Cairo-Bold.ttf` | SIL Open Font License 1.1 | Copyright 2009 The Cairo Project Authors |
| Tajawal | `Tajawal-Bold.ttf` | SIL Open Font License 1.1 | Copyright 2018 Boutros International |
| Changa | `Changa-SemiBold.ttf` | SIL Open Font License 1.1 | Copyright 2018 The Changa Project Authors |
| Reem Kufi | `ReemKufi-Regular.ttf` | SIL Open Font License 1.1 | Copyright 2015-2022 The Reem Kufi Project Authors |
| Lemonada | `Lemonada-SemiBold.ttf` | SIL Open Font License 1.1 | Copyright 2011 The Lemonada Project Authors |

Their license texts ship as `licenses/Almarai-OFL.txt`, `licenses/Lalezar-OFL.txt`, `licenses/Rakkas-OFL.txt`, `licenses/ArefRuqaa-OFL.txt`,
`licenses/Cairo-OFL.txt`, `licenses/Tajawal-OFL.txt`, `licenses/Changa-OFL.txt`, `licenses/ReemKufi-OFL.txt` and `licenses/Lemonada-OFL.txt`.

Replay Local release executables must be accompanied by their complete corresponding source as required by the GPL, including modified source and build scripts. The source commit is recorded in `dependency-manifest.json`. The companion `ReplayLocal-Fontes-e-Licencas.zip` supplies the exact application snapshot, official OBS source, pinned obs-deps recipes and patches, and dependency sources. `licenses/native-sources.json` records identities and hashes; `licenses/FFmpeg-build-configuration.txt` records the shipped FFmpeg options. The complete native dependency PE sections match the prebuilt package pinned by OBS; whole-file hashes differ after OBS signing. See [docs/PUBLISHING.md](docs/PUBLISHING.md).

Replay Local removes the browser and updater. It includes Microsoft .NET 10.0.13 (MIT, `licenses/dotnet-LICENSE.txt` and `licenses/dotnet-ThirdPartyNotices.txt`) and the signed Microsoft Visual C++ 14.44 app-local redistribution files from Visual Studio Build Tools. See Microsoft's redistribution terms: <https://learn.microsoft.com/visualstudio/releases/2022/redistribution>.

The icons in the interface come from the Segoe Fluent Icons font that ships with Windows; they are not redistributed.

Historical upstream attribution: the X, TikTok and Instagram marks supplied with Kairos belong to their respective owners. Replay Local has removed the social share panel; this note preserves the origin of inherited materials and does not claim endorsement or ownership of those marks.

The emoji pictures in the caption picker (`src/Kairos/Ui/Assets/Emoji`) are Microsoft's Fluent Emoji designs as Windows 11 draws them (Segoe UI Emoji), MIT License, Copyright (c) Microsoft Corporation, <https://github.com/microsoft/fluentui-emoji>.

## Historical upstream wordmark fonts (in inherited name images only)

The name in the sidebar (`src/Kairos/Ui/Assets/wordmark-en.png`, `wordmark-ar.png`) is drawn from
**Fraunces** (Undercase Type) and **Aref Ruqaa** (Abdullah Aref), both under the SIL Open Font License 1.1.
The fonts themselves are not shipped.

## License version used by the combined portable distribution

The shipped FFmpeg reports **GPL version 3 or later** (recorded in `licenses/FFmpeg-build-configuration.txt`). The combined portable distribution is supplied under **GPL-3.0-or-later**, using the later-version option of the GPL-2.0-or-later parts. Application source and original Kairos notices remain GPL-2.0-or-later. Other components retain their own notices and licenses; `licenses/GPL-3.0.txt` supplies the GPLv3 text.
