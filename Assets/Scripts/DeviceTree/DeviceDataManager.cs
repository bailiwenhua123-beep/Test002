// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unity.VisualScripting;
// using UnityEngine;


// public class DeviceNode
// {
//     public Guid NodeID;
//     public Guid ParentNodeID;
//     public string ModelID;
//     public string NodeName;
//     //层级-----资产树层0 企业层1，站层2，区域层3，生产线层4，装置单元层5(工艺)，设备层6(设备层)，控制层7(设备内阀门等)
//     public int LevelType;
//     public List<DeviceNode> DeviceNodeChildren = new List<DeviceNode>();
//     public CameraInfo Pos = new CameraInfo();
//     public string UnityScenePath;
// }

// /// <summary>
// /// 设备数据管理
// /// </summary>
// public class DeviceDataManager : Singleton<DeviceDataManager>
// {
//     private static Dictionary<string, DeviceNode> allDevices;
//     public static Dictionary<string, DeviceNode> AllDevices
//     {
//         get
//         {
//             if (allDevices == null)
//             {
//                 LoadTechDevices();
//             }
//             return allDevices;
//         }
//     }
//     //Top可能不为一个 将一级菜单全部装在里面了 在AllDevices中获取
//     private static DeviceNode topDevice;
//     public static DeviceNode TopDevice 
//     {
//         get 
//         {
//             if (topDevice==null)
//             {
//                 LoadTechDevices();
//             }
//             return topDevice;
//         }
//     }

//     /// <summary>
//     /// 读取设备信息配置
//     /// </summary>
//     public static void LoadTechDevices()
//     {
//         if (allDevices == null)
//         {
//             allDevices = new Dictionary<string, DeviceNode>();
//             topDevice = new DeviceNode();
//             foreach (var item in SQData.DeviceDatas)
//             {   //小于3的层级不是设备信息 不需要进行存储
//                 if (item.Value.LevelType < 3)
//                     continue;
//                 DeviceNode localDeviceNode = new DeviceNode();
//                 localDeviceNode.NodeID = item.Value.NodeID;
//                 localDeviceNode.ModelID = item.Value.ModelID;
//                 localDeviceNode.NodeName = item.Value.NodeName;
//                 localDeviceNode.LevelType = item.Value.LevelType;
//                 localDeviceNode.ParentNodeID = item.Value.ParentNodeID;
//                 localDeviceNode.DeviceNodeChildren = GetDeviceChilds(localDeviceNode);
//                 localDeviceNode.Pos = CameraDataResolve.JObjectToCameraInfo(item.Value.XYZ);
//                 localDeviceNode.UnityScenePath = item.Value.UnityScenePath;
//                 allDevices.Add(localDeviceNode.ModelID, localDeviceNode);
//                 if (localDeviceNode.LevelType==4)//生产线层 存最上层 逐一往下查询生成面板
//                 {
//                     topDevice.DeviceNodeChildren.Add(localDeviceNode);
//                 }
//             }
//         }
//     }
//     /// <summary>
//     /// 获取节点子集
//     /// </summary>
//     /// <param name="node">设备节点信息</param>
//     /// <returns></returns>
//     public static List<DeviceNode> GetDeviceChilds(DeviceNode node)
//     {
//         List<DeviceNode> LocalDevices = new List<DeviceNode>();
//         foreach (var item in SQData.DeviceDatas)
//         {
//             if (item.Value.ParentNodeID == node.NodeID)
//             {
//                 DeviceNode localDeviceNode = new DeviceNode();
//                 localDeviceNode.NodeID = item.Value.NodeID;
//                 localDeviceNode.ModelID = item.Value.ModelID;
//                 localDeviceNode.NodeName = item.Value.NodeName;
//                 localDeviceNode.LevelType = item.Value.LevelType;
//                 localDeviceNode.ParentNodeID = item.Value.ParentNodeID;
//                 localDeviceNode.DeviceNodeChildren=GetDeviceChilds(localDeviceNode);
//                 localDeviceNode.Pos = CameraDataResolve.JObjectToCameraInfo(item.Value.XYZ);
//                 localDeviceNode.UnityScenePath = item.Value.UnityScenePath;
//                 LocalDevices.Add(localDeviceNode);
//             }
//         }
//         return LocalDevices;
//     }
// }
