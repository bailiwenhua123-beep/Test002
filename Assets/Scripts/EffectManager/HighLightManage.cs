// using DG.Tweening;
// using HighlightPlus;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class HighLightManage : MonoBehaviour
// {
//     /// <summary>
//     /// 物体高亮对应的告警信息的类型
//     /// </summary>
//     public enum HighLightType
//     {
//         None,
//         /// <summary>
//         /// 故障告警高亮特效，红色高亮闪烁
//         /// </summary>
//         EffectErrorInfo,

//         /// <summary>
//         /// 严重告警高亮特效，黄色高亮闪烁
//         /// </summary>
//         EffectSeriousWarningInfo,

//         /// <summary>
//         /// 普通告警高亮特效，绿色高亮闪烁
//         /// </summary>
//         EffectWarningInfo,

//         /// <summary>
//         /// 提示选中的类型特效
//         /// </summary>
//         EffectSelectedObj,


//     }

//     HighlightEffect effect;
//     public Transform rootTrans;
//     public HighLightType highLightType = HighLightType.None;
//     public HighlightProfile highlightProfile_ErrorInfo;
//     public HighlightProfile highlightProfile_SeriousWarningInfo;
//     public HighlightProfile highlightProfile_WarningInfo;
//     public HighlightProfile highlightProfile_TipsInfo;

//     public Dictionary<Transform, Tween> dicTween = new Dictionary<Transform, Tween>();

//     // Start is called before the first frame update
//     void Start()
//     {
//         MessageCenter.Instance.Register<string>(MessageCenter.EMessage.VanishWarningInfo, VanishWarningInfo);
//         MessageCenter.Instance.Register<string, HighLightType>(MessageCenter.EMessage.GenerateWarningInfo, GenerateWarningInfo);
//         MessageCenter.Instance.Register<Transform, HighLightType, float>(MessageCenter.EMessage.EffectSelectedObj, EnableEffect);
//         MessageCenter.Instance.Register<Transform>(MessageCenter.EMessage.HideEffectSelectedObj, DisableEffect);
//     }

//     void VanishWarningInfo(string info)
//     {
//         Transform transTarget = null;
//         string objName = info;
//         DeviceNode deviceInfo = DeviceDataManager.AllDevices[objName];
//         if (deviceInfo != null)
//         {
//             transTarget = rootTrans.Find(deviceInfo.UnityScenePath);
//         }
//         if (transTarget)
//         {
//             DisableEffect(transTarget);
//         }
//     }

//     void GenerateWarningInfo(string info, HighLightType highLightType)
//     {
//         Transform transTarget = null;
//         string objName = info;
//         DeviceNode deviceInfo = DeviceDataManager.AllDevices[objName];
//         if (deviceInfo != null)
//         {
//             transTarget = rootTrans.Find(deviceInfo.UnityScenePath);
//         }
//         if (transTarget)
//         {
//             EnableEffect(transTarget, highLightType, 2);
//         }
//     }


//     /// <summary>
//     /// 开启高亮特效
//     /// </summary>
//     /// <param name="targetObj">高亮特效的目标物体</param>
//     /// <param name="highLightType">高亮特效类型</param>
//     /// <param name="durTime">特效持续时间:0=一直高亮</param>
//     public void EnableEffect(Transform targetObj, HighLightType highLightType, float durTime = 0)
//     {
//         if (targetObj == null)
//             return;
//         effect = targetObj.GetOrAddComponent<HighlightEffect>();
//         switch (highLightType)
//         {
//             case HighLightType.EffectErrorInfo:
//                 effect.ProfileLoad(highlightProfile_ErrorInfo);
//                 if (dicTween.TryGetValue(targetObj, out Tween tween0))
//                 {
//                     tween0?.Kill();
//                 }
//                 //todo 闪烁，关闭，闪烁，直到接收到关闭信号，才关闭特效
//                 break;
//             case HighLightType.EffectWarningInfo:
//             case HighLightType.EffectSeriousWarningInfo:
//                 effect.ProfileLoad(highlightProfile_SeriousWarningInfo);
//                 if (dicTween.TryGetValue(targetObj, out Tween tween1))
//                 {
//                     tween1?.Kill();
//                 }
//                 //todo 闪烁，关闭，闪烁，直到接收到关闭信号，才关闭特效
//                 break;

//             case HighLightType.EffectSelectedObj:
//                 effect.ProfileLoad(highlightProfile_TipsInfo);
//                 //todo time=0则一直高亮，time>0显示3s关闭特效
//                 if (durTime == 0)
//                 {
//                     if (dicTween.TryGetValue(targetObj, out Tween tween))
//                     {
//                         tween?.Kill();
//                     }
//                 }
//                 else
//                 {
//                     if (dicTween.TryGetValue(targetObj, out Tween tween))
//                     {
//                         if (tween != null)
//                         {
//                             tween.Kill();
//                             tween = transform.DOScaleX(transform.localScale.x, durTime).OnComplete(() =>
//                             {
//                                 DisableEffect(targetObj);
//                                 dicTween.Remove(targetObj);
//                             });
//                             dicTween[targetObj] = tween;
//                         }
//                     }
//                     else
//                     {
//                         tween = transform.DOScaleX(transform.localScale.x, durTime).OnComplete(() =>
//                         {
//                             DisableEffect(targetObj);
//                             dicTween.Remove(targetObj);
//                         });
//                         dicTween.Add(targetObj, tween);
//                     }
//                 }

//                 break;
//         }

//         effect.highLightType = highLightType;
//         effect.SetHighlighted(true);
//     }

//     /// <summary>
//     /// 关闭高亮效果
//     /// </summary>
//     /// <param name="targetObj"></param>
//     public void DisableEffect(Transform targetObj)
//     {
//         if (targetObj.TryGetComponent(out effect))
//         {
//             effect.SetHighlighted(false);
//         }
//     }

//     /// <summary>
//     /// 判断指定的物体是否已添加高亮效果组件，true:已添加，false:未添加
//     /// </summary>
//     /// <param name="targetObj"></param>
//     /// <returns></returns>
//     public bool IsHasEffect(Transform targetObj)
//     {
//         bool ishasEffect = false;
//         if (targetObj)
//         {
//             if (targetObj.TryGetComponent(out effect))
//             {
//                 ishasEffect = true;
//                 return ishasEffect;
//             }
//         }

//         return ishasEffect;
//     }

//     // Update is called once per frame
//     void Update()
//     {

// #if  UNITY_EDITOR

//         if (Input.GetKeyDown(KeyCode.Alpha1))
//         {
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.GenerateWarningInfo, "A_H920_1", HighLightType.EffectErrorInfo);
//         }
//         if (Input.GetKeyDown(KeyCode.Alpha2))
//         {
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.GenerateWarningInfo, "A_H920_1", HighLightType.EffectSeriousWarningInfo);
//         }
//         if (Input.GetKeyDown(KeyCode.Alpha3))
//         {
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.GenerateWarningInfo, "A_H920_1", HighLightType.EffectSelectedObj);
//         }
//         if (Input.GetKeyDown(KeyCode.Alpha4))
//         {
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.GenerateWarningInfo, "A_WaiGuanGongHao_2", HighLightType.EffectErrorInfo);
//         }
//         if (Input.GetKeyDown(KeyCode.Alpha5))
//         {
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.GenerateWarningInfo, "A_WaiGuanGongHao_2", HighLightType.EffectSelectedObj);
//         }
//         if (Input.GetKeyDown(KeyCode.Alpha6))
//         {
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.GenerateWarningInfo, "A_WaiGuanGongHao_2", HighLightType.EffectSeriousWarningInfo);
//         }
//         if (Input.GetKeyDown(KeyCode.Alpha7))
//         {
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.VanishWarningInfo, "A_H920_1");
//         }
//         if (Input.GetKeyDown(KeyCode.Alpha8))
//         {
//             MessageCenter.Instance.Dispatch(MessageCenter.EMessage.VanishWarningInfo, "A_WaiGuanGongHao_2");
//         }
// #endif

//     }
// }