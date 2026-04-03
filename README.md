# Purrfolio

WinUI 3 本地化高净值资产追踪与投资看板（永久组合 + 固定收益专项）。

## 项目结构

- `src/Purrfolio.App`: WinUI 3 UI 层（NavigationView + Mica + MVVM ViewModels/Views）。
- `src/Purrfolio.Core`: 领域模型与财务计算（XIRR、复利预测、永久组合偏离分析）。
- `src/Purrfolio.Infrastructure`: SQLite + EF Core 数据访问与仓储实现。
- `tests/Purrfolio.Core.Tests`: xUnit 财务计算单元测试。

## 已实现的 PRD 关键点

- `AssetViewModel` 通过 `IAsyncEnumerable` 异步读取 SQLite 投资记录。
- 通用 `XirrCalculator` 支持非定期现金流收益率计算（牛顿法 + 二分兜底）。
- 首页采用 `NavigationView`，窗口启用 `MicaBackdrop`，资产列表使用 `ItemsRepeater`。
- 财务计算逻辑提供 xUnit 测试覆盖（XIRR、复利达标预测、配比偏离预警）。

## 本地运行（Windows）

```bash
dotnet restore Purrfolio.sln
dotnet build Purrfolio.sln -c Debug
dotnet test tests/Purrfolio.Core.Tests/Purrfolio.Core.Tests.csproj
```

发布 Native AOT（示例）：

```bash
dotnet publish src/Purrfolio.App/Purrfolio.App.csproj -c Release -r win-x64
```
