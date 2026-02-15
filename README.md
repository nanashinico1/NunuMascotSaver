# NunuMascotSaver

Windows 11 向けヌヌぬいをがたくさん出てくるスクリーンセーバー（`.scr`）です。設定で以下を変更できます。

- キャラ数
- 衝突時の変形（ON/OFF）
- 文字に変形する（ON/OFF、デフォルトOFF）
- 対象の文字列
- 変わる間隔（秒）
- 代わっている時間（秒）
- ランダム時間（%）

## 使い方（開発時）

- `/c`: 設定画面
- `/s`: フルスクリーン実行
- `/p <HWND>`: スクリーンセーバープレビュー

## 配布用 `.scr` 作成

```powershell
.\publish-screensaver.ps1
```

出力先:

- `dist\NunuMascotSaver.scr`

## インストール

1. `NunuMascotSaver.scr` を `%WINDIR%\System32`（必要なら `SysWOW64` も）へコピー
2. Windows の「スクリーンセーバー設定」で `NunuMascotSaver` を選択
3. 「設定」ボタンで各オプション（キャラ数・衝突変形・文字列表示）を変更

設定ファイルは `%AppData%\NunuMascotSaver\settings.json` に保存されます。
