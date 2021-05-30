﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Threading.Tasks;
using System.Numerics;

namespace Clip_it
{
    /// <summary>
    /// 付箋に関するイベント通知
    /// </summary>
    interface IFusenEventHandler
    {
        public void OnFusenChangeAndEditEnd(Fusen fusen);
        public void OnFusenRequestUrlTitle(string url, Action<string> callback);
        public void OnFusenRequestOpenUrl(string url);
        public void OnFusenRequestOpenPath(string path);
    } 

    /// <summary>
    /// 付箋
    /// </summary>
    class Fusen
    {
        FusenModel _model;
        FusenView _view;

        IFusenEventHandler _eventHandler;

        public FusenModel Model => _model;
        public Vector2 LastSize => _view.LastSize;
        public Dictionary<string, LinkModel> Urls { get; private set; } = new Dictionary<string, LinkModel>();
        public Dictionary<string, string> Paths { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, bool> Dates { get; private set; } = new Dictionary<string, bool>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Fusen(IFusenEventHandler eventHandler)
            : this(new FusenModel(), eventHandler)
        {
        }
        public Fusen(FusenModel model, IFusenEventHandler eventHandler) 
        {
            this._model = model;
            this._eventHandler = eventHandler;
            this._model.OnChangeText += (changedModel) =>
            {
                //this.UpdateMetaData();
            };
            //this.UpdateMetaData();
            // 初回の内容変更を通知
            _eventHandler?.OnFusenChangeAndEditEnd(this);

            _view = new FusenView();
            _view.OnChangeText += (changedText) =>
            {
                this._model.Text = changedText;
            };
            _view.OnSelectURL += (url) =>
            {
                _eventHandler?.OnFusenRequestOpenUrl(url);
            };
            _view.OnSelectPath += (path) =>
            {
                _eventHandler?.OnFusenRequestOpenPath(path);
            };
            _view.OnChangeAndEditEnd += () =>
            {
                // 内容変更があってテキスト入力からフォーカスが外れたらメタ情報更新
                //this.UpdateMetaData();
                _eventHandler?.OnFusenChangeAndEditEnd(this);
            };
            _view.OnClose += () =>
            {
                model.Delete();
            };
        }

        /// <summary>
        /// 更新
        /// </summary>
        public bool Update()
        {
            // 削除済みは処理しない
            if (this._model.Deleted)
            {
                return false;
            }
            // 表示
            _view.Disp(this);
            return true;
        }

        /// <summary>
        /// 入力欄にフォーカスを合わせる
        /// </summary>
        public void SetFocusInput()
        {
            _view.SetFocusInput();
        }

        /// <summary>
        /// アクティブ状態か？
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            return _view.IsActive();
        }

        /// <summary>
        /// URLリストを更新
        /// </summary>
        public void UpdateMetaData()
        {
            // 時刻
            var dates = this._model.GetDates();
            foreach (var date in dates)
            {
                if (this.Dates.ContainsKey(date))
                {
                    continue;
                }
                this.Dates[date] = false;
            }

            // ファイルパス
            var paths = this.Model.GetPaths();
            foreach (var path in paths)
            {
                var item = "";
                if (System.IO.File.Exists(path))
                {
                    item = System.IO.Path.GetFileName(path);
                }
                else if (System.IO.Directory.Exists(path))
                {
                    item = path;
                }
                else
                {
                    continue;
                }

                if (this.Paths.ContainsKey(path))
                {
                    continue;
                }
                this.Paths[path] = item;
            }

            // URL
            var urls = this._model.GetURLs();
            foreach (var url in urls)
            {
                if (this.Urls.ContainsKey(url))
                {
                    continue;
                }
                this.Urls[url] = new LinkModel(url, "");

                // 非同期通信でタイトル取得
                this._eventHandler?.OnFusenRequestUrlTitle(
                    url,
                    (title) =>
                    {
                        this.Urls[url].Title = title;
                    }
                );
            }
        }

    }
}
