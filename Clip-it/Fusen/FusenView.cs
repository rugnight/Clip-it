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
        public const string EDIT_POPUP_NAME = "編集";
        // 本文入力の幅
        const float INPUT_WIDTH = 600.0f;
        const float IMAGE_H = 150.0f;

        // メモのヘッダー部分に表示する文字数
        const int MEMO_HEADER_TEXT_COUNT = 30;

        // ボタンのサイズ
        readonly Vector2 BUTTON_SIZE = new Vector2(INPUT_WIDTH, 20.0f);

        // 有効なウィンドウか？
        bool _bEnableWindow = true;

        // アクティブなウィンドウ化？
        bool _bActive = false;

        // 入力状態にフォーカスする
        // ウィンドウ作成直後の場合、そのフレームでフォーカス命令が機能しないので
        // 2フレーム後にフォーカスするように実装している
        int _setFocusFrame = 0;

        // このフレームの移動
        public Vector2 Move { get; set; } = new Vector2();

        // 最後に表示したときのウィンドウサイズ
        Vector2 lastSize = new Vector2();
        public Vector2 LastSize => lastSize;


        // 最後に表示したときの何個分の大きさかの値
        int lastSizeUnit = 1;
        public int LastSizeUnit => lastSizeUnit;
        

        // 各種UIイベント通知
        public event Action<string> OnChangeText;
        public event Action<Uri> OnSelectURL;
        public event Action<string> OnSelectPath;
        public event Action<DateTime, bool> OnToggleDateTime;
        public event Action OnChangeAndEditEnd;
        public event Action OnClose;

        Vector4 bgColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        /// 表示
        /// </summary>
        /// <param name="fusen"></param>
        /// <param name="model"></param>
        public void Disp(Fusen fusen)
        {
            _bActive = false;

            var model = fusen.Model;
            var text = model.Text;
            var windowsFlags = ImGuiWindowFlags.AlwaysAutoResize 
                | ImGuiWindowFlags.NoCollapse 
                | ImGuiWindowFlags.NoFocusOnAppearing 
                | ImGuiWindowFlags.NoDecoration;

            if (!_bEnableWindow)
            {
                return;
            }

            // 背景色
            ImGui.PushStyleColor(ImGuiCol.WindowBg, bgColor);

            // ウィンドウサイズの設定
            var w = INPUT_WIDTH + 30;
            ImGui.SetNextWindowSizeConstraints(new Vector2(w, 100), new Vector2(w, float.MaxValue));

            // 大量のスペースはタイトルバーにIDを表示しないため
            // 改行は ini ファイルが機能しなくなるため入れてはいけない
            //if (ImGui.Begin($"{model.Id.ToString()}".PadLeft('_'), ref _bEnableWindow, windowsFlags))
            if (ImGui.Begin($"{model.Id.ToString()}".PadLeft('_'), ref _bEnableWindow, windowsFlags))
            {
                // ウィンドウの移動
                ImGui.SetWindowPos(ImGui.GetWindowPos() + Move);
                // 移動量を0に
                Move = Vector2.Zero;

                DispContext();


                DispEditPopup(model);
                DispText(model, w);


                ImGui.Spacing();
                DispDateButtons(fusen, w);
                DispDateTimeButtons(fusen, w);

                ImGui.Spacing();
                DispURLButtons(fusen, w);

                ImGui.Spacing();
                DispPathButtons(fusen, w);

                ImGui.Spacing();
                //DispTags(fusen);

                //DispImages(fusen, w);
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
            // 最後に描画したウィンドウがなんブロックだったかを保存
            lastSizeUnit = (int)lastSize.X / (int)INPUT_WIDTH;
            _bActive = ImGui.IsWindowFocused();

            ImGui.End();
            ImGui.PopStyleColor();
        }

        /// <summary>
        /// この伏線がアクティブか？
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            return _bActive;
        }

        /// <summary>
        /// 入力欄にフォーカスを与える
        /// </summary>
        public void SetFocusInput()
        {
            _setFocusFrame = 2;
        }

        /// <summary>
        /// コンテキストメニューを表示
        /// </summary>
        void DispContext()
        {
            if (!ImGui.BeginPopupContextWindow())
            {
                return;
            }
            if (ImGui.MenuItem("削除"))
            {
                _bEnableWindow = false;
            }
            if (ImGui.BeginMenu("色"))
            {
                if (ImGui.MenuItem("赤")) { bgColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f); }
                if (ImGui.MenuItem("緑")) { bgColor = new Vector4(0.0f, 1.0f, 0.0f, 1.0f); }
                if (ImGui.ColorPicker4("BgColor", ref bgColor))
                {
                }
                ImGui.EndMenu();
            }
            ImGui.EndPopup();
        }

        /// <summary>
        /// 編集ダイアログの表示
        /// </summary>
        /// <param name="model"></param>
        /// <param name="width"></param>
        void DispEditPopup(FusenModel model)
        {
            var w = 1000;
            var h = 500;

            var text = model.Text;
            // 本文をダブルクリックしたら編集ダイアログを出す
            //if (!ImGui.IsPopupOpen(popupName) && ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            if (!ImGui.IsPopupOpen(EDIT_POPUP_NAME) && ImGui.IsWindowHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                ImGui.OpenPopup(EDIT_POPUP_NAME);
                // ダイアログの表示位置を設定
                ImGui.SetNextWindowPos(new Vector2(20));
                // 開いたときにメモ欄にフォーカスを移す
                this.SetFocusInput();
            }
            if (ImGui.BeginPopupModal(EDIT_POPUP_NAME))
            {
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

                if (ImGui.Button("閉じる"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        /// <summary>
        /// 本文の表示
        /// </summary>
        /// <param name="text"></param>
        void DispText(FusenModel model, float width)
        {
            var text = model.Text;
            var flags = ImGuiTreeNodeFlags.None;
            // 初回起動時に前回の開閉状態を再現する
            //if (model.OpenedText) 
            {
                flags |= ImGuiTreeNodeFlags.DefaultOpen;
            }

            // ヘッダのタイトルは空文字だとテキストボックス編集ができなくなるので
            // 内容が空のときは空白を入れておく
            var title = (0 < text.Length) ? text.Substring(0, Math.Min(text.Length, MEMO_HEADER_TEXT_COUNT)) : " ";
            ImGui.TextWrapped(text);
        }

        /// <summary>
        /// URLボタンの表示
        /// </summary>
        /// <param name="fusen"></param>
        void DispURLButtons(Fusen fusen, float windowWidth)
        {
            if (fusen.Urls.Count == 0)
            {
                return;
            }

            //bool bOpen = ImGui.CollapsingHeader("URL", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.None);
            //if (!bOpen)
            //{
            //    return;
            //}

            // URLボタン
            foreach (var pair in fusen.Urls)
            {
                var width = windowWidth;
                var title = string.IsNullOrEmpty(pair.Value.Title) ? pair.Value.Uri.AbsoluteUri : pair.Value.Title;
                var isClicked = false;

                TextureInfo texInfo;
                if (fusen.Images.TryGetValue(pair.Value.OgImageUrl, out texInfo))
                {

                    var h = IMAGE_H;
                    var w = texInfo.texture.Width * (h / texInfo.texture.Height);

                    if (ImGui.ImageButton(texInfo.texId, new Vector2(w, h)))
                    {
                        isClicked |= true;
                    }
                    ImGui.SameLine();

                    ImGui.TextWrapped(title);
                    isClicked |= (ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left));
                }
                else
                {
                    var h = IMAGE_H / 3;
                    var w = IMAGE_H / 3;

                    if (ImGui.Button("開く", new Vector2(w, h)))
                    {
                        isClicked |= true;
                    }

                    ImGui.SameLine();
                    ImGui.TextWrapped(title);
                    isClicked |= (ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left));
                }

                if (isClicked)
                {
                    this.OnSelectURL?.Invoke(pair.Value.Uri);
                }
            }
        }

        /// <summary>
        /// パスボタンを表示
        /// </summary>
        /// <param name="fusen"></param>
        void DispPathButtons(Fusen fusen, float windowWidth)
        {
            if (fusen.Paths.Count == 0)
            {
                return;
            }

            foreach (var pair in fusen.Paths)
            {
                var width = windowWidth;
                //var title = string.IsNullOrEmpty(pair.Value.Title) ? pair.Value.Uri.AbsoluteUri : pair.Value.Title;
                var title = string.IsNullOrEmpty(pair.Value) ? pair.Key.LocalPath : pair.Value;
                var isClicked = false;

                TextureInfo texInfo;
                if (fusen.Images.TryGetValue(pair.Key.AbsoluteUri, out texInfo))
                {

                    var h = IMAGE_H;
                    var w = texInfo.texture.Width * (h / texInfo.texture.Height);

                    if (ImGui.ImageButton(texInfo.texId, new Vector2(w, h)))
                    {
                        isClicked |= true;
                    }
                    ImGui.SameLine();

                    ImGui.TextWrapped(title);
                    isClicked |= (ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left));
                }
                else
                {
                    var h = IMAGE_H / 3;
                    var w = IMAGE_H / 3;

                    if (System.IO.File.Exists(pair.Key.LocalPath) || System.IO.Directory.Exists(pair.Key.LocalPath))
                    {
                        if (ImGui.Button("開く", new Vector2(w, h)))
                        {
                            isClicked |= true;
                        }
                    }
                    else
                    {
                        if (ImGui.InvisibleButton("開く", new Vector2(w, h)))
                        {
                            //isClicked |= true;
                        }
                    }
                    ImGui.SameLine();

                    ImGui.TextWrapped(title);
                    isClicked |= (ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left));
                }

                if (isClicked)
                {
                    this.OnSelectPath?.Invoke(pair.Key.LocalPath);
                }
            }
        }

        void DispTags(Fusen fusen)
        {
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            ImGui.SmallButton($"{ImGui.GetColumnWidth()}");
            ImGui.SameLine();
            

        }

        /// <summary>
        /// 日付ボタンを表示
        /// </summary>
        /// <param name="fusen"></param>
        void DispDateButtons(Fusen fusen, float width)
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
        /// 日時ボタンを表示
        /// </summary>
        /// <param name="fusen"></param>
        void DispDateTimeButtons(Fusen fusen, float width)
        {
            var changed = new List<DateTime>();
            foreach (var pair in fusen.DateTimes)
            {
                var dt = pair.Key;
                var dateText = $"{dt.ToShortDateString()} {dt.ToShortTimeString()}";

                if (dt < DateTime.Now)
                {
                    ImGui.Text(dateText);
                }
                else
                {
                    // 日付
                    bool bOn = pair.Value;
                    if (ImGui.Checkbox(dateText, ref bOn))
                    {
                        changed.Add(pair.Key);
                    }
                }
            }
            foreach (var key in changed)
            {
                var bOn = !fusen.DateTimes[key];
                fusen.DateTimes[key] = bOn;
                OnToggleDateTime?.Invoke(key, bOn);
            }
        }

        /// <summary>
        /// 画像の表示
        /// </summary>
        /// <param name="fusen"></param>
        void DispImages(Fusen fusen, float width)
        {
            if (fusen.Images.Count == 0)
            {
                return;
            }

            bool bOpen = ImGui.CollapsingHeader("画像", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.None);
            if (!bOpen)
            {
                return;
            }

            //if (2 <= fusen.Images.Count)
            {
                //width = (width / 2) - 5;
            }
            //bool sameLine = true;
            foreach (var texInfo in fusen.Images.Values)
            {
                //var height = Math.Min(width, texInfo.texture.Height) * (width / texInfo.texture.Width);
                var h = 150.0f;
                var w = texInfo.texture.Width * (h / texInfo.texture.Height);
                ImGui.Image(texInfo.texId, new Vector2(w, h));
                //if (sameLine)
                //{
                //    ImGui.SameLine();
                //}
                //sameLine = !sameLine;
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
