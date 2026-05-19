// using Newtonsoft.Json;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Text.RegularExpressions;
// using TMPro;
// //using TreeEditor;
// using UnityEngine;
// using UnityEngine.UI;
// using static HighLightManage;

// public class DeviceItem : MonoBehaviour
// {
//     public DeviceNode DeviceData;
//     //用于控制箭头是否被展示
//     public GameObject ArrowGroup,ArrowRight, ArrowDown;

//     public GameObject onSelect;
//     //当前使用的组件
//     private TextMeshProUGUI CurrentTxt;
//     private Button CurrentBtn;
//     public List<DeviceItem> childList =new List<DeviceItem>();  //子物体集合
//     public bool isOpen = false;          //子物体开启状态
//     private Vector2 startSize;           //起始大小
//     private GameObject CurrentObj;      //当前选中的模型

//     private void Reset()
//     {
//         ArrowGroup = transform.Find("ContentPanel/Arrow").gameObject;
//         ArrowRight = ArrowGroup.transform.Find("Right").gameObject;
//         ArrowDown = ArrowGroup.transform.Find("Down").gameObject;
//         onSelect = transform.Find("ContentPanel/ThreeBtn/onSelect").gameObject;
//     }

//     private void InitPanel()
//     {   //初始化关闭所有按钮 根据需求打开
//         transform.Find("ContentPanel/FirstBtn").gameObject.SetActive(false);
//         transform.Find("ContentPanel/SecondBtn").gameObject.SetActive(false);
//         transform.Find("ContentPanel/ThreeBtn").gameObject.SetActive(false);
//         startSize= this.GetComponent<RectTransform>().sizeDelta;
//     }
//     private void InitInfo()
//     {
//         switch (DeviceData.LevelType)
//         {
//             case 4:
//                 CurrentBtn = transform.Find("ContentPanel/FirstBtn").GetComponent<Button>();
//                 CurrentTxt = CurrentBtn.transform.Find("NameTxt").GetComponent<TextMeshProUGUI>();
//                 CurrentBtn.SetActive(true);
//                 onSelect = transform.Find("ContentPanel/FirstBtn/onSelect").gameObject;
//                 break;
//             case 5:
//                 CurrentBtn = transform.Find("ContentPanel/SecondBtn").GetComponent<Button>();
//                 CurrentTxt = CurrentBtn.transform.Find("NameTxt").GetComponent<TextMeshProUGUI>();
//                 CurrentBtn.SetActive(true);
//                 onSelect = transform.Find("ContentPanel/SecondBtn/onSelect").gameObject;
//                 break;
//             case 6:
//                 CurrentBtn = transform.Find("ContentPanel/ThreeBtn").GetComponent<Button>();
//                 CurrentTxt = CurrentBtn.transform.Find("NameTxt").GetComponent<TextMeshProUGUI>();
//                 CurrentBtn.SetActive(true);
//                 onSelect = transform.Find("ContentPanel/ThreeBtn/onSelect").gameObject;
//                 break;
//             default:
//                 break;
//         }
//         CurrentTxt.text = DeviceData.NodeName;
//         CurrentBtn.onClick.AddListener(OnDeviceItemBtnClick);
//         try
//         {
//             CurrentObj = SceneObjManager.Instance.Environment.Find(DeviceData.UnityScenePath).gameObject;
//         }
//         catch (Exception ex) 
//         {
//             Debug.LogError("错误地址 :" + DeviceData.UnityScenePath);
//         }

        
//     }
//     /// <summary>
//     /// 子物体创建出来
//     /// </summary>
//     private void CreatchildList(GameObject deviceItem)
//     {
//         foreach (var item in DeviceData.DeviceNodeChildren)
//         {
//             GameObject newDeviceItem = Instantiate(deviceItem, transform);
//             newDeviceItem.name = item.NodeName;
//             newDeviceItem.GetComponent<DeviceItem>().InitPanelContent(item,deviceItem);
//             newDeviceItem.SetActive(false);
//             childList.Add(newDeviceItem.GetComponent<DeviceItem>());
//         }

       
//         if (DeviceData.DeviceNodeChildren.Count > 0)
//         {
//             ChangeArrowState(false);
//         }
//         else
//         {
//             ChangeArrowState(true,true);
//         }
//     }

//     private void OnDeviceItemBtnClick()
//     {
//         //要求只有设备层可以定位
//         if (DeviceData.LevelType == 6)
//         {
//             isOpen = false;
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.CameraMoveDevicePos, DeviceData.Pos, CurrentObj);
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.EffectSelectedObj,CurrentObj.transform, HighLightType.EffectSelectedObj, 2f);
//         }
//         else
//         {
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.CameraPatternChange, CameraPattern.FreePattern);
//             CameraControllManage.Instance.ResetCameraPosition(DeviceData.Pos);
//         }
//         ChangeChildState(!isOpen);
//     }

//     /// <summary>
//     /// 增加一个子物体后更新Panel大小
//     /// </summary>
//     /// <param name="change"></param>
//     public void UpdateRectTranSize(int change)
//     {
//         this.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(startSize.x, this.gameObject.GetComponent<RectTransform>().sizeDelta.y + change);
//     }
//     /// <summary>
//     /// 增加父物体高度
//     /// </summary>
//     /// <param name="parentItem"></param>
//     /// <param name="change"></param>
//     public void AddParentSize(int change)
//     {
//         if (this.transform.parent.GetComponent<DeviceItem>() != null)
//         {
//             this.transform.parent.GetComponent<DeviceItem>().UpdateRectTranSize(change);
//             this.transform.parent.GetComponent<DeviceItem>().AddParentSize(change);
//         }
//     }


//     /// <summary>
//     /// 改变子物体状态
//     /// </summary>
//     public void ChangeChildState(bool state)
//     { 
//         if(isOpen == state)
//             return;
//         isOpen = state;
//         SetSelect(isOpen);

//         foreach (DeviceItem child in childList)
//         {
//             //优先处理关闭下层标签 再计算上层标签
//             if (DeviceData.LevelType < 5 && !state)
//             {
//                 //生产线层触发关闭下层标签
//                 child.ChangeChildState(state);
//             }
//             child.SetActive(isOpen);
//             int changeSize = isOpen ? 1 : -1;
//             child.AddParentSize(changeSize*(int)child.gameObject.GetComponent<RectTransform>().sizeDelta.y);
//         }
//         ChangeArrowState(isOpen);

//         MessageCenter.Instance.Dispatch(MessageCenter.EMessage.DeviceListSelected, this);
//     }

//     /// <summary>
//     /// 改变所有子物体的状态 包括孙子级别
//     /// </summary>
//     public void ChangeAllChildState(bool state)
//     {   //这里不去计算面板尺寸 只适用于打开标签 打开后设备面板需要调用关闭掉 避免界面错乱
//         foreach (DeviceItem child in childList)
//         {
//             ChangeChildState(state);
//             child.ChangeAllChildState(state);
//         }
//     }
//     /// <summary>
//     /// 用于检测子物体有没有被打开，有一个被关闭就会返回false
//     /// </summary>
//     public bool CheckAllChildState()
//     {
//         bool ChildHaveOpen= true;

//         foreach (DeviceItem child in childList)
//         {
//             if (!child.isOpen)
//             {
//                 ChildHaveOpen = false;
//                 break;
//             }
//         }
//         return ChildHaveOpen;
//     }


//     //填充Item数据
//     public void InitPanelContent(DeviceNode data,GameObject DeviceItem)
//     {
//         DeviceData = data;
//         InitPanel();
//         InitInfo();
//         CreatchildList(DeviceItem);
//     }
    
//     public void SetSelect(bool isSelect)
//     {
//         onSelect.SetActive(isSelect);
//         CurrentTxt.color = isSelect?new Color(0.396f, 0.667f,1f,1f): Color.white;
//     }
//     //更改箭头状态
//     private void ChangeArrowState(bool state,bool isClose =false)
//     {
//         if (isClose)
//         ArrowGroup.SetActive(false);

//         ArrowRight.SetActive(!state);
//         ArrowDown.SetActive(state);
//     }
// }
