using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Clip_it
{
    /// <summary>
    /// 付箋情報
    /// </summary>
    class FusenModel
    {
        // 識別ID
        Guid _id;
        public Guid Id 
        {
            get => this._id;
            set 
            {
                if (this._id == value) return;
                this._id = value;
            }
        }

        // 内容
        string _text = "";
        public string Text
        {
            get => this._text;
            set
            {
                if (this._text == value) return;
                this._text = value;
                this.OnChangeText?.Invoke(this);
            }
        }

        // 折りたたみ済み
        public bool Opened = true;

        // ポジション
        public float X = 0.0f;
        public float Y = 0.0f;

        // 削除済み
        public bool Deleted { get; set; } = false;

        // 内容更新通知
        public event Action<FusenModel> OnChangeText;

        // コンストラクタ
        public FusenModel() 
            : this(Guid.NewGuid(), "")
        {
        }
        public FusenModel(string text)
            : this(Guid.NewGuid(), text)
        {
        }
        public FusenModel(Guid id, string text)
        {
            this._id = id;
            this._text = (text != null) ? text : "";
        }

        // テキストに含まれるURLを取得
        public List<string> GetURLs()
        {
            return CollectURL(this.Text);
        }

        // テキストに含まれるパスを取得
        public List<string> GetPaths()
        {
            return CllectPath(this.Text);
        }

        // テキストに含まれる日付を取得
        public List<string> GetDates()
        {
            return CollectDate(this.Text);
        }

        // テキストに含まれるURLを取得
        static List<string> CollectURL(string text)
        {
            var result = new List<string>();

            var reg = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (Match match in reg.Matches(text))
            {
                result.Add(match.Value);
            }

            return result;
        }

        // テキストに含まれるファイルパスを取得

        static List<string> CllectPath(string text)
        {
            var result = new List<string>();

            var reg = new Regex(@"(?:(?:(?:\b[a-z]:|\\\\[a-z0-9_.$]+\\[a-z0-9_.$]+)\\|\\?[^\\/:*?""<>|\r\n]+\\?)(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]*)", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            foreach (Match match in reg.Matches(text))
            {
                result.Add(match.Value);
            }

            return result;
        }

        /// <summary>
        /// テキストに含まれる日付を取得
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        static List<string> CollectDate(string text)
        {
            var result = new List<string>();

            // 2021-05-29 00:00
            var reg = new Regex(@"(\d{4}-\d{1,2}-\d{1,2}\s\d{2}:\d{2})|(\d{4}-\d{1,2}-\d{1,2})");
            foreach (Match match in reg.Matches(text))
            {
                result.Add(match.Value);
            }

            // 2021/05/29
            reg = new Regex(@"\d{4}/\d{1,2}/\d{1,2}");
            foreach (Match match in reg.Matches(text))
            {
                result.Add(match.Value);
            }

            // 2021年5月29日
            reg = new Regex(@"\d{4}年\d{1,2}月\d{1,2}日");
            foreach (Match match in reg.Matches(text))
            {
                result.Add(match.Value);
            }

            return result;
        }

    };
}
