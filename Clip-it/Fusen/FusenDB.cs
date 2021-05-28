using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Clip_it
{
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

        public void Close()
        {
            if (connection == null) return;

            connection.Close();
            connection = null;
        }

        public void CreateDB()
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            try
            {
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    //command.CommandText = "create table t_product(id INTEGER  PRIMARY KEY AUTOINCREMENT, text TEXT)";
                    command.CommandText = "create table t_fusen(id TEXT PRIMARY KEY, text TEXT, opened TEXT, price INTEGER)";
                    command.ExecuteNonQuery();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

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
                        cmd.CommandText = "REPLACE INTO t_fusen (id, text, opened) VALUES (@Id, @Text, @Opened)";
                        // パラメータセット
                        cmd.Parameters.Add("Id", System.Data.DbType.String);
                        cmd.Parameters.Add("Text", System.Data.DbType.String);
                        cmd.Parameters.Add("Opened", System.Data.DbType.String);
                        // データ追加
                        cmd.Parameters["Id"].Value = model.Id.ToString();
                        cmd.Parameters["Text"].Value = model.Text;
                        cmd.Parameters["Opened"].Value = model.Opened.ToString();
                    }
                    cmd.ExecuteNonQuery();

                }
                // コミット
                trans.Commit();
            }
        }

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
                    result.Add(model);
                }
            }
            return result;
        }

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
