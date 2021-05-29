using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data.SQLite;
using ImGuiNET;

namespace Clip_it
{
    /// <summary>
    /// Sqliteで付箋を管理
    /// </summary>
    class FusenDB
    {
        SQLiteConnection connection = null;

        public FusenDB()
        {
        }
        ~FusenDB()
        {
            Close();
        }

        /// <summary>
        /// コネクションを開く
        /// </summary>
        /// <param name="dbFilePath"></param>
        /// <returns></returns>
        public bool Open(string dbFilePath)
        {
            if (connection != null)
            {
                return true;
            }


            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = dbFilePath };
            connection = new SQLiteConnection(sqlConnectionSb.ToString());
            connection.Open();

            return true;
        }

        /// <summary>
        /// コネクションを閉じる
        /// </summary>
        public void Close()
        {
            if (connection == null) return;

            connection.Close();
            connection = null;
        }

        /// <summary>
        /// テーブルを生成
        /// </summary>
        public void CreateDB()
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            try
            {
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "create table t_fusen(id TEXT PRIMARY KEY, text TEXT, opened TEXT, opened_text TEXT)";
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
        public void Save(List<FusenModel> models)
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
                        cmd.CommandText = $"DELETE FROM t_fusen WHERE id = '{model.Id.ToString()}'";
                    }
                    else
                    { 
                        // インサート
                        cmd.CommandText = "REPLACE INTO t_fusen (id, text, opened, opened_text) VALUES (@Id, @Text, @Opened, @Opened_Text)";
                        // パラメータセット
                        cmd.Parameters.Add("Id", System.Data.DbType.String);
                        cmd.Parameters.Add("Text", System.Data.DbType.String);
                        cmd.Parameters.Add("Opened", System.Data.DbType.String);
                        cmd.Parameters.Add("Opened_Text", System.Data.DbType.String);
                        // データ追加
                        cmd.Parameters["Id"].Value = model.Id.ToString();
                        cmd.Parameters["Text"].Value = model.Text;
                        cmd.Parameters["Opened"].Value = model.Opened.ToString();
                        cmd.Parameters["Opened_Text"].Value = model.OpenedText.ToString();
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
        public List<FusenModel> Load()
        {
            var result = new List<FusenModel>();

            Debug.Assert(connection != null);
            if (connection == null) return result;

            SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM t_fusen";

            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var model  = new FusenModel(
                        new Guid(reader["Id"].ToString()),
                        reader["Text"].ToString()
                        );
                    model.Opened = bool.Parse(reader["Opened"].ToString());
                    model.OpenedText = bool.Parse(reader["Opened_Text"].ToString());
                    result.Add(model);
                }
            }
            return result;
        }

        /// <summary>
        /// SQLITEのバージョンを表示
        /// </summary>
        public void VersionDB()
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = "select sqlite_version()";
                Console.WriteLine(cmd.ExecuteScalar());
            }
        }

    };
}
