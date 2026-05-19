// using Sirenix.OdinInspector;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Data.SQLite;
// using System.Linq;
// using Unity.VisualScripting;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UI;

// public class DeviceTreePanel : MonoBehaviour
// {
//     public Transform DeviceContentTr;    //数据卡片容器
//     public GameObject variablePre;      //数据卡片的预制体
//     private DeviceItem currentSelected; //当前被选中的数据卡片
//     //设备列表最顶层面板信息
//     private List<DeviceItem> devicesitems = new List<DeviceItem>();
//     //全部展开标签按钮
//     public ButtonLight BtnMark;



//     private void Reset()
//     {
//         BtnMark = transform.FindComponent<ButtonLight>("Panel/Btn_Mark");
//     }

//     /// <summary>
//     /// 重置面板
//     /// </summary>
//     public void ResetPanel()
//     {
//         foreach (DeviceItem item in devicesitems)
//         {
//             item.ChangeChildState(false);
//         }
//     }

//     /// <summary>
//     /// 初始化面板
//     /// </summary>
//     public void InitPanel()
//     {
//         MessageCenter.Instance.Register<EWalkType, EFixedPathType>(MessageCenter.EMessage.CameraWalk_WalkStart, ResponseWalkStart);

//         MessageCenter.Instance.Register<MarkType>(MessageCenter.EMessage.ShowMark, ShowDeviceMarks);

//         MessageCenter.Instance.Register(MessageCenter.EMessage.CameraWalk_WalkFinish, ResponseWalkEnd);

//         MessageCenter.Instance.Register(MessageCenter.EMessage.ClickDeviceTreePanel, ShowPanel);

//         MessageCenter.Instance.Register<DeviceItem>(MessageCenter.EMessage.DeviceListSelected, OnTabClick);

//         BtnMark.button.onClick.AddListener(ClickBtnMark);
//         BtnMark.IsLight = false;

//         foreach (var item in DeviceDataManager.TopDevice.DeviceNodeChildren)
//         {
//             GameObject newDeviceItem = Instantiate(variablePre, DeviceContentTr);
//             newDeviceItem.GetComponent<DeviceItem>().InitPanelContent(item, variablePre);
//             newDeviceItem.name = item.NodeName;
//             devicesitems.Add(newDeviceItem.GetComponent<DeviceItem>());
//         }
//         ShowPanel();
//     }

//     private void OnTabClick(DeviceItem tabItem)
//     {
//         //检查按钮当前所处状态
//         CheckMarkState();

//         if (currentSelected == tabItem)
//             return;
//         if (currentSelected)
//         {
//             currentSelected.SetSelect(false);
//             //点击折叠上一个列表
//             //if (currentSelected.DeviceData.LevelType == 5&& !currentSelected.DeviceData.DeviceNodeChildren.Contains(tabItem.DeviceData)) //关闭上一个选择的子集
//             //    currentSelected.ChangeChildState(false);
//         }
//         currentSelected = tabItem;
//     }
//     /// <summary>
//     /// 打开面板
//     /// </summary>
//     private void ShowPanel()
//     {
//         transform.SetActive(true);
//         transform.Find("MoveCtr").GetComponent<MoveAnimatorControl>().CheckShow();
//         //检测冲突面板并关闭他们
//         MessageCenter.Instance.Dispatch(MessageCenter.EMessage.ConflictPanelCheck, this.gameObject.name);
//     }
//     /// <summary>
//     /// 响应漫游开始 打开对应标签
//     /// </summary>
//     private void ResponseWalkStart(EWalkType walktype, EFixedPathType pathType)
//     {
//         //用户要求去掉漫游标签 避免后续使用 先咱留
//         HidePanel();
//         ResetPanel();
//         return;
//         HidePanel();
//         if (walktype == EWalkType.FixedPath)
//         {
//             switch (pathType)
//             {
//                 case EFixedPathType.A:
//                     ShowDeviceMarks(MarkType.A线);
//                     return;
//                 case EFixedPathType.B:
//                     ShowDeviceMarks(MarkType.B线);
//                     return;
//                 default:
//                     break;
//             }
//         }
//         //显示所有
//         ShowDeviceMarks();
//     }
//     /// <summary>
//     /// All 全部  A A线  B B线
//     /// </summary>
//     /// <param name="GroupName"></param>
//     private void ShowDeviceMarks(MarkType markType = MarkType.All)
//     {
//         if (markType == MarkType.None)
//         {
//             ResetPanel(); //重置关闭面板
//             return;
//         }

//         Guid parent = Guid.Empty;
//         switch (markType)   
//         {//这样写不能保证数据的正确性 后续需要改动
//             case MarkType.A线:
//                 parent = devicesitems[0].DeviceData.NodeID;
//                 break;
//             case MarkType.B线:
//                 parent = devicesitems[1].DeviceData.NodeID;
//                 break;
//             default:
//                 break;
//         }

//         foreach (var item in devicesitems)
//         {
//             foreach (var GYItem in item.DeviceData.DeviceNodeChildren)
//             {
//                 if (GYItem.ParentNodeID == parent || parent == Guid.Empty)
//                 {
//                     item.ChangeAllChildState(true);
//                 }
//                 else
//                 {
//                     item.ChangeAllChildState(false);
//                 }
//             }
//         }
//     }

//     //检查并更新标签眼睛状态
//     private void CheckMarkState()
//     {
//         bool ChildHaveOpen = true;
//         foreach (var item in devicesitems)
//         {
//             if (!item.CheckAllChildState())
//             {
//                 ChildHaveOpen = false;
//                 break;
//             }
//         }
//         BtnMark.IsLight = ChildHaveOpen;
//     }

//     //点击标签展开按钮
//     private void ClickBtnMark()
//     {
//         if (BtnMark.IsLight)
//         {
//             //所有面板全部被开启了 调用重置
//             ResetPanel();
//         }
//         else 
//         {
//             ShowDeviceMarks();
//         }
//     }

//     /// <summary>
//     /// 响应漫游结束
//     /// </summary>
//     private void ResponseWalkEnd()
//     {
//         ResetPanel();
//         ShowPanel();
//     }
//     /// <summary>
//     /// 关闭面板
//     /// </summary>
//     private void HidePanel()
//     {
//         transform.SetActive(false);
//     }
// }
