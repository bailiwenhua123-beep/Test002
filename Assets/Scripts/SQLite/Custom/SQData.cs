// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// public enum ETab
// {
//     ShengChanXinXi = 1, //生产信息
//     XuShiLianDong, //虚实联动
//     XuNiFangZhen, //虚拟仿真
//     GuZhangHuiSu, //故障回溯
//     YunWeiJueCe, //运维决策
//     ZhiNengXunJian, //智能巡检

//     YWZJ,//运维专家
//     WDZS,//问答助手
// }

// public class SQData
// {

//     private static Dictionary<int, Tab> tabs; //页签
//     public static Dictionary<int, Tab> Tabs
//     {
//         get
//         {
//             if (tabs == null)
//             {
// #if UNITY_WEBGL && !UNITY_EDITOR
//                 TextAsset ta = Util.LoadRes<TextAsset>($"Data/Tab");
//                 IEnumerable<Tab> dbs = JsonHelper.DeSerializeDB<List<Tab>>(ta.text);
                
// #else
//                 IEnumerable<Tab> dbs = SQManager.Instance.GetTable<Tab>();
// #endif
//                 tabs = dbs.ToDictionary(b => b.id);
//             }
//             return tabs;
//         }
//     }

//     private static Dictionary<string, EquipmentTree> deviceDatas;
//     /// <summary>
//     /// 数据库设备信息
//     /// </summary>
//     public static Dictionary<string, EquipmentTree> DeviceDatas
//     {
//         get
//         {
//             if (deviceDatas == null)
//             {
// #if UNITY_WEBGL && !UNITY_EDITOR
//                 TextAsset ta = Util.LoadRes<TextAsset>($"Data/EquipmentTree");
//                 IEnumerable<EquipmentTree> dbs = JsonHelper.DeSerializeDB<List<EquipmentTree>>(ta.text);
                
// #else
//                 IEnumerable<EquipmentTree> dbs = SQManager.Instance.GetTable<EquipmentTree>();
// #endif
//                 deviceDatas = dbs.ToDictionary(b => b.ModelID);
//             }
//             return deviceDatas;
//         }
//     }

//     private static Dictionary<string, TimeLineIntroduce> timeLineIntroduceData;
//     /// <summary>
//     /// TimeLine介绍信息内容
//     /// </summary>
//     public static Dictionary<string, TimeLineIntroduce> TimeLineIntroduceData
//     {
//         get
//         {
//             if (timeLineIntroduceData == null)
//             {
// #if UNITY_WEBGL && !UNITY_EDITOR
//                 TextAsset ta = Util.LoadRes<TextAsset>($"Data/TimeLineIntroduce");
//                 IEnumerable<TimeLineIntroduce> dbs = JsonHelper.DeSerializeDB<List<TimeLineIntroduce>>(ta.text);
                
// #else
//                 IEnumerable<TimeLineIntroduce> dbs = SQManager.Instance.GetTable<TimeLineIntroduce>();
// #endif
//                 timeLineIntroduceData = dbs.ToDictionary(b => b.IntroduceKey);
//             }
//             return timeLineIntroduceData;
//         }
//     }


//     private static Dictionary<int, MarkTag> markTags;
//     /// <summary>
//     /// 标签信息列表
//     /// </summary>
//     public static Dictionary<int, MarkTag> MarkTags
//     {
//         get
//         {
//             if (markTags == null)
//             {
// #if UNITY_WEBGL && !UNITY_EDITOR
//                 TextAsset ta = Util.LoadRes<TextAsset>($"Data/MarkTag");
//                 IEnumerable<MarkTag> dbs = JsonHelper.DeSerializeDB<List<MarkTag>>(ta.text);
                
// #else
//                 IEnumerable<MarkTag> dbs = SQManager.Instance.GetTable<MarkTag>();
// #endif
//                 markTags = dbs.ToDictionary(b => b.id);
//             }
//             return markTags;
//         }
//     }

//     private static Dictionary<int, BiaoTypeData> biaoTypes;
//     /// <summary>
//     /// 表类型列表
//     /// </summary>
//     public static Dictionary<int, BiaoTypeData> BiaoTypes
//     {
//         get
//         {
//             if (biaoTypes == null)
//             {
// #if UNITY_WEBGL && !UNITY_EDITOR
//                 TextAsset ta = Util.LoadRes<TextAsset>($"Data/BiaoTypeData");
//                 IEnumerable<BiaoTypeData> dbs = JsonHelper.DeSerializeDB<List<BiaoTypeData>>(ta.text);
                
// #else
//                 IEnumerable<BiaoTypeData> dbs = SQManager.Instance.GetTable<BiaoTypeData>();

// #endif
//                 biaoTypes = dbs.ToDictionary(b => b.id);
//             }
//             return biaoTypes;
//         }
//     }



//     /// <summary>
//     /// 数据库设备监控信息
//     /// </summary>
//     private static Dictionary<string, List<MonitorModel>> monitorDatas;
//     /// <summary>
//     /// 数据库设备监控信息
//     /// </summary>
//     public static Dictionary<string, List<MonitorModel>> MonitorDatas
//     {
//         get
//         {
//             if (monitorDatas == null)
//             {
//                 monitorDatas = new Dictionary<string, List<MonitorModel>>();

// #if UNITY_WEBGL && !UNITY_EDITOR
//                 TextAsset ta = Util.LoadRes<TextAsset>($"Data/MonitorModel");
//                 IEnumerable<MonitorModel> dbs = JsonHelper.DeSerializeDB<List<MonitorModel>>(ta.text);
// #else
//                 IEnumerable<MonitorModel> dbs = SQManager.Instance.GetTable<MonitorModel>();
// #endif
//                 foreach (MonitorModel model in dbs)
//                 {
//                     if (monitorDatas.TryGetHave(model.ModelId))
//                     {
//                         monitorDatas[model.ModelId].Add(model);
//                     }
//                     else
//                     {
//                         List<MonitorModel> monitordata = new List<MonitorModel>();
//                         monitordata.Add(model);
//                         monitorDatas.Add(model.ModelId, monitordata);
//                     }
//                 }
//             }
//             return monitorDatas;
//         }
//     }


//     public static int CurTabID = 2; //页签ID：默认为2=虚实联动
//     public static ETab CurTab { get { return (ETab)CurTabID; } } //页签枚举类型


//     //静态构造，最先调用，早于静态字段
//     static SQData()
//     {
//         InitData();
//     }

//     public static void InitData()
//     {
//         //foreach (var tab in Tabs) 
//         //{
//         //    Debug.Log(tab.Value.name);
//         //}
//     }

//     //切换页签
//     public static void ChangeTab(int tabID)
//     {
//         CurTabID = tabID;
//         MessageCenter.Instance.Dispatch(MessageCenter.EMessage.TabChanged, CurTab);
//         Debug.Log("切换页签至：" + Tabs[CurTabID].name);
//     }

//     public static void RefreshData()
//     {
//         //UIManager.Instance.StartIE(IERefresh());
//     }
//     private static IEnumerator IERefresh()
//     {
//         yield return new WaitForEndOfFrame();

//         InitData();
//         //MessageCenter.Instance.Dispatch(MessageCenter.EMessage.RefreshSQData);
//     }

// }