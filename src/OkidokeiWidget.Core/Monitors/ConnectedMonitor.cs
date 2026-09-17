namespace OkidokeiWidget.Core.Monitors;

/// <summary>
/// 現在接続されているモニター 1 台分の情報。<see cref="Identifier"/> は EDID 由来の安定した ID
/// (<see cref="MonitorIdentifier"/> 参照)。作業領域の座標は仮想デスクトップ上の絶対座標。
/// <see cref="DisplayNumber"/> は Win32 のアダプターデバイス名 (`\\.\DISPLAY<n>`) 由来の番号で、
/// Windows の「設定 > システム > ディスプレイ」に表示される番号と一致する (UI 表示用)。
/// </summary>
public sealed record ConnectedMonitor(
    string Identifier,
    bool IsPrimary,
    int DisplayNumber,
    int WorkAreaX,
    int WorkAreaY,
    int WorkAreaWidth,
    int WorkAreaHeight);
