using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;

    public class SqliteHelper
    {
        public static bool CreateDataBaseFile(string tableName, SQLiteConnection sqliteConn, string dbPath, List<string> fieldsList)
        {
            try
            {
                if (!File.Exists(dbPath))
                {
                    SQLiteConnection.CreateFile(dbPath);
#if UNITY_EDITOR
                    UnityEditor.AssetDatabase.Refresh();
#endif
                }
                InitTable(tableName, sqliteConn, fieldsList);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("新建数据库文件" + dbPath + "失败：" + ex.Message);
            }
        }

        private static void InitTable(string tableName, SQLiteConnection sqliteConn, List<string> fieldsList)
        {
            if (TableExist(tableName, sqliteConn) == false)
            {
                CreateTable(tableName, sqliteConn, fieldsList);
            }
        }

        /// <summary>
        /// 判断表是否存在 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static bool TableExist(string tableName, SQLiteConnection sqliteConn)
        {
            if (sqliteConn.State == ConnectionState.Closed) sqliteConn.Open();
            SQLiteCommand mDbCmd = sqliteConn.CreateCommand();
            mDbCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master where type='table' and name='" + tableName + "';";
            int row = Convert.ToInt32(mDbCmd.ExecuteScalar());
            //sqliteConn.Close();
            if (0 < row)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="dbPath">指定数据库文件</param>
        /// <param name="tableName">表名称</param>
        static public void CreateTable(string tableName, SQLiteConnection sqliteConn, List<string> Columns)
        {
            if (sqliteConn.State != System.Data.ConnectionState.Open) sqliteConn.Open();
            string Column = "";
            for (int i = 0; i < Columns.Count; i++)
            {
                Column += Columns[i] + ",";
            }
            Column = Column.Substring(0, Column.Length - 1);
            SQLiteCommand cmd = new SQLiteCommand();
            cmd.Connection = sqliteConn;
            cmd.CommandText = " CREATE TABLE " + tableName + "(" + Column + ")";
            cmd.ExecuteNonQuery();
            //sqliteConn.Close();
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="dbPath">指定数据库文件</param>
        /// <param name="tableName">表名称</param>
        static public void CreateTable(string sql, SQLiteConnection sqliteConn)
        {
            if (sqliteConn.State != System.Data.ConnectionState.Open) sqliteConn.Open();
            SQLiteCommand cmd = new SQLiteCommand();
            cmd.Connection = sqliteConn;
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            //sqliteConn.Close();
        }

        #region Example

        //private void DBOperationExaple(EquipmentTreeData equipmentTreeData)
        //{
        //    //查询
        //    var dataList = SqliteHelper.Query<EquipmentTreeData>("RP_Column", " WHERE [TableID]='" + "TableID" + "' ORDER BY TagName");
        //    //添加
        //    SqliteHelper.Add(SqliteHelper.AddString("[RP_Table]", new string[] { "TableID", "SvrID" }, new string[] { "TableID", "SvrID" }));
        //    //更新
        //    SqliteHelper.Update(SqliteHelper.UpdateString("[RP_Table]",
        //                    "[Name]='" + tableInfo.Name + "'" +
        //                    ",[Description]='" + tableInfo.Description + "'" +
        //                    ",[MaxRecords]='" + tableInfo.MaxRecords + "'" +
        //                    ",[STEnabled]='" + tableInfo.STEnabled + "'" +
        //                    ",[ArchiveTableCount]='" + tableInfo.ArchiveTableCount + "'" +
        //                    ",[MaxArchiveRecords]='" + tableInfo.MaxArchiveRecords + "'" +
        //                    ",[ArchiveEnabled]='" + tableInfo.ArchiveEnabled + "'" +
        //                    ",[TimeInterval]='" + tableInfo.TimeInterval + "'" +
        //                    ",[TableName]='" + tableInfo.TableName + "'" +
        //                    ",[ReportTitle]='" + tableInfo.ReportTitle + "'" +
        //                    ",[ReportStype]='" + tableInfo.ReportStype + "'" +
        //                    ",[StoreTime]='" + tableInfo.StoreTime + "'" +
        //                    ",[StoreType]='" + tableInfo.StoreType + "'" +
        //                    ",[LastModified]='" + tableInfo.LastModified + "'", " WHERE [TableID]='" + tableInfo.TableID + "'"));
        //}       

        #endregion

        #region 数据库查询



        /// <summary>
        /// 条件查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="TableName"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<T> Query<T>(string TableName, SQLiteConnection sqliteConn, string where = "") where T : new()
        {
            try
            {
                List<T> datas = new List<T>();
                if (sqliteConn.State != System.Data.ConnectionState.Open) sqliteConn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = sqliteConn;
                    cmd.CommandText = "select * from " + TableName + " " + where;
                    //Debug.Log("cmd.CommandText:"+cmd.CommandText);

                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    System.Reflection.PropertyInfo[] properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (properties.Length <= 0)
                    {
                        throw new Exception("类属性长度为零");
                    }
                    foreach (DataRow dd in dt.Rows)
                    {
                        int i = 0;
                        var model = new T();
                        foreach (System.Reflection.PropertyInfo item in properties)
                        {
                            if (item.PropertyType == Type.GetType("System.Guid"))
                            {
                                var ds = Guid.Parse(dd[i++].ToString());
                                item.SetValue(model, ds, null);
                            }
                            else
                            {
                                try
                                {
                                    var ds = Convert.ChangeType(dd[i++], item.PropertyType);
                                    item.SetValue(model, ds, null);
                                }
                                catch { }
                            }

                        }
                        datas.Add(model);
                    }
                }
                //sqliteConn.Close();
                return datas;
            }
            catch (Exception ex)
            {
            //Log.Error("查询出错:" + ex.Message + "\r\n" + ex.StackTrace);
            throw new Exception("查询出错：" + ex.Message);
            }
        }

        /// <summary>
        /// 查询一整张表
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static DataTable QueryDataTable(string TableName, SQLiteConnection sqliteConn, string where = "")
        {
            try
            {
                if (sqliteConn.State != System.Data.ConnectionState.Open) sqliteConn.Open();
                DataTable dt = new DataTable();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = sqliteConn;
                    cmd.CommandText = "select * from " + TableName + " " + where;
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);

                    da.Fill(dt);

                }
                //sqliteConn.Close();
                return dt;
            }
            catch (Exception ex)
            {
            //Log.Error("查询出错:" + ex.Message + "\r\n" + ex.StackTrace);
            throw new Exception("查询出错：" + ex.Message);
            }
        }

        /// <summary>
        /// 查询是否有数据
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static bool Query(string sql, SQLiteConnection sqliteConn)
        {
            try
            {
                bool b = false;
                if (sqliteConn.State != System.Data.ConnectionState.Open) sqliteConn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = sqliteConn;
                    cmd.CommandText = sql;
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    b = dt.Rows.Count > 0;
                }
                //sqliteConn.Close();
                return b;
            }
            catch (Exception ex)
            {
            //Log.Error("查询出错:" + ex.Message + "\r\n" + ex.StackTrace);
            throw new Exception("查询出错：" + ex.Message);
            }
        }

        #endregion

        #region 数据库增加

        /// <summary>
        /// 添加记录
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static bool Add(string sql, SQLiteConnection sqliteConn)
        {
            if (sqliteConn.State == ConnectionState.Closed) sqliteConn.Open();
            DbTransaction trans = sqliteConn.BeginTransaction();
            try
            {
                int rows = 0;
                using (SQLiteCommand cmd = new SQLiteCommand("VACUUM", sqliteConn))
                {
                    cmd.CommandText = sql;
                    rows = cmd.ExecuteNonQuery();
                }
                trans.Commit();//提交事务
                //sqliteConn.Close();
                return rows > 0;
            }
            catch (Exception ex)
            {
                trans.Rollback();//回滚事务
                //Log.Error("新增出错:" + ex.Message + "\r\n" + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// 添加记录的SQL语句
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string AddString(string TableName, string[] keys, string[] values)
        {
            string keys_string = "(" + keys[0];
            string value_string = "('" + values[0] + "'";

            for (int i = 1; i < keys.Length; i++)
            {
                keys_string += "," + keys[i];
            }
            for (int i = 1; i < values.Length; i++)
            {
                value_string += ",'" + values[i] + "'";
            }
            keys_string += ")";
            value_string += ")";
            string sql = string.Format("INSERT INTO " + TableName + " {0} VALUES {1} ;", keys_string, value_string);
            return sql;
        }
        #endregion

        #region 数据库更新

        /// <summary>
        /// 数据库更新
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static bool Update(string sql, SQLiteConnection sqliteConn) //修改信息 
        {
            //Debug.Log("sql:" + sql);
            if (sqliteConn.State == ConnectionState.Closed) sqliteConn.Open();
            DbTransaction trans = sqliteConn.BeginTransaction();
            try
            {
                int rows = 0;
                using (SQLiteCommand cmd = new SQLiteCommand("VACUUM", sqliteConn))
                {
                    cmd.CommandText = sql;
                    rows = cmd.ExecuteNonQuery();
                }
                trans.Commit();//提交事务
                //sqliteConn.Close();
                return rows > 0;
            }
            catch (Exception ex)
            {
                trans.Rollback();//回滚事务
                //Log.Error("修改出错:" + ex.Message + "\r\n" + ex.StackTrace);
                return false;
            }
        }




        /// <summary>
        /// 更新字符串SQL语句
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="values"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static string UpdateString(string TableName, string values, string where = "")
        {
            string sql = string.Format("UPDATE " + TableName + " SET {0} {1}; ", values, where);
            return sql;
        }
        #endregion

        #region 数据库删除

        /// <summary>
        /// 删除信息
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static bool Delete(string sql, SQLiteConnection sqliteConn)//删除信息
        {
            if (sqliteConn.State == ConnectionState.Closed) sqliteConn.Open();
            DbTransaction trans = sqliteConn.BeginTransaction();
            try
            {
                int rows = 0;
                using (SQLiteCommand cmd = new SQLiteCommand("VACUUM", sqliteConn))
                {
                    cmd.CommandText = sql;
                    rows = cmd.ExecuteNonQuery();
                }
                trans.Commit();//提交事务
                //sqliteConn.Close();
                return rows > 0;
            }
            catch (Exception ex)
            {
                trans.Rollback();//回滚事务
                //Log.Error("删除出错:" + ex.Message + "\r\n" + ex.StackTrace);
                return false;
            }
        }

        public static string DeleteStringAll(string TableName)
        {
            string sql = "delete from " + TableName;

            return sql;
        }

        public static string DeleteString(string TableName, string where = "")
        {
            string sql = "delete from " + TableName + " " + where;

            return sql;
        }

        #endregion

    }
