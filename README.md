# Purrfolio

WinUI 3 本地化高净值资产追踪与投资看板（永久组合 + 固定收益专项）。

## 产品功能总览

### 1. 资产概览仪表盘

- 首页展示总资产净值，并按股票、黄金、政府债券、现金四类汇总。
- 首页新增资产环形图（Donut），直观展示各资产类别权重。
- 首页新增净值曲线，并叠加沪深300/CPI离线归一化对比线。
- 基于永久组合目标配比（默认各 25%）自动计算偏离度，偏离超过 5% 时显示预警。
- 提供“百万目标进度”展示，包括当前达成百分比、预计达标时间和关键年龄倒计时。

### 2. 固定收益专项管理

- 支持政府债券扩展字段：票面利率、派息频率、到期日、应计利息、特别国债标记。
- 支持债券仓 XIRR 计算，用于估算债券现金流内部收益率。
- 新增固定收益收益率看板：支持“单只债券维度 XIRR”与“分批次维度 XIRR”独立展示。
- 新增派息日历：自动生成未来 18 个月派息/到期兑付事件，并支持推送至 Windows 通知中心。
- 提供手动录入页面，新增记录直接写入本地 SQLite 数据库。

### 3. 财富目标与复利预测

- 支持按“当前净值 + 月投入 + 预期年化收益率”计算达成目标所需月份。
- 输出预测达标日期，辅助长期投资计划管理。
- 提供可交互复利模拟器：输入参数后生成月度资产轨迹，展示逐月余额变化。

## 项目结构

- `src/Purrfolio.App`: WinUI 3 UI 层（NavigationView + Mica + MVVM ViewModels/Views）。
- `src/Purrfolio.Core`: 领域模型与财务计算（XIRR、复利预测、永久组合偏离分析）。
- `src/Purrfolio.Infrastructure`: SQLite + EF Core 数据访问与仓储实现。
- `tests/Purrfolio.Core.Tests`: xUnit 财务计算单元测试。

## 已实现的 PRD 关键点

- `AssetViewModel` 通过 `IAsyncEnumerable` 异步读取 SQLite 投资记录。
- 通用 `XirrCalculator` 支持非定期现金流收益率计算（牛顿法 + 二分兜底）。
- 首页采用 `NavigationView`，窗口启用 `MicaBackdrop`，并已实现资产环形图和净值/基准对比曲线。
- 固定收益页新增 XIRR 看板：支持债仓总览、单只债券维度、分批次维度三层展示。
- 新增“手动录入”页面，支持普通资产与政府债券（含票息/频率/到期日）写入本地 SQLite，并支持特别国债模板与记录删除。
- 新增可交互复利模拟器页面（目标达成推演 + 月度轨迹）。
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
