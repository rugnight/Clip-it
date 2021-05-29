using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data.SQLite;
using ImGuiNET;

namespace Clip_it
{
    class LinkTable
    {
        const string TB_NAME = "t_link";

        ClipItDB _db = null;
        SQLiteConnection connection => _db.Connection;

        public LinkTable(ClipItDB db)
        {
            _db = db;
        }

        /// <summary>
        /// テーブルを生成
        /// </summary>
        public void CreateTable()
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            try
            {
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"create table {TB_NAME}(link TEXT PRIMARY KEY, title TEXT)";
                    command.ExecuteNonQuery();
                }
            }
            catch(Exception e)
            {
                ImGui.LogText(e.Message);
            }
        }

        /// <summary>
        /// 付箋を保存
        /// </summary>
        /// <param name="models"></param>
        public void Save(List<LinkModel> models)
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            using (SQLiteTransaction trans = connection.BeginTransaction())
            {
                SQLiteCommand cmd = connection.CreateCommand();
                foreach (var model in models)
                {
                    // 削除済み
                    if (model.Deleted)
                    {
                        // デリート
                        cmd.CommandText = $"DELETE FROM {TB_NAME} WHERE link = '{model.Link}'";
                    }
                    else
                    { 
                        // インサート
                        cmd.CommandText = $"REPLACE INTO {TB_NAME} (link, title) VALUES (@Link, @Title)";
                        // パラメータセット
                        cmd.Parameters.Add("Link", System.Data.DbType.String);
                        cmd.Parameters.Add("Title", System.Data.DbType.String);
                        // データ追加
                        cmd.Parameters["Link"].Value = model.Link;
                        cmd.Parameters["Title"].Value = model.Title;
                    }
                    cmd.ExecuteNonQuery();

                }
                // コミット
                trans.Commit();
            }
        }

        /// <summary>
        /// 付箋を読み込み
        /// </summary>
        /// <returns></returns>
        public List<LinkModel> Load(List<string> urls)
        {
            var result = new List<LinkModel>();

            Debug.Assert(connection != null);
            if (connection == null) return result;

            SQLiteCommand cmd = connection.CreateCommand();

            foreach (var url in urls)
            {
                cmd.CommandText = $"SELECT * FROM {TB_NAME} WHERE link = '{url}'";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new LinkModel(reader["Link"].ToString(), reader["Title"].ToString());
                        result.Add(model);
                    }
                }
            }
            return result;
        }


    }
}
