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
        const float INPUT_WIDTH = 300.0f;
        const float IMAGE_H = 100.0f;
        const float NO_IMAGE_H = 30.0f;

        // メモのヘッダー部分に表示する文字数
        const int MEMO_HEADER_TEXT_COUNT = 30;

        // ボタンのサイズ
        readonly Vector2 BUTTON_SIZE = new Vector2(INPUT_WIDTH, 20.0f);

        // 有効なウィンドウか？
        bool _bEnableWindow = true;

        // アクティブなウィンドウ化？
        bool _bActive = false;

        // 編集ポップアップ
        bool _bOpenPopup = false;

        // 入力状態にフォーカスする
        // ウィンドウ作成直後の場合、そのフレームでフォーカス命令が機能しないので
        // 2フレーム後にフォーカスするように実装している
        int _setFocusFrame = 0;

        // このフレームの移動
        public Vector2 Move { get; set; } = new Vector2();

        // 最後に表示したときのウィンドウサイズ
        Vector2 _lastSize = new Vector2();
        public Vector2 LastSize => _lastSize;


        // 最後に表示したときの何個分の大きさかの値
        int _lastSizeUnit = 1;
        public int LastSizeUnit => _lastSizeUnit;
        
        Vector2 _parentWindowSize = new Vector2();
        Vector4 _bgColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        // 各種UIイベント通知
        public event Action<string> OnChangeText;
        public event Action<Uri> OnSelectURL;
        public event Action<string> OnSelectPath;
        public event Action<DateTime, bool> OnToggleDateTime;
        public event Action OnChangeAndEditEnd;
        public event Action<string> OnAddTag;
        public event Action OnClose;

        /// <summary>
        /// 表示
        /// </summary>
        /// <param name="fusen"></param>
        /// <param name="model"></param>
        public void Disp(Fusen fusen)
        {
            _bActive = false;
            _parentWindowSize = ImGui.GetWindowSize();

            var model = fusen.Model;
            var text = model.Text;
            var windowsFlags = ImGuiWindowFlags.AlwaysAutoResize 
                | ImGuiWindowFlags.NoCollapse 
                | ImGuiWindowFlags.NoFocusOnAppearing 
                | ImGuiWindowFlags.NoTitleBar;

            if (!_bEnableWindow)
            {
                return;
            }

            // 背景色
            ImGui.PushStyleColor(ImGuiCol.WindowBg, _bgColor);

            // ウィンドウサイズの設定
            var w = INPUT_WIDTH + 30;
            var h = 500;
            ImGui.SetNextWindowSizeConstraints(new Vector2(w, 100), new Vector2(w, h));

            // 大量のスペースはタイトルバーにIDを表示しないため
            // 改行は ini ファイルが機能しなくなるため入れてはいけない
            //if (ImGui.Begin($"{model.Id.ToString()}".PadLeft('_'), ref _bEnableWindow, windowsFlags))
            if (ImGui.Begin(model.Id.ToString(), ref _bEnableWindow, windowsFlags))
            {
                // ウィンドウの移動
                ImGui.SetWindowPos(ImGui.GetWindowPos() + Move);

                // 移動量を0に
                Move = Vector2.Zero;

                DispContext(model);

                DispEditPopup(model);
                DispText(fusen, w);

                ImGui.Spacing();
                DispDateButtons(fusen, w);
                DispDateTimeButtons(fusen, w);

                ImGui.Spacing();
                DispURLButtons(fusen, w);

                ImGui.Spacing();
                DispPathButtons(fusen, w);

                ImGui.Spacing();
                DispTags(fusen);
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
            _lastSize = ImGui.GetWindowSize();
            // 最後に描画したウィンドウがなんブロックだったかを保存
            _lastSizeUnit = (int)_lastSize.X / (int)INPUT_WIDTH;
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
        void DispContext(FusenModel model)
        {
            if (!ImGui.BeginPopupContextWindow())
            {
                return;
            }
            if (ImGui.BeginMenu("タグ"))
            {
                string text = "";
                if (ImGui.InputText("追加", ref text, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    OnAddTag?.Invoke(text);
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("色"))
            {
                if (ImGui.MenuItem("赤")) { _bgColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f); }
                if (ImGui.MenuItem("緑")) { _bgColor = new Vector4(0.0f, 1.0f, 0.0f, 1.0f); }
                if (ImGui.ColorPicker4("BgColor", ref _bgColor))
                {
                }
                ImGui.EndMenu();
            }
            if (ImGui.MenuItem("削除"))
            {
                _bEnableWindow = false;
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
            var w = _parentWindowSize.X;// * 0.8f;
            var h = 300;// * 0.8f;

            var text = model.Text;
            // 本文をダブルクリックしたら編集ダイアログを出す
            //if (!ImGui.IsPopupOpen(popupName) && ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            if (!ImGui.IsPopupOpen(EDIT_POPUP_NAME) 
                && ImGui.IsWindowHovered() 
                && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                ImGui.OpenPopup(EDIT_POPUP_NAME);

                // ダイアログの表示位置を設定
                //ImGui.SetNextWindowPos(new Vector2(20));

                // 開いたときにメモ欄にフォーカスを移す
                this.SetFocusInput();

                _bOpenPopup = true;
            }
            if (ImGui.BeginPopupModal(
                EDIT_POPUP_NAME,
                ref _bOpenPopup, 
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.SetWindowSize(new Vector2(w, h));

                // フォーカス設定フラグが立っていたら、テキストボックスにフォーカスする
                if (0 < _setFocusFrame)
                {
                    _setFocusFrame--;
                    if (_setFocusFrame == 0)
                    {
                        ImGui.SetKeyboardFocusHere(0);
                    }
                }

                // Ctrl+Enter or ESC
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Escape), false)
                    || ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Enter), false))
                {
                    _bOpenPopup = false;
                    OnChangeAndEditEnd?.Invoke();
                }
                else if (ImGui.InputTextMultiline(
                    "",
                    ref text,
                    1024,
                    new Vector2(w, h),
                    ImGuiInputTextFlags.None
                    ))
                {
                    this.OnChangeText?.Invoke(text);
                }


                // フォーカス状態の監視
                //if (ImGui.IsItemDeactivatedAfterEdit())
                //{
                //    OnChangeAndEditEnd?.Invoke();
                //}
                ImGui.EndPopup();
            }
        }

        /// <summary>
        /// 本文の表示
        /// </summary>
        /// <param name="text"></param>
        void DispText(Fusen fusen, float width)
        {
            var model = fusen.Model;
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
            ImGui.TextWrapped(fusen.DisplayText);
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
                }
                else
                {
                    var h = NO_IMAGE_H;
                    var w = NO_IMAGE_H;

                    if (ImGui.Button("開く", new Vector2(w, h)))
                    {
                        isClicked |= true;
                    }

                    ImGui.SameLine();
                    ImGui.TextWrapped(title);
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
                    var h = NO_IMAGE_H;
                    var w = windowWidth;

                    var path = pair.Key.LocalPath;
                    if (System.IO.Directory.Exists(path))
                    {
                        //var dirName = System.IO.Path.GetDirectoryName(path);
                        var dirName = System.IO.Path.GetFileName(path);
                        if (ImGui.Button(dirName, new Vector2(w, h)))
                        {
                            isClicked |= true;
                        }
                    }
                    else if (System.IO.File.Exists(path))
                    {
                        if (ImGui.Button(System.IO.Path.GetFileName(path), new Vector2(w, h)))
                        {
                            isClicked |= true;
                        }
                    }
                    else
                    {
                        if (ImGui.Button("開く", new Vector2(w, h)))
                        {
                            //isClicked |= true;
                        }
                    }
                    //ImGui.SameLine();

                    //ImGui.TextWrapped(title);
                    //isClicked |= (ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left));
                }

                if (isClicked)
                {
                    this.OnSelectPath?.Invoke(pair.Key.LocalPath);
                }
            }
        }

        void DispTags(Fusen fusen)
        {
            for (int i = 0; i < fusen.Model.Tags.Count; ++i)
            {
                ImGui.SmallButton($"{fusen.Model.Tags[i]}");
                if (i < fusen.Model.Tags.Count - 1)
                {
                    ImGui.SameLine();
                }
            }
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
