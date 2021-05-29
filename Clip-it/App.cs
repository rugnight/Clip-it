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
        public const string AppName = "Clip-it";

        FusenDB db = new FusenDB();
        List<Fusen> fusens = new List<Fusen>();
        Vector2 _windowSize;

        string DataDir { get => System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName); }
        string DbFilePath { get => System.IO.Path.Combine(DataDir, "fusen.db"); }
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

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.MenuItem("New"))
                {
                    fusens.Add(new Fusen(new FusenModel()));
                }
                if (ImGui.MenuItem("FreeLayout"))
                {
                }
                if (ImGui.MenuItem("Scedules"))
                {
                }
                if (ImGui.MenuItem("Save"))
                {
                    // imgui.ini保存
                    ImGui.SaveIniSettingsToDisk(IniFilePath);
                }
                if (ImGui.MenuItem("Load"))
                {
                    // imgui.iniロード
                    ImGui.LoadIniSettingsFromDisk(IniFilePath);
                }
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

            foreach (var fusen in fusens)
            {
                fusen.Update();
            }

            //ImGui.End();

            return true;
        }

        public void OnDropItem(string dropFile)
        {
            var model = new FusenModel();
            model.Text = dropFile;
            fusens.Add(new Fusen(model));
        }
    }
}
