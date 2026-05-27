# Godot 资产技能

这是一组可移植的 Codex 技能，用于把 Godot 资产需求整理成可审查、可执行、可验证的工作流。

```text
godot-asset-interview -> godot-asset-review -> godot-asset-execute
```

## 技能

- `godot-asset-interview`：访谈并澄清 Godot 资产需求，生成可审查的资产合约。
- `godot-asset-review`：审查资产合约的完整性、安全性和可执行性，并产出执行计划。
- `godot-asset-execute`：在用户批准或明确接受风险后，通过 Godot AI MCP 执行资产合约。

## 运行依赖

这些技能依赖 Godot AI 提供的 MCP 能力来连接和操作 Godot 编辑器。使用前请先安装并配置 Godot AI：

[https://github.com/hi-godot/godot-ai.git](https://github.com/hi-godot/godot-ai.git)

确认 Godot AI MCP 可用后，再安装本项目中的技能。

## 安装

在本目录运行：

```powershell
.\install.ps1
```

默认情况下，安装脚本会复制技能到 `$env:CODEX_HOME\skills`。如果未设置 `CODEX_HOME`，则复制到 `$HOME\.codex\skills`。

安装到其他目录：

```powershell
.\install.ps1 -Destination "C:\path\to\.codex\skills"
```

预览将要复制的文件：

```powershell
.\install.ps1 -WhatIf
```

## 项目输出

技能会在当前 Godot 项目中写入 `.godot-ai/` 工作流产物：

```text
.godot-ai/
  context/
  interviews/
  contracts/
  reviews/
  execution-plans/
  verification/
```

这些产物用于记录访谈上下文、资产合约、审查结果、执行计划和验证证据。
