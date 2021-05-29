using ImGuiNET;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using System.Numerics;

namespace Clip_it
{
    class App
    {
        // アプリ名
        public const string AppName = "Clip-it";

        FusenDB db = new FusenDB();
        List<Fusen> fusens = new List<Fusen>();
        Vector2 _windowSize;

        // データ保存先ディレクトリ
        string DataDir { get => System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName); }

        // DB情報保存ファイル
        string DbFilePath { get => System.IO.Path.Combine(DataDir, "fusen.db"); }

        // IMGUIの保存情報ファイル
        string IniFilePath { get => System.IO.Path.Combine(DataDir, "imgui.ini"); }

        public void Initialize(Vector2 windowSize)
        {
            _windowSize = new Vector2(windowSize.X, 1.0f);

            // アプリケーションフォルダの作成
            System.IO.Directory.CreateDirectory(DataDir);

            // DB開く
            db.Open(DbFilePath);
            db.CreateDB();

            // DBの付箋から読み込み
            foreach (var model in db.Load())
            {
                fusens.Add(new Fusen(model));
            }
            // imgui.iniロード
            ImGui.LoadIniSettingsFromDisk(IniFilePath);
        }

        public void Terminate()
        {
            // DB保存
            db.Save(fusens.Select((fusen) => fusen.Model).ToList());
            // imgui.ini保存
            ImGui.SaveIniSettingsToDisk(IniFilePath);
        }

        public bool Update()
        {
            ImGui.SetNextWindowPos(new Vector2(0.0f, 0.0f));
            ImGui.SetNextWindowSize(_windowSize);
            //ImGui.Begin("Fusen Manager", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

            bool bAlighn = false;
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.MenuItem("New"))
                {
                    fusens.Add(new Fusen(new FusenModel()));
                }
                if (ImGui.MenuItem("Alighn"))
                {
                    bAlighn = true;
                }
                //if (ImGui.MenuItem("Save"))
                //{
                //    // imgui.ini保存
                //    ImGui.SaveIniSettingsToDisk(IniFilePath);
                //}
                //if (ImGui.MenuItem("Load"))
                //{
                //    // imgui.iniロード
                //    ImGui.LoadIniSettingsFromDisk(IniFilePath);
                //}
                if (ImGui.MenuItem("Quit"))
                {
                    return false;
                }
            }

            var io = ImGui.GetIO();
            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.V, false))
            {
                // CTRL+Vで付箋作成
                if (ImGui.IsWindowFocused())
                {
                    var clipText = ImGui.GetClipboardText();

                    if (clipText != null)
                    {
                        var model = new FusenModel();
                        model.Text = clipText;
                        fusens.Add(new Fusen(model));
                    }
                }
            }

            // 付箋を描画
            if (bAlighn)
            {
                FusenSortUpdate();
            }
            else
            {
                FusenUpdate();
            }

            //ImGui.Begin("Debug");
            //ImGui.LogButtons();
            //ImGui.LogFinish();
            //ImGui.End();


            //ImGui.End();

            return true;
        }

        /// <summary>
        /// 付箋の更新・描画
        /// </summary>
        void FusenUpdate()
        {
            foreach (var fusen in fusens)
            {
                fusen.Update();
            }
        }

        /// <summary>
        /// 付箋を並び替え描画
        /// </summary>
        void FusenSortUpdate()
        {
            int cnt = 0;
            int colNum = 3;
            float padX = 10;
            float padY = 10;
            float x = padX;
            float[] colStartY = Enumerable.Repeat<float>(padY + 20, colNum).ToArray();
            foreach (var fusen in fusens)
            {
                int rowIdx = (cnt / colNum);
                int colIdx = (cnt % colNum);
                float y = colStartY[colIdx];
                ImGui.SetNextWindowPos(new Vector2(x, y));

                fusen.Update();

                if (colIdx == colNum - 1)
                {
                    x = padX;
                }
                else
                {
                    x += fusen.LastSize.X + padX;
                }
                colStartY[colIdx] += fusen.LastSize.Y + padY;
                cnt++;
            }
        }

        public void OnDropItem(string dropFile)
        {
            var model = new FusenModel();
            model.Text = dropFile;
            fusens.Add(new Fusen(model));
        }
    }
}
