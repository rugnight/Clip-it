using System;
using System.Numerics;
using System.Collections.Generic;
using ImGuiNET;

namespace Clip_it
{
    /// <summary>
    /// 付箋表示
    /// </summary>
    class FusenView
    {
        // 本文入力の幅
        const float INPUT_WIDTH = 350.0f;

        // メモのヘッダー部分に表示する文字数
        const int MEMO_HEADER_TEXT_COUNT = 30;

        // ボタンのサイズ
        readonly Vector2 BUTTON_SIZE = new Vector2(INPUT_WIDTH, 20.0f);

        // 有効なウィンドウか？
        bool _bEnableWindow = true;

        // 入力状態にフォーカスする
        // ウィンドウ作成直後の場合、そのフレームでフォーカス命令が機能しないので
        // 2フレーム後にフォーカスするように実装している
        int _setFocusFrame = 0;

        // 最後に表示したときのウィンドウサイズ
        Vector2 lastSize = new Vector2();
        public Vector2 LastSize => lastSize;

        // 各種UIイベント通知
        public event Action<string> OnChangeText;
        public event Action<string> OnSelectURL;
        public event Action<string> OnSelectPath;
        public event Action OnChangeAndEditEnd;
        public event Action OnClose;

        /// <summary>
        /// 表示
        /// </summary>
        /// <param name="fusen"></param>
        /// <param name="model"></param>
        public void Disp(Fusen fusen)
        {
            if (!_bEnableWindow)
            {
                return;
            }

            var model = fusen.Model;
            var text = model.Text;
            var windowsFlags = ImGuiWindowFlags.AlwaysAutoResize;

            // 大量のスペースはタイトルバーにIDを表示しないため
            // 改行は ini ファイルが機能しなくなるため入れてはいけない
            if (ImGui.Begin($"{model.Id.ToString()}".PadLeft('_'), ref _bEnableWindow, windowsFlags))
            {
                DispText(model);

                ImGui.Spacing();
                DispURLButtons(fusen);

                ImGui.Spacing();
                DispPathButtons(fusen);

                ImGui.Spacing();
                DispDateButtons(fusen);
            }
            else
            {
                // 折りたたまれているとき
            }

            // 閉じられた
            if (!_bEnableWindow)
            {
                OnClose?.Invoke();
            }

            // 最後に描画したウィンドウサイズを保存
            lastSize = ImGui.GetWindowSize();

            ImGui.End();
        }

        /// <summary>
        /// 入力欄にフォーカスを与える
        /// </summary>
        public void SetFocusInput()
        {
            _setFocusFrame = 2;
        }

        /// <summary>
        /// 本文の表示
        /// </summary>
        /// <param name="text"></param>
        void DispText(FusenModel model)
        {
            var text = model.Text;
            var flags = ImGuiTreeNodeFlags.None;
            // 初回起動時に前回の開閉状態を再現する
            if (model.OpenedText) 
            {
                flags |= ImGuiTreeNodeFlags.DefaultOpen;
            }

            // ヘッダのタイトルは空文字だとテキストボックス編集ができなくなるので
            // 内容が空のときは空白を入れておく
            var title = (0 < text.Length) ? text.Substring(0, Math.Min(text.Length, MEMO_HEADER_TEXT_COUNT)) : " ";

            // 開閉状態を保存する
            model.OpenedText = ImGui.CollapsingHeader(title, flags);

            // 開いたときにメモ欄にフォーカスを移す
            if (ImGui.IsItemToggledOpen())
            {
                SetFocusInput();
            }
            if (model.OpenedText)
            {
                var style = ImGui.GetStyle();
                var innerSpace = style.ItemInnerSpacing;
                var itemSpace = style.ItemSpacing;
                var w = INPUT_WIDTH;
                var h = (ImGui.CalcTextSize(text).Y * 1.0f) + (innerSpace.Y * 1.0f) + (itemSpace.Y * 1.0f) + 30.0f;

                // フォーカス設定フラグが立っていたら、テキストボックスにフォーカスする
                if (0 < _setFocusFrame)
                {
                    _setFocusFrame--;
                    if (_setFocusFrame == 0)
                    {
                        ImGui.SetKeyboardFocusHere(0);
                    }
                }

                if (ImGui.InputTextMultiline(
                    "",
                    ref text,
                    1024,
                    new System.Numerics.Vector2(w, h),
                    ImGuiInputTextFlags.None
                    ))
                {
                    this.OnChangeText?.Invoke(text);
                }

                // フォーカス状態の監視
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    OnChangeAndEditEnd?.Invoke();
                }
            }
        }

        /// <summary>
        /// URLボタンの表示
        /// </summary>
        /// <param name="fusen"></param>
        void DispURLButtons(Fusen fusen)
        {
            // URLボタン
            foreach (var pair in fusen.Urls)
            {
                var title = string.IsNullOrEmpty(pair.Value) ? pair.Key : pair.Value;

                // タイトル
                if (ImGui.Button(title, BUTTON_SIZE))
                {
                    this.OnSelectURL?.Invoke(pair.Key);
                }
            }
        }

        /// <summary>
        /// パスボタンを表示
        /// </summary>
        /// <param name="fusen"></param>
        void DispPathButtons(Fusen fusen)
        {
            foreach (var pair in fusen.Paths)
            {
                var title = string.IsNullOrEmpty(pair.Value) ? pair.Key : pair.Value;

                // タイトル
                if (ImGui.Button(title, BUTTON_SIZE))
                {
                    this.OnSelectPath?.Invoke(pair.Key);
                }
            }
        }

        /// <summary>
        /// 日付ボタンを表示
        /// </summary>
        /// <param name="fusen"></param>
        void DispDateButtons(Fusen fusen)
        {
            var changed = new List<string>();
            foreach (var pair in fusen.Dates)
            {
                // 日付
                bool bOn = pair.Value;
                if (ImGui.Checkbox(pair.Key, ref bOn))
                {
                    changed.Add(pair.Key);
                }
            }
            foreach (var key in changed)
            {
                fusen.Dates[key] = !fusen.Dates[key];
            }
        }

        /// <summary>
        /// 閉じるボタンの表示
        /// </summary>
        /// <param name="fusen"></param>
        void DispCloseButton(Fusen fusen)
        {
            if (ImGui.Button("X", BUTTON_SIZE))
            {
                OnClose?.Invoke();
            }
        }
    };
}
