using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ShangHaiSYS.CameraControl
{
    /// <summary>
    /// 摄像机当前由哪一套交互逻辑接管。
    /// </summary>
    public enum CameraWorkMode
    {
        [InspectorName("无控制")]
        None,

        [InspectorName("自由相机")]
        FreeCamera,

        [InspectorName("目标环绕")]
        TargetOrbit,

        [InspectorName("仅旋转漫游")]
        RotateOnly
    }

    /// <summary>
    /// 摄像机投影方式。
    /// </summary>
    public enum CameraProjectionMode
    {
        [InspectorName("透视")]
        Perspective,

        [InspectorName("正交")]
        Orthographic
    }

    /// <summary>
    /// 鼠标按键映射，避免在主控制逻辑里散落魔法数字。
    /// </summary>
    public enum MouseButton
    {
        [InspectorName("左键")]
        Left = 0,

        [InspectorName("右键")]
        Right = 1,

        [InspectorName("中键")]
        Middle = 2
    }

    /// <summary>
    /// 透视缩放时摄像机靠近/远离的参考方向。
    /// </summary>
    public enum ZoomFocusMode
    {
        [InspectorName("鼠标落点")]
        MousePoint,

        [InspectorName("画面中心")]
        CameraForward,

        [InspectorName("场景中心")]
        SceneCenter
    }

    /// <summary>
    /// 右键旋转时绕哪个点做环绕，只有自由相机模式会使用。
    /// </summary>
    public enum OrbitPivotMode
    {
        [InspectorName("画面中心落地")]
        ViewCenterGround,

        [InspectorName("场景中心")]
        SceneCenter,

        [InspectorName("目标物体")]
        Target
    }

    /// <summary>
    /// 平面约束方式，用于防止摄像机平移到业务场景外。
    /// </summary>
    public enum PlaneLimitMode
    {
        [InspectorName("不限制")]
        None,

        [InspectorName("矩形范围")]
        Rectangle,

        [InspectorName("半径范围")]
        Radius
    }

    /// <summary>
    /// 可保存和恢复的摄像机状态。
    /// </summary>
    [Serializable]
    public struct CameraPose
    {
        [LabelText("世界位置")]
        // 使用世界坐标保存，避免摄像机父节点变化后恢复位置出现偏移。
        public Vector3 position;

        [LabelText("世界欧拉角")]
        // 保存欧拉角便于在配置文件或 Inspector 中查看和手动调整。
        public Vector3 eulerAngles;

        [LabelText("是否正交")]
        // 位姿中同时记录投影状态，恢复时能完整回到透视或正交视角。
        public bool isOrthographic;

        [LabelText("正交尺寸")]
        // 只有正交模式真正使用，但保存透视位姿时也保留当前值，方便来回切换。
        public float orthographicSize;

        [LabelText("透视视野")]
        // 只有透视模式真正使用，恢复位姿时会钳制在 Camera 合法 FOV 范围内。
        public float fieldOfView;

        public CameraPose(Camera camera)
        {
            if (camera == null)
            {
                // 空相机兜底值用于防御外部误调用，避免保存状态时直接抛异常。
                position = Vector3.zero;
                eulerAngles = Vector3.zero;
                isOrthographic = false;
                orthographicSize = 5f;
                fieldOfView = 60f;
                return;
            }

            position = camera.transform.position;
            eulerAngles = camera.transform.eulerAngles;
            isOrthographic = camera.orthographic;
            orthographicSize = camera.orthographicSize;
            fieldOfView = camera.fieldOfView;
        }
    }

    /// <summary>
    /// 鼠标与输入相关的统一配置。
    /// </summary>
    [Serializable]
    public class CameraInputSettings
    {
        [LabelText("鼠标在 UI 上时禁止控制")]
        // 开启后，鼠标按在按钮、列表、面板等 UI 上不会触发相机旋转或拖拽。
        public bool blockWhenPointerOverUI = true;

        [LabelText("动画定位时禁止输入")]
        // 避免 DOTween 动画和手动输入同时写入相机 Transform，造成抖动或最终位置不确定。
        public bool blockInputDuringTween = true;

        [LabelText("使用不受暂停影响的时间")]
        // 如果项目会暂停 Time.timeScale，但仍希望镜头菜单或预览可操作，可以打开此项。
        public bool useUnscaledTime = false;

        [LabelText("自由旋转按键")]
        // 自由相机透视模式下用于右键旋转/绕点旋转的按键。
        public MouseButton freeRotateButton = MouseButton.Right;

        [LabelText("自由拖拽按键")]
        // 自由相机透视与正交模式共用的拖拽按键。
        public MouseButton freePanButton = MouseButton.Left;

        [LabelText("目标环绕旋转按键")]
        // 目标环绕模式下改变 yaw/pitch 的按键。
        public MouseButton orbitRotateButton = MouseButton.Right;

        [LabelText("目标环绕平移按键")]
        // 目标环绕模式下平移“环绕中心”的按键，不是直接移动目标物体。
        public MouseButton orbitPanButton = MouseButton.Left;

        [LabelText("仅旋转按键")]
        // 仅旋转漫游模式下原地转向使用的按键。
        public MouseButton rotateOnlyButton = MouseButton.Right;

        [LabelText("仅旋转平移按键")]
        // 仅旋转漫游模式下沿相机本地 right/up 平移的按键。
        public MouseButton rotateOnlyPanButton = MouseButton.Middle;
    }

    /// <summary>
    /// 场景空间范围，所有模式都会复用这里的边界数据。
    /// </summary>
    [Serializable]
    public class CameraRangeSettings
    {
        [LabelText("场景中心")]
        // 所有距离限制、半径限制、目标为空时的默认环绕点都以该点为基准。
        public Vector3 sceneCenter = Vector3.zero;

        [LabelText("场景尺寸")]
        // 用于描述业务场景的大致体积；矩形范围未配置时会使用 X/Z 自动推导边界。
        public Vector3 sceneSize = new Vector3(200f, 80f, 200f);

        [LabelText("限制距离和高度")]
        // 关闭后只保留平面范围限制，相机可以自由靠近、远离或改变高度。
        public bool limitDistanceAndHeight = true;

        [ShowIf(nameof(limitDistanceAndHeight))]
        [LabelText("最小距离")]
        [MinValue(0.01f)]
        // 相机距离 sceneCenter 过近时会被推出，避免穿入建筑中心或目标内部。
        public float minDistance = 2f;

        [ShowIf(nameof(limitDistanceAndHeight))]
        [LabelText("最大距离")]
        [MinValue(0.01f)]
        // 相机距离 sceneCenter 过远时会被拉回，避免飞出业务范围。
        public float maxDistance = 200f;

        [ShowIf(nameof(limitDistanceAndHeight))]
        [LabelText("最低高度")]
        // 世界坐标高度下限，常用于防止镜头进入地面或地下管廊以下。
        public float minHeight = 0.5f;

        [LabelText("平面范围限制")]
        // 只限制 XZ 平面，可选矩形或圆形区域。
        public PlaneLimitMode planeLimitMode = PlaneLimitMode.None;

        [ShowIf(nameof(planeLimitMode), PlaneLimitMode.Rectangle)]
        [LabelText("矩形范围 XZ")]
        // 矩形宽度使用 x，深度使用 y，对应世界坐标的 X/Z。
        public Vector2 rectangleSize = new Vector2(200f, 200f);

        [ShowIf(nameof(planeLimitMode), PlaneLimitMode.Radius)]
        [LabelText("平面半径")]
        [MinValue(0.01f)]
        // 圆形平面范围半径，中心点为 sceneCenter。
        public float planeRadius = 100f;

        [LabelText("鼠标落点超出范围时回中心")]
        // 鼠标滚轮按落点缩放时，如果落点在场景矩形外，则改为朝场景中心缩放。
        public bool zoomMouseOutOfRangeUseCenter = true;
    }

    /// <summary>
    /// 自由相机模式下的旋转、拖拽、滚轮配置。
    /// </summary>
    [Serializable]
    public class FreeCameraSettings
    {
        [FoldoutGroup("透视旋转")]
        [LabelText("启用右键旋转")]
        // 关闭后自由相机透视模式下不会响应旋转按键，但拖拽和滚轮仍可单独工作。
        public bool enableRotation = true;

        [FoldoutGroup("透视旋转")]
        [LabelText("水平旋转速度")]
        // 数值越大，鼠标左右移动时 yaw 改变量越大。
        public float rotateHorizontalSpeed = 180f;

        [FoldoutGroup("透视旋转")]
        [LabelText("垂直旋转速度")]
        // 数值越大，鼠标上下移动时 pitch 改变量越大。
        public float rotateVerticalSpeed = 180f;

        [FoldoutGroup("透视旋转")]
        [LabelText("最小俯仰角")]
        // 负值表示允许向下看；与最大俯仰角共同限制镜头上下翻转范围。
        public float minPitch = -80f;

        [FoldoutGroup("透视旋转")]
        [LabelText("最大俯仰角")]
        // 正值表示允许向上看；建议不要超过 89，避免接近垂直时操作方向突变。
        public float maxPitch = 80f;

        [FoldoutGroup("透视旋转")]
        [LabelText("旋转平滑")]
        // 开启后使用插值靠近目标旋转，关闭则立即应用角度。
        public bool smoothRotation = true;

        [FoldoutGroup("透视旋转")]
        [ShowIf(nameof(smoothRotation))]
        [LabelText("旋转平滑速度")]
        public float rotationSmoothSpeed = 18f;

        [FoldoutGroup("透视绕点")]
        [LabelText("右键同时绕点环绕")]
        // 开启后右键旋转不仅改变角度，还会围绕一个中心点移动相机位置。
        public bool orbitWhileRotating = true;

        [FoldoutGroup("透视绕点")]
        [LabelText("环绕中心")]
        // 决定自由相机绕点旋转时使用画面中心落地点、场景中心还是目标物体。
        public OrbitPivotMode orbitPivotMode = OrbitPivotMode.ViewCenterGround;

        [FoldoutGroup("透视绕点")]
        [LabelText("落点最大偏离中心")]
        [MinValue(0f)]
        public float maxPivotOffsetFromCenter = 120f;

        [FoldoutGroup("透视绕点")]
        [LabelText("低空环绕高度")]
        // 低于该高度时会再检查半径，半径过小时只原地旋转，避免近距离绕点眩晕。
        public float lowHeightOrbitThreshold = 40f;

        [FoldoutGroup("透视绕点")]
        [LabelText("低空环绕最小半径")]
        [MinValue(0f)]
        public float lowHeightMinOrbitRadius = 80f;

        [FoldoutGroup("透视缩放")]
        [LabelText("启用滚轮靠近远离")]
        public bool enablePerspectiveZoom = true;

        [FoldoutGroup("透视缩放")]
        [LabelText("滚轮参考方向")]
        // 鼠标落点更适合大地图浏览，画面中心更接近传统飞行相机，场景中心适合建筑总览。
        public ZoomFocusMode zoomFocusMode = ZoomFocusMode.MousePoint;

        [FoldoutGroup("透视缩放")]
        [LabelText("滚轮强度")]
        public float perspectiveZoomStrength = 80f;

        [FoldoutGroup("透视缩放")]
        [LabelText("高度速度曲线")]
        // 横轴通常是相机高度比例，纵轴是速度倍率；可让低空更细，高空更快。
        public AnimationCurve perspectiveZoomCurve = AnimationCurve.Linear(0f, 0.25f, 1f, 1f);

        [FoldoutGroup("透视缩放")]
        [LabelText("缩放平滑")]
        public bool smoothPerspectiveZoom = true;

        [FoldoutGroup("透视缩放")]
        [ShowIf(nameof(smoothPerspectiveZoom))]
        [LabelText("缩放平滑速度")]
        public float perspectiveZoomSmoothSpeed = 16f;

        [FoldoutGroup("透视拖拽")]
        [LabelText("启用拖拽平移")]
        public bool enablePerspectivePan = true;

        [FoldoutGroup("透视拖拽")]
        [LabelText("优先使用地面锚点拖拽")]
        // 开启后会用射线落地方式拖拽，鼠标按住的地面点会更稳定地跟随光标。
        public bool useGroundAnchorPan = true;

        [FoldoutGroup("透视拖拽")]
        [LabelText("拖拽强度")]
        public float perspectivePanStrength = 0.06f;

        [FoldoutGroup("透视拖拽")]
        [LabelText("高度速度曲线")]
        public AnimationCurve perspectivePanCurve = AnimationCurve.Linear(0f, 0.25f, 1f, 1f);

        [FoldoutGroup("透视拖拽")]
        [LabelText("拖拽允许抬升")]
        // 关闭时拖拽只在水平面移动；开启后会混入相机 up 方向，形成带高度变化的拖拽。
        public bool panCanMoveUp = false;

        [FoldoutGroup("透视拖拽")]
        [LabelText("拖拽平滑")]
        public bool smoothPerspectivePan = true;

        [FoldoutGroup("透视拖拽")]
        [ShowIf(nameof(smoothPerspectivePan))]
        [LabelText("拖拽平滑速度")]
        public float perspectivePanSmoothSpeed = 20f;

        [FoldoutGroup("键盘移动")]
        [LabelText("启用 WASD 移动")]
        // 开启后自由相机在透视和正交模式下都可以用 WASD 沿水平面前后左右移动。
        public bool enableKeyboardMove = true;

        [FoldoutGroup("键盘移动")]
        [LabelText("WASD 移动速度")]
        // 单位是世界坐标/秒；移动方向使用相机朝向的水平投影，避免俯仰角影响高度。
        public float keyboardMoveSpeed = 20f;

        [FoldoutGroup("键盘移动")]
        [LabelText("WASD 移动平滑")]
        // 开启后键盘移动会插值靠近目标位置，手感更柔和；关闭则按当前帧输入立即位移。
        public bool smoothKeyboardMove = false;

        [FoldoutGroup("键盘移动")]
        [ShowIf(nameof(smoothKeyboardMove))]
        [LabelText("WASD 平滑速度")]
        public float keyboardMoveSmoothSpeed = 20f;

        [FoldoutGroup("正交缩放")]
        [LabelText("启用正交滚轮缩放")]
        public bool enableOrthographicZoom = true;

        [FoldoutGroup("正交缩放")]
        [LabelText("正交缩放强度")]
        public float orthographicZoomStrength = 12f;

        [FoldoutGroup("正交缩放")]
        [LabelText("最小正交尺寸")]
        [MinValue(0.01f)]
        // 正交尺寸越小画面越近，该值限制最大放大程度。
        public float minOrthographicSize = 5f;

        [FoldoutGroup("正交缩放")]
        [LabelText("最大正交尺寸")]
        [MinValue(0.01f)]
        // 正交尺寸越大画面越远，该值限制最大俯瞰范围。
        public float maxOrthographicSize = 120f;

        [FoldoutGroup("正交缩放")]
        [LabelText("正交缩放平滑")]
        public bool smoothOrthographicZoom = true;

        [FoldoutGroup("正交缩放")]
        [ShowIf(nameof(smoothOrthographicZoom))]
        [LabelText("正交缩放平滑速度")]
        public float orthographicZoomSmoothSpeed = 18f;

        [FoldoutGroup("正交拖拽")]
        [LabelText("启用正交拖拽")]
        public bool enableOrthographicPan = true;

        [FoldoutGroup("正交拖拽")]
        [LabelText("正交拖拽强度")]
        public float orthographicPanStrength = 1f;

        [FoldoutGroup("正交拖拽")]
        [LabelText("正交拖拽平滑")]
        public bool smoothOrthographicPan = true;

        [FoldoutGroup("正交拖拽")]
        [ShowIf(nameof(smoothOrthographicPan))]
        [LabelText("正交拖拽平滑速度")]
        public float orthographicPanSmoothSpeed = 20f;
    }

    /// <summary>
    /// 目标环绕模式配置，适合查看设备、建筑、模型等对象。
    /// </summary>
    [Serializable]
    public class TargetOrbitSettings
    {
        [LabelText("目标偏移")]
        // 在目标物体位置基础上额外偏移环绕中心，例如希望绕模型上半身或建筑中心层旋转时使用。
        public Vector3 targetOffset = Vector3.zero;

        [LabelText("启用旋转")]
        // 关闭后目标环绕模式仍可保留滚轮缩放和平移中心。
        public bool enableRotation = true;

        [LabelText("水平旋转速度")]
        // 控制鼠标左右拖动时围绕目标的水平转速。
        public float rotateHorizontalSpeed = 220f;

        [LabelText("垂直旋转速度")]
        // 控制鼠标上下拖动时相机俯仰角变化速度。
        public float rotateVerticalSpeed = 180f;

        [LabelText("最小俯仰角")]
        public float minPitch = -75f;

        [LabelText("最大俯仰角")]
        public float maxPitch = 80f;

        [LabelText("启用滚轮距离")]
        // 目标环绕中的滚轮改变相机与环绕中心的距离，不改变 Camera.fieldOfView。
        public bool enableZoom = true;

        [LabelText("滚轮强度")]
        // 与当前距离相乘使用，距离越远单次滚轮移动越大。
        public float zoomStrength = 0.8f;

        [LabelText("最小距离")]
        [MinValue(0.01f)]
        public float minDistance = 1f;

        [LabelText("最大距离")]
        [MinValue(0.01f)]
        public float maxDistance = 200f;

        [LabelText("启用目标平移")]
        // 这里平移的是环绕中心偏移量，适合查看目标的局部区域。
        public bool enablePan = true;

        [LabelText("目标平移强度")]
        // 与当前环绕距离相乘，保证远距离观察时平移不会过慢。
        public float panStrength = 0.015f;

        [LabelText("启用 WASD 移动")]
        // 开启后目标环绕模式会用 WASD 平移环绕中心，保持当前观察角度和距离不变。
        public bool enableKeyboardMove = true;

        [LabelText("WASD 移动速度")]
        // 单位是世界坐标/秒；实际相机位置仍由环绕中心、角度和距离统一计算。
        public float keyboardMoveSpeed = 20f;

        [LabelText("启用空闲自动旋转")]
        // 开启后，用户停止操作一段时间后相机会缓慢水平旋转。
        public bool enableIdleAutoRotate = false;

        [ShowIf(nameof(enableIdleAutoRotate))]
        [LabelText("空闲等待时间")]
        [MinValue(0f)]
        // 用户最后一次手动操作后等待多少秒开始自动旋转。
        public float idleDelay = 2f;

        [ShowIf(nameof(enableIdleAutoRotate))]
        [LabelText("自动旋转速度")]
        public float idleRotateSpeed = 12f;

        [LabelText("环绕平滑")]
        public bool smoothOrbit = true;

        [ShowIf(nameof(smoothOrbit))]
        [LabelText("环绕平滑速度")]
        public float orbitSmoothSpeed = 16f;

        [LabelText("根据目标包围盒设定距离")]
        // 设置目标或点击“根据目标重新取景”时，自动估算一个能看完整目标的距离。
        public bool fitDistanceFromTargetBounds = true;

        [ShowIf(nameof(fitDistanceFromTargetBounds))]
        [LabelText("包围盒距离倍率")]
        [MinValue(0.01f)]
        // 倍率越大，相机离目标越远，画面留白越多。
        public float boundsDistanceScale = 1.8f;
    }

    /// <summary>
    /// 仅旋转漫游模式配置，适合不需要固定目标点的轻量查看。
    /// </summary>
    [Serializable]
    public class RotateOnlySettings
    {
        [LabelText("启用旋转")]
        // 关闭后仅旋转漫游模式只保留滚轮前后移动和平移功能。
        public bool enableRotation = true;

        [LabelText("水平旋转速度")]
        public float rotateHorizontalSpeed = 160f;

        [LabelText("垂直旋转速度")]
        public float rotateVerticalSpeed = 140f;

        [LabelText("最小俯仰角")]
        public float minPitch = -80f;

        [LabelText("最大俯仰角")]
        public float maxPitch = 80f;

        [LabelText("启用滚轮前后移动")]
        // 与自由相机滚轮缩放不同，这里直接沿当前 forward 移动。
        public bool enableScrollMove = true;

        [LabelText("滚轮移动速度")]
        // 数值越大，滚轮前后移动越快。
        public float scrollMoveSpeed = 30f;

        [LabelText("启用 WASD 移动")]
        // 开启后仅旋转漫游模式可用 WASD 沿相机当前水平朝向移动，适合边看边走。
        public bool enableKeyboardMove = true;

        [LabelText("WASD 移动速度")]
        // 单位是世界坐标/秒；只改变位置，不改变当前旋转角度。
        public float keyboardMoveSpeed = 20f;

        [LabelText("WASD 移动平滑")]
        // 开启后键盘移动走平滑插值，关闭时更接近第一人称漫游的即时响应。
        public bool smoothKeyboardMove = false;

        [ShowIf(nameof(smoothKeyboardMove))]
        [LabelText("WASD 平滑速度")]
        public float keyboardMoveSmoothSpeed = 20f;

        [LabelText("启用平移")]
        // 中键拖拽时沿相机本地 right/up 方向移动。
        public bool enablePan = true;

        [LabelText("平移速度")]
        // 仅旋转模式的平移不做地面锚点换算，适合作为局部微调。
        public float panSpeed = 0.35f;

        [LabelText("旋转平滑")]
        public bool smoothRotation = true;

        [ShowIf(nameof(smoothRotation))]
        [LabelText("旋转平滑速度")]
        public float rotationSmoothSpeed = 18f;
    }
}
