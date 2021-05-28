using System;
using System.Collections.Generic;
using ImGuiNET;

namespace Clip_it
{
    /// <summary>
    /// 付箋表示
    /// </summary>
    class FusenView
    {
        public bool visible = true;
        public bool Visible { get; set; } = true;

        public event Action<string> OnChangeText;
        public event Action<string> OnSelectURL;
        public event Action<string> OnSelectPath;
        public event Action OnClose;

        /// <summary>
        /// 表示
        /// </summary>
        /// <param name="fusen"></param>
        /// <param name="model"></param>
        public void Disp(Fusen fusen, FusenModel model)
        {
            if (!visible)
            {
                return;
            }

            var text = model.Text;
            var windowsFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize;
            ImGui.Begin(model.Id.ToString(), ref visible, windowsFlags);

            DispText(text);

            ImGui.Spacing();
            DispURLButtons(fusen);

            ImGui.Spacing();
            DispPathButtons(fusen);

            ImGui.Spacing();
            DispDateButtons(fusen);

            ImGui.Spacing();
            DispCloseButton(fusen);

            ImGui.End();

        }

        /// <summary>
        /// 本文の表示
        /// </summary>
        /// <param name="text"></param>
        void DispText(string text)
        {
            var style = ImGui.GetStyle();
            var innerSpace = style.ItemInnerSpacing;
            var itemSpace = style.ItemSpacing;
            var w = (ImGui.CalcTextSize(text).X * 1.0f) + (innerSpace.X * 2.0f) + (itemSpace.X * 2.0f);
            var h = (ImGui.CalcTextSize(text).Y * 1.0f) + (innerSpace.Y * 2.0f) + (itemSpace.Y * 2.0f) + 20.0f;

            if (ImGui.InputTextMultiline("", ref text, 1024, new System.Numerics.Vector2(w, h), ImGuiInputTextFlags.None))
            {
                this.OnChangeText?.Invoke(text);
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
                // タイトル
                if (ImGui.Button(pair.Value))
                {
                    this.OnSelectURL?.Invoke(pair.Key);
                }
                ImGui.SameLine();
                // URL
                ImGui.LabelText("", pair.Key);
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
                // タイトル
                if (ImGui.Button(pair.Value))
                {
                    this.OnSelectPath?.Invoke(pair.Key);
                }
                ImGui.SameLine();
                // パス
                ImGui.LabelText("", pair.Key);
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
            if (ImGui.Button("X"))
            {
                visible = false;
                OnClose?.Invoke();
            }
        }
    };
}
