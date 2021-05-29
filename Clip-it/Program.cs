using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Collections.Generic;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using AngleSharp;
using ImGuiNET;
using static ImGuiNET.ImGuiNative;
using System.Net.Http;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Clip_it
{
    class Program
    {
        static App app = new App();
        static List<Fusen> fusens = new List<Fusen>();

        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;

        // UI state
        private static Vector3 _clearColor = new Vector3(0.2f, 0.7f, 0.3f);

        static void Main(string[] args)
        {

            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.BorderlessFullScreen, App.AppName),
                new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _gd);
            _cl = _gd.ResourceFactory.CreateCommandList();


            _window.BorderVisible = false;

            // キー入力
            _window.KeyDown += (evt) =>
            {
                // ESCで終了
                if (evt.Key == Key.Escape)
                {
                    _window.Close();
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
                app.OnDropItem(evt.File);
                //var model = new FusenModel();
                //model.Text = evt.File;
                //fusens.Add(new Fusen(model));

                //System.Diagnostics.Debug.WriteLine(evt.File);
                //System.Diagnostics.Debug.WriteLine(ImGui.GetClipboardText());
                //var payload = ImGui.GetDragDropPayload();
                //System.Diagnostics.Debug.WriteLine(payload.ToString());
            };


            // IMGUI制御
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

            // アプリケーション初期化
            app.Initialize(new Vector2(_window.Width, _window.Height));

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
                app.Update();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 0.5f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            // アプリケーション終了
            app.Terminate();


            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }
};
}
