﻿using System.Numerics;
using System.Collections.Generic;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;

namespace Clip_it
{
    class Program : IAppEventHandler
    {
        App _app = new App();
        Sdl2Window _window;
        GraphicsDevice _gd;
        CommandList _cl;
        ImGuiController _controller;

        // UI state
        private Vector3 _clearColor = new Vector3(0.2f, 0.2f, 0.2f);

        // エントリポイント
        static void Main(string[] args)
        {
            var main = new Program();
            main.Run();
        }

        // 処理の実行
        void Run()
        { 
            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, App.AppName),
                new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _gd);
            _cl = _gd.ResourceFactory.CreateCommandList();

            _window.BorderVisible = false;

            // キー入力
            _window.KeyDown += (evt) =>
            {
                // ESCで非表示
                if (evt.Key == Key.Escape)
                {
                    _window.Visible = false;
                }
                // タブで全体を半透明
                if (evt.Key == Key.Tab)
                {
                    _window.Opacity = 0.5f;
                }
            };
            _window.KeyUp += (evt) =>
            {
                // タブで全体を半透明解除
                if (evt.Key == Key.Tab)
                {
                    _window.Opacity = 1.0f;
                }
            };

            // リサイズ時の処理
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };

            // ドラッグアンドドロップ
            _window.DragDrop += (evt) =>
            {
                _app.OnDropItem(evt.File);
            };


            // IMGUI制御
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

            // アプリケーション初期化
            _app.Initialize(new Vector2(_window.Width, _window.Height), this);

            // Main application loop
            while (_window.Exists)
            {

                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) 
                {
                    break; 
                }
                _controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                // アプリケーション処理
                if (!_app.Update())
                {
                    _window.Close();
                }

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 0.5f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            // アプリケーション終了
            _app.Terminate();

            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }

        // 隠すボタンが押されたときの処理
        public void OnPushHide()
        {
            _window.Visible = false;
        }
    }
}
