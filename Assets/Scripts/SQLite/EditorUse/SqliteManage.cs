
// using Newtonsoft.Json;
// using System;
// using System.Collections.Generic;
// using System.Data.SQLite;
// using Unity.VisualScripting;
// using UnityEngine;

//     public class SqliteManage 
//     {
//         public static SqliteManage instance;
//         public static SqliteManage Instance
//         {
//             get {
//                 if (instance == null)
//                 {
//                     instance = new SqliteManage();
//                 }
//                 return instance;
//             }            
//         }
       
//         public string dbPath;
//         public SQLiteConnection sqliteConn;

//         private SqliteManage()
//         {
//             dbPath = Application.streamingAssetsPath + "/SqLite/WuHan.db";
//             sqliteConn = new SQLiteConnection("data source=" + dbPath);
//         }

//         /// <summary>
//         /// 创建设备树数据库文件
//         /// </summary>
//         public void CreateDataBaseFile(string tableName,List<string> fieldsList)
//         {
//             SqliteHelper.CreateDataBaseFile(tableName, sqliteConn,dbPath, fieldsList);
//         }

//         /// <summary>
//         /// 根据位号modelID 查询节点信息
//         /// </summary>
//         /// <param name="modelID"></param>
//         /// <returns></returns>
//         public Guid GetNodeInfoByModelID(string modelID)
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [ModelID]='" + modelID + "'");
           

//             if (dataList.Count > 0)
//             {
//                 return dataList[0].NodeID;
//             }
//             return Guid.Parse(null);
//         }

//         public Guid GetNodeIdByNodeName(string nodeName)
//         { 
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [NodeName]='" + nodeName + "'");
//             if (dataList.Count > 0)
//             {
//                 return dataList[0].NodeID;
//             }
//             return Guid.Parse(null);
//         }

//         /// <summary>
//         /// 根据位号modelID 查询节点信息
//         /// </summary>
//         /// <param name="modelID"></param>
//         /// <returns></returns>
//         public Guid GetNodeInfoByID(string modelID)
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("AllTrreeNode", sqliteConn, " WHERE [ModelID]='" + modelID + "'");
//             if (dataList.Count > 0)
//             {
//                 return dataList[0].NodeID;
//             }
//             return Guid.Parse(null);
//         }

//         /// <summary>
//         /// 根据modelID获取节点位置信息
//         /// </summary>
//         public string QueryNodePos(string modelID)
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [ModelID]='" + modelID + "'");

//             if (dataList.Count > 0)
//             {
//                 return dataList[0].XYZ;
//             }
//             return null;
//         }

//         /// <summary>
//         /// 根据modelID获取节点位置信息
//         /// </summary>
//         public string QueryNodePosByParentNodeID(string ParentNodeID)
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [ParentNodeID]='" + ParentNodeID + "'");

//             if (dataList.Count > 0)
//             {
//                 return dataList[0].XYZ;
//             }
//             return null;
//         }

//         /// <summary>
//         /// 根据ParentNodeID获取NodeID
//         /// </summary>
//         /// <param name="ParentNodeID"></param>
//         /// <returns></returns>
//         public string QueryNodeID(string ParentNodeID) {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [ParentNodeID]='" + ParentNodeID + "'");

//             if (dataList.Count > 0)
//             {
//                 return dataList[0].NodeID.ToString();
//             }
//             return null;
//         }

//         /// <summary>
//         /// 根据modelID获取节点名称
//         /// </summary>
//         public string QueryNodeName(string modelID)
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [ModelID]='" + modelID + "'");

//             if (dataList.Count > 0)
//             {
//                 return dataList[0].NodeName;
//             }
//             return "";
//         }

//         /// <summary>
//         /// 根据modelID获取节点描述
//         /// </summary>
//         public string QueryNodeDescribe(string modelID)
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn," WHERE [ModelID]='" + modelID + "'");

//             if (dataList.Count > 0)
//             {
//                 return dataList[0].Description;
//             }
//             return "";
//         }

//         /// <summary>
//         /// 获取所有的节点信息
//         /// </summary>
//         /// <returns></returns>
//         public List<EquipmentTree> QueryAllNodeInfo()
//         {
//             return SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn);
//         }

//         public List<EquipmentTree> QueryAllNodeInfoEnabled()
//         {
//         if (sqliteConn == null)
//         { Debug.LogError("未连接成功"); }
//         else { }
//             return SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [Enabled]='True'");
//         }

//         /// <summary>
//         /// 查询节点的子节点
//         /// </summary>
//         /// <param name="nodeID"></param>
//         /// <returns></returns>
//         public List<EquipmentTree> QueryChildNodesInfoOrderByDisplay(Guid nodeID)
//         {
//             string sql = " WHERE [ParentNodeID]='" + nodeID + "' and [Enabled] = 'True' order by DisplayOrder";
//             return SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, sql);
//         }

//         public List<EquipmentTree> QueryChildNodesInfo(Guid nodeID)
//         {
//             string sql = " WHERE [ParentNodeID]='" + nodeID + "' and [Enabled] = 'True' order by DisplayOrder asc";
//             return SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, sql);
//         }

//         /// <summary>
//         /// 查询节点的子节点
//         /// </summary>
//         /// <param name="nodeID"></param>
//         /// <returns></returns>
//         public List<EquipmentTree> QueryChildNodesInfo(string modelID)
//         {
//             return SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [ModelID]='" + modelID + "'");
//         }

//         /// <summary>
//         /// 查询节点信息
//         /// </summary>
//         /// <param name="nodeID"></param>
//         /// <returns></returns>
//         public EquipmentTree QueryNodeInfo(Guid nodeID)
//         {
//             string sql = " WHERE [NodeID]='" + nodeID + "' and [Enabled] = 'True'"; 
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [NodeID]='" + nodeID + "'");
//             if (dataList.Count > 0)
//             {
//                 return dataList[0];
//             }
//             return null;
//         }

//         /// <summary>
//         /// 根据modelID查询节点信息
//         /// </summary>
//         /// <param name="modelID"></param>
//         /// <returns></returns>
//         public EquipmentTree QueryNodeInfo(string modelID)
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [ModelID]='" + modelID + "'");

//             if (dataList.Count > 0)
//             {
//                 return dataList[0];
//             }
//             return null;
//         }

//         /// <summary>
//         /// 根据modelID查询节点信息
//         /// </summary>
//         /// <param name="modelID"></param>
//         /// <returns></returns>
//         public EquipmentTree QueryNodeInfoByNodeID(string NodeID)
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [NodeID]='" + NodeID + "'");

//             if (dataList.Count > 0)
//             {
//                 return dataList[0];
//             }
//             return null;
//         }
//         public EquipmentTree QueryNodeInfoByNodeName(string NodeName)
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [NodeName]='" + NodeName + "'");

//             if (dataList.Count > 0)
//             {
//                 return dataList[0];
//             }
//             return null;
//         }

//         /// <summary>
//         /// 根据ParentNodeID 查询节点列表
//         /// </summary>
//         /// <param name="ParentNodeID"></param>
//         /// <returns></returns>
//         public List<EquipmentTree> QueryNodeInfoByParentNodeID(string ParentNodeID) {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [ParentNodeID]='" + ParentNodeID + "'");
//             if (dataList.Count > 0) {
//                 return dataList;
//             }
//             return null;
//         }

//         public List<EquipmentTree> QueryNodeInfoByParentNodeIDOrderByDisplayOrder(string ParentNodeID) 
//         {
//             List<EquipmentTree> dataList = SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [ParentNodeID]='" + ParentNodeID + "' order by DisplayOrder asc");
//             if (dataList.Count > 0)
//             {
//                 return dataList;
//             }
//             return null;
//         }

//         /// <summary>
//         /// 通过节点名称进行模糊查询
//         /// </summary>
//         /// <param name="nodeName"></param>
//         /// <returns></returns>
//         public List<EquipmentTree> QueryNodeInfo_Dim(string nodeName)
//         {
//             return SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [NodeName] like '" +"%"+ nodeName + "%'");
//         }

//         /// <summary>
//         /// 更新数据库记录
//         /// </summary>
//         /// <param name="equipmentTreeData"></param>   
//         public void UpdateNodeXYZ(string positionStr, string moedlID)
//         {
//             SqliteHelper.Update(SqliteHelper.UpdateString("[EquipmentTree]",
//                     "[XYZ]='" + positionStr + "'"
//                     , " WHERE [ModelID]='" + moedlID + "'"
//                     ), sqliteConn);
//         }

//         public void UpdateNodeName(string nodeName, string NodeID)
//         {
//             SqliteHelper.Update(SqliteHelper.UpdateString("[EquipmentTree]",
//                     "[NodeName]='" + nodeName + "'"
//                     , " WHERE [NodeID]='" + NodeID + "'"
//                     ), sqliteConn);
//         }

//         /// <summary>
//         /// 更新节点的层级结构
//         /// </summary>
//         /// <param name="NodeID"></param>
//         /// <param name="ParentNodeID"></param>
//         /// <param name="DisplayOrder"></param>
//         public void UpdateNodeHierarchy(Guid NodeID, Guid ParentNodeID, int DisplayOrder,string nodeName, int LevelType) 
//         {
//             //Debug.Log("====UpdateNodeHierarchy===="+ LevelType.ToString());
//             SqliteHelper.Update(SqliteHelper.UpdateString("[EquipmentTree]",
//                     "[ParentNodeID]='" + ParentNodeID + "'" +
//                     ",[DisplayOrder]='" + DisplayOrder + "',[NodeName]='"+ nodeName+ "',[LevelType]='" + LevelType + "'", " WHERE [NodeID]='" + NodeID + "'"
//                     ), sqliteConn);
//         }

//         /// <summary>
//         /// 更新数据库记录
//         /// </summary>
//         /// <param name="equipmentTreeData"></param>   
//         public void UpdateNodeInfo(EquipmentTree equipmentTreeData)
//         {
//             Debug.Log("equipmentTreeData.Enabled:"+equipmentTreeData.Enabled);

//             SqliteHelper.Update(SqliteHelper.UpdateString("[EquipmentTree]",
//                     "[ModelID]='" + (string.IsNullOrEmpty(equipmentTreeData.ModelID) ? "null" : equipmentTreeData.ModelID) + "'" +
//                     ",[Description]='" + (string.IsNullOrEmpty(equipmentTreeData.Description) ? "null" : equipmentTreeData.Description) + "'" +
//                     ",[XYZ]='" + (string.IsNullOrEmpty(equipmentTreeData.XYZ) ? "null" : equipmentTreeData.XYZ) + "'"
//                     + ",[UnityScenePath]='" + (string.IsNullOrEmpty(equipmentTreeData.UnityScenePath) ? "null" : equipmentTreeData.UnityScenePath) + "'"
//                     + ",[Enabled]='" + (string.IsNullOrEmpty(equipmentTreeData.Enabled.ToString()) ? "null" : equipmentTreeData.Enabled.ToString()) + "'"
//                     , " WHERE [NodeID]='" + equipmentTreeData.NodeID + "' " 
//                     ), sqliteConn);
//         }

//         /// <summary>
//         /// 更新数据库记录
//         /// </summary>
//         /// <param name="equipmentTreeData"></param>   
//         public void UpdateNodeInfo1(EquipmentTree equipmentTreeData)
//         {
//             SqliteHelper.Update(SqliteHelper.UpdateString("[EquipmentTree]",
//                     "[ModelID]='" + (string.IsNullOrEmpty(equipmentTreeData.ModelID) ? "null" : equipmentTreeData.ModelID) + "'" +
//                     ",[Description]='" + (string.IsNullOrEmpty(equipmentTreeData.Description) ? "null" : equipmentTreeData.Description) + "'" +
//                     ",[XYZ]='" + (string.IsNullOrEmpty(equipmentTreeData.XYZ) ? "null" : equipmentTreeData.XYZ) + "'"
//                     + ",[UnityScenePath]='" + (string.IsNullOrEmpty(equipmentTreeData.UnityScenePath) ? "null" : equipmentTreeData.UnityScenePath) + "'"
//                     , " WHERE [NodeID]='" + equipmentTreeData.NodeID + "' and Enabled='" + equipmentTreeData.Enabled + "';"
//                     ), sqliteConn);
//         }

//         /// <summary>
//         /// 更新数据库记录
//         /// </summary>
//         /// <param name="NodeID"></param>
//         /// <param name="ModelID">模型ID</param>
//         /// <param name="XYZ">节点坐标</param>
//         /// <param name="UnityScenePath">节点路径</param>
//         public void UpdateNodeInfo(Guid NodeID, string NodeName, string ModelID, string Description,string XYZ, string UnityScenePath,bool Enabled)
//         {
//             SqliteHelper.Update(SqliteHelper.UpdateString("[EquipmentTree]",
//                     "[NodeName]='" + (string.IsNullOrEmpty(NodeName) ? "null" : NodeName) + "'" +
//                     ",[ModelID]='" + (string.IsNullOrEmpty(ModelID) ? "null" : ModelID) + "'" +
//                     ",[Description]='" + (string.IsNullOrEmpty(Description) ? "null" : Description) + "'"+
//                     ",[XYZ]='" + (string.IsNullOrEmpty(XYZ) ? "null" : XYZ) + "'"
//                     + ",[UnityScenePath]='" + (string.IsNullOrEmpty(UnityScenePath) ? "null" : UnityScenePath) + "'"
//                     , " WHERE [NodeID]='" + NodeID + "' and Enabled='" + Enabled + "';"
//                     ), sqliteConn);
//         }

//         /// <summary>
//         /// 更新节点描述
//         /// </summary>
//         /// <param name="moedlID"></param>
//         /// <param name="describe"></param>
//         public void UpdateNodeDescribe(string moedlID, string describe)
//         {
//             SqliteHelper.Update(SqliteHelper.UpdateString("[EquipmentTree]",
//                     "[Description]='" + (string.IsNullOrEmpty(describe) ? "null" : describe) + "'"
//                     , " WHERE [ModelID]='" + moedlID + "'"
//                     ), sqliteConn);
//         }

//         /// <summary>
//         /// 更新节点路径
//         /// </summary>
//         /// <param name="moedlID"></param>
//         /// <param name="describe"></param>
//         public void UpdateNodeScreenPath(string MoedlID, string path)
//         {
//             SqliteHelper.Update(SqliteHelper.UpdateString("[EquipmentTree]",
//                     "[UnityScenePath]='" + path + "'"
//                     , " WHERE [ModelID]='" + MoedlID + "'"
//                     ), sqliteConn);
//         }

//         /// <summary>
//         /// 添加节点
//         /// </summary>
//         /// <param name="equipmentTreeData"></param>
//         public void AddNode(EquipmentTree equipmentTreeData)
//         {
//             SqliteHelper.Add(SqliteHelper.AddString("[EquipmentTree]",
//                     new string[] { "NodeID", "ParentNodeID", "NodeName", "ModelID", "Description", "XYZ", "UnityScenePath", "DisplayOrder", "Enabled", "LevelType" },
//                     new string[] {
//                     equipmentTreeData .NodeID.ToString(),
//                     equipmentTreeData .ParentNodeID.ToString(),
//                     equipmentTreeData .NodeName,
//                     string.IsNullOrEmpty(equipmentTreeData .ModelID) ? "null" : equipmentTreeData .ModelID,
//                     string.IsNullOrEmpty(equipmentTreeData .Description) ? "null" : equipmentTreeData .Description,
//                     string.IsNullOrEmpty(equipmentTreeData .XYZ) ? "null" : equipmentTreeData .XYZ,
//                     string.IsNullOrEmpty(equipmentTreeData .UnityScenePath) ? "null" : equipmentTreeData .UnityScenePath,
//                     equipmentTreeData .DisplayOrder.ToString(),
//                     true.ToString(),
//                     equipmentTreeData .LevelType.ToString()
//                     }), sqliteConn);

//         }

//         /// <summary>
//         /// 删除节点信息
//         /// </summary>
//         /// <param name="NodeID"></param>
//         public void DeleteNode(Guid NodeID)
//         {
//             SqliteHelper.Delete(SqliteHelper.DeleteString("[EquipmentTree]", "WHERE [NodeID]='" + NodeID + "'"), sqliteConn);
//         }

//         /// <summary>
//         /// 通过 NodeName删除节点信息
//         /// 删除节点信息
//         /// </summary>
//         /// <param name="NodeID"></param>
//         public void DeleteNodeByNodeName(string NodeName)
//         {
//             SqliteHelper.Delete(SqliteHelper.DeleteString("[EquipmentTree]", "WHERE [NodeName]='" + NodeName + "'"), sqliteConn);
//         }

//         /// <summary>
//         /// 删除节点信息
//         /// </summary>
//         /// <param name="NodeID"></param>
//         public void DeleteTableAll()
//         {
//             SqliteHelper.Delete(SqliteHelper.DeleteStringAll("[EquipmentTree]"), sqliteConn);
//         }

//         public List<EquipmentTree> QueryAssistInfo()
//         {
//             return SqliteHelper.Query<EquipmentTree>("EquipmentTree", sqliteConn, " WHERE [UnityScenePath] != 'null' and [XYZ] != 'null' ");
//         }

//     }

