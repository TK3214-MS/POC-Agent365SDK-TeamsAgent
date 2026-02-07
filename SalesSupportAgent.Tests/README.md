# SalesSupportAgent Tests

営業支援エージェントのユニットテストプロジェクトです。

## 📦 テスト構成

```
SalesSupportAgent.Tests/
├── Bot/
│   └── AdaptiveCardHelperTests.cs      # Adaptive Card生成テスト
├── Configuration/
│   ├── M365SettingsTests.cs            # M365設定テスト
│   └── BotSettingsTests.cs             # Bot設定テスト
├── Models/
│   └── SalesSummaryModelsTests.cs      # モデルテスト
└── Services/
    └── SalesAgentTests.cs              # エージェントテスト
```

## 🚀 テストの実行

### すべてのテストを実行

```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent
dotnet test
```

### 詳細な出力で実行

```bash
dotnet test --verbosity detailed
```

### カバレッジレポート付きで実行

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### 特定のテストのみ実行

```bash
# 特定のテストクラス
dotnet test --filter FullyQualifiedName~AdaptiveCardHelperTests

# 特定のテストメソッド
dotnet test --filter FullyQualifiedName~CreateSalesSummaryCard_ShouldReturnValidAttachment
```

## 🧪 テストフレームワーク

- **xUnit**: テストフレームワーク
- **Moq**: モッキングライブラリ
- **coverlet**: コードカバレッジ

## 📊 テストカバレッジ

現在のテストカバレッジ対象:

- ✅ **Adaptive Card Helper** - カード生成ロジック
- ✅ **Configuration Settings** - 設定クラスのバリデーション
- ✅ **Models** - リクエスト/レスポンスモデル
- ⚠️ **Sales Agent** - 基本構造のみ（モック複雑化のため部分的）

## 🔧 テストの追加

新しいテストを追加する場合:

1. 適切なフォルダにテストファイルを作成
2. `[Fact]` または `[Theory]` 属性を使用
3. AAA パターン（Arrange, Act, Assert）に従う

### テンプレート

```csharp
using Xunit;

namespace SalesSupportAgent.Tests.YourNamespace;

public class YourClassTests
{
    [Fact]
    public void YourMethod_ShouldDoSomething_WhenCondition()
    {
        // Arrange
        var sut = new YourClass();

        // Act
        var result = sut.YourMethod();

        // Assert
        Assert.NotNull(result);
    }
}
```

## 🎯 今後の拡張

- [ ] MCPツール（Outlook, SharePoint, Teams）の統合テスト
- [ ] エンドツーエンドテスト（Bot Framework Emulator使用）
- [ ] パフォーマンステスト
- [ ] セキュリティテスト（認証・認可）

## 📚 参考リンク

- [xUnit ドキュメント](https://xunit.net/)
- [Moq ドキュメント](https://github.com/moq/moq4)
- [.NET テストのベストプラクティス](https://learn.microsoft.com/ja-jp/dotnet/core/testing/unit-testing-best-practices)
