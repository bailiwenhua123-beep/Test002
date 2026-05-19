// using SQLite4Unity3d;
// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;
// using System.Collections;

// public class SQManager : Singleton<SQManager>
// {
//     public SQLiteConnection connection { get; private set; }


//     public SQManager() 
//     {
//         string dbPath = Application.streamingAssetsPath + "/SqLite/WuHan.db";
//         connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
//         Debug.Log($"数据库已连接：{dbPath}");

// #if UNITY_EDITOR
//         PlayModeChangeListener.ExitingPlayModeAction += () =>
//         {
//             connection.Dispose();
//             Debug.Log("数据库已注销");
//         };
// #endif
//     }
//     ~SQManager() 
//     {
//         connection.Dispose();
//         Debug.Log("析构函数：数据库已注销");
//     }

//     #region 表操作 T
//     //增表
//     public void AddTable<T>() where T : new()
//     {
//         connection.CreateTable<T>();
//         Debug.Log($"数据库新增表：[{typeof(T)}]");
//     }
//     //删表
//     public void DeleteTable<T>() where T : new()
//     {
//         connection.DropTable<T>();
//         Debug.Log($"数据库删除表：[{typeof(T)}]");
//     }
//     //查表
//     public IEnumerable<T> GetTable<T>() where T : new()
//     {
//         return connection.Table<T>();
//     }
//     #endregion

//     #region 表操作 DB
//     //查表
//     public IEnumerable<DB> GetTable()
//     {
//         return connection.Table<DB>();
//     }



//     #endregion

//     #region 数据操作 T
//     //增
//     public void AddLine<T>(T db) where T : DB, new()
//     {
//         connection.Insert(db);
//     }
//     //删
//     public void DeleteLine<T>(T db) where T : DB, new()
//     {
//         connection.Delete(db);
//     }
//     //改
//     public void UpdateLine<T>(T db) where T : DB, new()
//     {
//         connection.Update(db);
//     }
//     //查
//     public T GetLine<T>(T db) where T : DB, new()
//     {
//         return connection.Find<T>(db);
//     }
//     //查
//     public T GetLine<T>(int id) where T : DB, new()
//     {
//         IEnumerable<T> dbs = GetTable<T>();
//         return dbs.Where(db => db.id == id).FirstOrDefault();
//     }
//     #endregion

//     #region 数据操作 DB
//     //增
//     public void AddLine(DB db)
//     {
//         connection.Insert(db);
//     }
//     //删
//     public void DeleteLine(DB db)
//     {
//         connection.Delete(db);
//     }
//     //改
//     public void UpdateLine(DB db)
//     {
//         connection.Update(db);
//     }
//     //查
//     public DB GetLine(DB db)
//     {
//         return connection.Find<DB>(db);
//     }
//     //查
//     public DB GetLine(int id)
//     {
//         IEnumerable<DB> dbs = GetTable<DB>();
//         return dbs.Where(db => db.id == id).FirstOrDefault();
//     }
//     #endregion


// }