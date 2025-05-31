![Box2D Logo](https://raw.githubusercontent.com/ikpil/ikpil/refs/heads/main/img/box2d_logo.svg)

# Box2D.NET

*Box2D.NET is a C# port of the [Box2D](https://github.com/erincatto/box2d) physics engine.*  
*If you'd like to support the project, we'd appreciate starring(⭐) our repos on Github for more visibility.*

---

[![GitHub License](https://img.shields.io/github/license/ikpil/Box2D.NET?style=for-the-badge)](https://github.com/ikpil/Box2D.NET/blob/main/LICENSE)
![Languages](https://img.shields.io/github/languages/top/ikpil/Box2D.NET?style=for-the-badge)
![GitHub repo size](https://img.shields.io/github/repo-size/ikpil/Box2D.NET?style=for-the-badge)
[![GitHub Repo stars](https://img.shields.io/github/stars/ikpil/Box2D.NET?style=for-the-badge&logo=github)](https://github.com/ikpil/Box2D.NET)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ikpil/Box2D.NET/dotnet.yml?style=for-the-badge&logo=github)](https://github.com/ikpil/Box2D.NET/actions/workflows/dotnet.yml)
[![CodeQL Advanced](https://img.shields.io/github/actions/workflow/status/ikpil/Box2D.NET/codeql.yml?style=for-the-badge&logo=github&label=CODEQL)](https://github.com/ikpil/Box2D.NET/actions/workflows/codeql.yml)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ikpil/Box2D.NET?style=for-the-badge&logo=github)](https://github.com/ikpil/Box2D.NET/commits)
[![GitHub issues](https://img.shields.io/github/issues-raw/ikpil/Box2D.NET?style=for-the-badge&logo=github&color=44cc11)](https://github.com/ikpil/Box2D.NET/issues)
[![GitHub closed issues](https://img.shields.io/github/issues-closed-raw/ikpil/Box2D.NET?style=for-the-badge&logo=github&color=a371f7)](https://github.com/ikpil/Box2D.NET/issues)
[![NuGet Version](https://img.shields.io/nuget/vpre/Box2D.NET?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/Box2D.NET)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Box2D.NET?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/Box2D.NET)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/ikpil?style=for-the-badge&logo=GitHub-Sponsors&link=https%3A%2F%2Fgithub.com%2Fsponsors%2Fikpil)](https://github.com/sponsors/ikpil)

---

[![demo](https://raw.githubusercontent.com/ikpil/ikpil/refs/heads/main/img/423227516-755634a3-bdb4-4118-8222-95ceb3856257.gif)](https://www.youtube.com/watch?v=_a1QxD4Al_w)

---

## ✨ Features

- 🌿 Purity - Fully implemented in pure C#.
- 💻 Compatibility - Ensuring seamless integration with the .NET platform and Unity3D.
- 🌍 Cross-Platform Support - Easily integrates with all major platforms, including Linux, Windows, macOS


Box2D.NET is divided into multiple modules, each contained in its own folder:

  - [Box2D.NET](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET) : A C# port of the Box2D physics engine for 2D physics simulations.
  - [Box2D.NET.Shared](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET.Shared) : Shared code and utilities for Box2D.NET, for use in sample projects and by library users.
  - [Box2D.NET.Samples](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET.Samples) : Sample projects to showcase the features of Box2D.NET
  - [Box2D.NET.Test](https://github.com/ikpil/Box2D.NET/tree/main/test/Box2D.NET.Test) : Unit tests for Box2D.NET.

## 🚀 Getting Started

- To verify the run for all modules, run [Box2D.NET.Samples](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET.Samples/Box2D.NET.Samples.csproj)
    - on the Windows platform, you need to install the redistributable package
    - [Microsoft Visual C++ Redistributable Package](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)

#### ▶️ Running With Command Prompt

```shell
dotnet run --project src/Box2D.NET.Samples --framework net9.0 -c Release
```

## 🛠️ Integration

- There are a few ways to integrate [Box2D.NET](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET) into your project.
- Source integration is the most popular and most flexible. Additionally, NuGet installation will also be supported in the future.

## 📚 Documentation & Links

### Box2D.NET Links
 - [Box2D.NET/issues](https://github.com/ikpil/Box2D.NET/issues)

### Original Box2D Links
 - [box2d.org](https://box2d.org)
 - [box2d/issues](https://github.com/erincatto/box2d/issues)
 - [box2d.org/documentation](https://box2d.org/documentation/)

You can use the original Box2D documentation as a reference, since the Box2D.NET API closely mirrors the original implementation. If you are new to Box2D, we recommend starting with the original documentation to learn the basics and core concepts.


Key Naming Conventions:

- Properties and methods: Start with lowercase (matches original Box2D)
- Classes and structs: Start with Uppercase (differs from original Box2D)