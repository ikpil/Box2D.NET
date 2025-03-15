 
# Box2D.NET

*Box2D.NET is C# Box2D, a port of [Box2D](https://github.com/erincatto/box2d) to the C# language.*  
*If you'd like to support the project, we'd appreciate starring(⭐) our repos on Github for more visibility.*

---

![GitHub License](https://img.shields.io/github/license/ikpil/Box2D.NET?style=for-the-badge)
![Languages](https://img.shields.io/github/languages/top/ikpil/Box2D.NET?style=for-the-badge)
![GitHub repo size](https://img.shields.io/github/repo-size/ikpil/Box2D.NET?style=for-the-badge)
[![GitHub Repo stars](https://img.shields.io/github/stars/ikpil/Box2D.NET?style=for-the-badge&logo=github)](https://github.com/ikpil/Box2D.NET)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ikpil/Box2D.NET/dotnet.yml?style=for-the-badge&logo=github)](https://github.com/ikpil/Box2D.NET/actions/workflows/dotnet.yml)
[![CodeQL Advanced](https://img.shields.io/github/actions/workflow/status/ikpil/Box2D.NET/codeql.yml?style=for-the-badge&logo=github&label=CODEQL)](https://github.com/ikpil/Box2D.NET/actions/workflows/codeql.yml)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ikpil/Box2D.NET?style=for-the-badge&logo=github)](https://github.com/ikpil/Box2D.NET/commits)
[![GitHub issues](https://img.shields.io/github/issues-raw/ikpil/Box2D.NET?style=for-the-badge&logo=github&color=44cc11)](https://github.com/ikpil/Box2D.NET/issues)
[![GitHub closed issues](https://img.shields.io/github/issues-closed-raw/ikpil/Box2D.NET?style=for-the-badge&logo=github&color=a371f7)](https://github.com/ikpil/Box2D.NET/issues)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/ikpil?style=for-the-badge&logo=GitHub-Sponsors&link=https%3A%2F%2Fgithub.com%2Fsponsors%2Fikpil)](https://github.com/sponsors/ikpil)

---

[![demo](https://github.com/ikpil/ikpil/blob/main/img/423102962-0dc4a92b-0f1a-407d-9e65-fc507e09962b.gif?raw=true)](https://github.com/ikpil/Box2D.NET)

---

## 🚀 Features

Box2D.NET is divided into multiple modules, each contained in its own folder:

  - [Box2D.NET.Memory](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET.Memory) : A module providing efficient structures and APIs for memory and array management. 
  - [Box2D.NET](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET) : A C# port of the Box2D physics engine for 2D physics simulations.
  - [Box2D.NET.Shared](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET.Shared) : Shared code and utilities for Box2D.NET, for use in sample projects and by library users.
  - [Box2D.NET.Samples](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET.Samples) : Sample projects to showcase the features of Box2D.NET
  - [Box2D.NET.Test](https://github.com/ikpil/Box2D.NET/tree/main/test/Box2D.NET.Test) : Unit tests for Box2D.NET.
  - [Box2D.NET.Samples.Test](https://github.com/ikpil/Box2D.NET/tree/main/test/Box2D.NET.Samples.Test) : Unit tests for Box2D.NET.Samples

## ⚡ Getting Started

- To verify the run for all modules, run [Box2D.NET.Samples](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET.Samples/Box2D.NET.Samples.csproj)
    - on the Windows platform, you need to install the redistributable package
    - [Microsoft Visual C++ Redistributable Package](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)

#### ▶️ Running With Command Prompt

```shell
dotnet run --project src/Box2D.NET.Samples --framework net9.0 -c Release
```

## 🛠️ Integration

There are a few ways to integrate [Box2D.NET](https://github.com/ikpil/DotRecast/tree/main/src/Box2D.NET) and [Box2D.NET.Memory](https://github.com/ikpil/Box2D.NET/tree/main/src/Box2D.NET.Memory) into your project.
Source integration is the most popular and most flexible. Additionally, NuGet installation will also be supported in the future.

## 📚 Documentation & Links

- DotRecast Links
    - [Box2D.NET/issues](https://github.com/ikpil/Box2D.NET/issues)

- Official Links
    - [box2d/issue](https://github.com/erincatto/box2d/issues)
    - [box2d.org](https://box2d.org)
