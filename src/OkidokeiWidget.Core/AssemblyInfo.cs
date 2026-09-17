using System.Runtime.Versioning;

// 対象 OS は Windows 11 のみ (plan.md の Target Platform)。CA1416 (プラットフォーム互換性警告) を
// P/Invoke・COM 相互運用のたびに抑制する属性を書かずに済ませるため、アセンブリ全体に宣言する。
[assembly: SupportedOSPlatform("windows")]
