# JDK Manager

<div align="center">

**Windows 环境下一键切换 JDK 版本的可视化工具**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows-0078D7?style=flat-square&logo=windows)](https://docs.microsoft.com/dotnet/desktop/wpf/)

</div>

---

## 📖 简介

JDK Manager 是一款专为 Java 开发者设计的 Windows 应用程序，帮助您轻松管理多个 JDK 版本并实现一键切换。无需手动修改环境变量，告别繁琐的命令行操作。

## ✨ 功能特点

- 🔍 **自动扫描** - 从注册表和常见安装路径自动发现已安装的 JDK
- ➕ **手动添加** - 支持手动指定任意 JDK 安装目录
- 🔄 **一键切换** - 可视化界面修改系统环境变量 `JAVA_HOME` 和 `PATH`
- ✅ **实时验证** - 切换后自动验证是否成功
- 💾 **配置持久化** - 自动保存 JDK 列表到本地配置文件
- 🎨 **现代界面** - 简洁美观的 Material Design 风格

## 📦 系统要求

| 项目 | 要求 |
|------|------|
| 操作系统 | Windows 10 / Windows 11 (x64) |
| .NET 运行时 | 无需预装（已打包到应用中） |
| 权限 | **需要管理员权限**（修改系统环境变量） |

## 🚀 快速开始

### 方式一：直接运行（开发/测试）

```bash
# 在项目根目录执行
dotnet run --project src/JdkManager/JdkManager.csproj
```

### 方式二：发布后使用

1. **运行发布脚本**
   ```powershell
   .\publish.bat
   ```

2. **找到可执行文件**

   在 `publish\win-x64\` 目录中找到 `JdkManager.exe`

3. **运行程序**

   双击运行（会自动请求管理员权限）

## 📖 使用指南

### 首次使用

1. 启动程序后，会自动扫描系统中已安装的 JDK
2. 当前激活的 JDK 版本会显示在顶部并标记绿色圆点

### 添加 JDK

1. 点击 **`+ 添加`** 按钮
2. 选择 JDK 安装目录（需包含 `bin\java.exe`）
3. 添加成功后会显示在列表中

### 切换 JDK

1. 在列表中点击要切换的 JDK
2. 点击 **`切换选中版本`** 按钮
3. 等待切换完成

### 验证切换

打开新的命令行窗口，执行：
```bash
java -version
```

## 🏗️ 项目结构

```
JdkManager/
├── src/JdkManager/          # 源代码目录
│   ├── Assets/              # 应用资源（图标等）
│   ├── Converters/          # XAML 转换器
│   ├── Models/              # 数据模型
│   ├── Services/            # 业务服务
│   ├── ViewModels/          # MVVM 视图模型
│   └── Views/               # XAML 视图
├── publish/                 # 发布输出
├── .gitignore
├── LICENSE
├── README.md
└── publish.bat              # 发布脚本
```

## 🛠️ 技术栈

- **框架**: .NET 8.0 + WPF
- **架构**: MVVM (CommunityToolkit.Mvvm)
- **发布**: 单文件自包含应用 (Single-file Self-contained)
- **图标**: 自定义设计 ICO

## ⚠️ 注意事项

1. **管理员权限**: 程序需要管理员权限才能修改系统环境变量
2. **环境变量生效**: 切换后，已打开的命令行窗口需要重新打开才能生效
3. **IDE 重启**: IntelliJ IDEA、Eclipse 等 IDE 可能需要重启才能识别新的 JDK 设置
4. **配置文件**: JDK 列表保存在 `%LOCALAPPDATA%\JdkManager\jdk-config.xml`

## 📸 界面预览

```
┌────────────────────────────────────────────────┐
│  🎯 JDK Manager                                │
│  一键切换 Java 版本                             │
├────────────────────────────────────────────────┤
│  当前激活的 Java 版本                           │
│  JDK 17.0.8                                   │
├────────────────────────────────────────────────┤
│  已安装的 JDK:                                 │
│                                                │
│  ● JDK 17.0.8 (17) • Oracle                   │
│    C:\Program Files\Java\jdk-17               │
│                                                │
│    JDK 11.0.20 (11) • OpenJDK                 │
│    C:\Program Files\Java\jdk-11               │
│                                                │
│    JDK 1.8.0_381 (8) • Oracle                 │
│    D:\Java\jdk1.8                             │
├────────────────────────────────────────────────┤
│  [⟳ 刷新]  [+ 添加]  [- 删除]  [📂 打开]      │
│                                                │
│            [  切换选中版本  ]                  │
│                                                │
│  已扫描到 3 个 JDK                              │
└────────────────────────────────────────────────┘
```

## ⚙️ 配置文件

JDK 列表和当前激活的 Java 路径保存在以下位置：

```
%LOCALAPPDATA%\JdkManager\jdk-config.xml
```

配置文件采用 XML 格式，包含：
- 已添加的 JDK 路径列表
- 当前激活的 JAVA_HOME 路径
- 最后修改时间
