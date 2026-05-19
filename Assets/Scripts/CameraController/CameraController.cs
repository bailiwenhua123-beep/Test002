using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ShangHaiSYS.CameraControl
{
    /// <summary>
    /// 独立摄像机控制器。
    /// 
    /// 设计目标：
    /// 1. 保留原相机模块中常用的交互能力，例如自由旋转、绕点旋转、滚轮缩放、拖拽平移、正交视口缩放等。
    /// 2. 不依赖旧脚本的实现细节，所有输入、范围、动画、状态缓存都在本脚本组中重新组织。
    /// 3. 把“运行逻辑”和“参数配置”分离，Inspector 中暴露的中文参数集中放在 Settings 类里，便于策划或场景人员调参。
    /// 4. 对外提供切换模式、切换投影、设置目标、保存/恢复位姿等接口，方便业务脚本直接调用。
    /// 
    /// 使用方式：
    /// 把本组件挂在需要控制的 Camera 上即可；如果希望环绕某个对象，把 targetObject 指向该对象。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class ShanghaiCameraController : MonoBehaviour
    {
        public static ShanghaiCameraController Instance { get; private set; }

        [TitleGroup("基础")]
        [LabelText("受控摄像机")]
        // 允许手动指定摄像机；未指定时会自动取当前物体上的 Camera。
        public Camera controlledCamera;

        [TitleGroup("基础")]
        [LabelText("目标物体")]
        // 目标环绕模式下优先围绕该对象旋转；为空时回退到“场景中心”。
        public Transform targetObject;

        [TitleGroup("基础")]
        [LabelText("工作模式")]
        // 决定 LateUpdate 中走哪一套交互逻辑。
        public CameraWorkMode workMode = CameraWorkMode.FreeCamera;

        [TitleGroup("基础")]
        [LabelText("投影模式")]
        // 用枚举维护投影状态，避免外部脚本直接改 Camera.orthographic 后内部状态不同步。
        public CameraProjectionMode projectionMode = CameraProjectionMode.Perspective;

        [TitleGroup("基础")]
        [LabelText("启用鼠标输入")]
        // 只关闭鼠标交互，不影响外部脚本调用 MoveToPose、ApplyPose 等接口。
        public bool enableMouseInput = true;

        [TitleGroup("基础")]
        [LabelText("运行时同步投影模式")]
        // 打开后 Inspector 或外部脚本改 projectionMode 会在下一帧同步到 Camera。
        public bool syncProjectionAtRuntime = true;

        [TitleGroup("配置")]
        [LabelText("输入配置")]
        public CameraInputSettings inputSettings = new CameraInputSettings();

        [TitleGroup("配置")]
        [LabelText("场景范围")]
        public CameraRangeSettings rangeSettings = new CameraRangeSettings();

        [TitleGroup("配置")]
        [LabelText("自由相机")]
        public FreeCameraSettings freeSettings = new FreeCameraSettings();

        [TitleGroup("配置")]
        [LabelText("目标环绕")]
        public TargetOrbitSettings targetOrbitSettings = new TargetOrbitSettings();

        [TitleGroup("配置")]
        [LabelText("仅旋转漫游")]
        public RotateOnlySettings rotateOnlySettings = new RotateOnlySettings();

        [TitleGroup("调试")]
        [LabelText("调试相机状态")]
        public CameraPose debugPose;

        [TitleGroup("运行状态")]
        [ReadOnly]
        [LabelText("当前环绕中心")]
        // 调试用：显示自由绕点或目标环绕当前使用的中心点。
        public Vector3 currentOrbitPivot;

        [TitleGroup("运行状态")]
        [ReadOnly]
        [LabelText("动画定位中")]
        // DOTween 位姿动画播放时置为 true，可配合 inputSettings.blockInputDuringTween 禁止鼠标打断。
        public bool isTweening;

        [TitleGroup("调试/场景范围绘制")]
        [LabelText("绘制场景范围")]
        // 编辑器调试开关。关闭后不绘制任何范围 Gizmos，避免场景视图过于杂乱。
        public bool drawSceneRangeGizmos = true;

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("仅选中时绘制")]
        // 关闭时即使未选中摄像机也能看到范围；打开时只有选中本组件所在物体才显示。
        public bool drawRangeOnlyWhenSelected = false;

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("绘制场景尺寸框")]
        // sceneSize 的三维线框，适合确认业务场景大致体积。
        public bool drawSceneSizeBox = true;

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("绘制平面边界")]
        // 根据 planeLimitMode 绘制矩形或半径边界，主要用于检查拖拽/平移限制。
        public bool drawPlaneLimit = true;

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("绘制距离边界")]
        // 绘制 minDistance/maxDistance 对应的球形边界，便于判断滚轮缩放限制。
        public bool drawDistanceLimit = true;

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("绘制最低高度")]
        // 绘制 minHeight 所在的水平参考线，便于检查相机是否会低于地面或业务底面。
        public bool drawMinHeightPlane = true;

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("绘制环绕中心")]
        // 显示当前环绕中心，调试绕点旋转和目标环绕时很直观。
        public bool drawOrbitPivot = true;

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("场景范围颜色")]
        public Color sceneRangeGizmoColor = new Color(0.1f, 0.7f, 1f, 0.8f);

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("距离边界颜色")]
        public Color distanceLimitGizmoColor = new Color(0.2f, 1f, 0.35f, 0.75f);

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("最低高度颜色")]
        public Color minHeightGizmoColor = new Color(1f, 0.35f, 0.1f, 0.75f);

        [TitleGroup("调试/场景范围绘制")]
        [ShowIf(nameof(drawSceneRangeGizmos))]
        [LabelText("环绕中心颜色")]
        public Color orbitPivotGizmoColor = new Color(1f, 0.75f, 0.1f, 0.9f);

        // 上一帧的模式缓存，用于检测 Inspector 或外部脚本是否切换了模式。
        private CameraWorkMode lastWorkMode;
        private CameraProjectionMode lastProjectionMode;
        // 当前摄像机定位动画，新的动画开始前会先 Kill 旧动画，避免多个 Tween 同时写 Transform。
        private Tween activeTween;

        // 自由相机旋转缓存：Yaw 控制水平转向，Pitch 控制俯仰角。
        private float freeYaw;
        private float freePitch;
        // 鼠标按下时如果指针在 UI 上，本次按住期间都会忽略，避免拖出 UI 后误触相机。
        private bool freeRotateBlockedByUI;
        // 自由相机右键绕点旋转时的中心点和距离。
        private Vector3 freePivot;
        private float freePivotDistance;

        // 透视自由拖拽的起始状态，按下鼠标时记录，拖动时用来计算相机位移。
        private bool freePanBlockedByUI;
        private Vector3 freePanStartMouse;
        private Vector3 freePanStartPosition;
        // 地面锚点拖拽：记录鼠标按下时射到地面的点，拖动时保持这个点跟随鼠标。
        private Vector3 freePanGroundAnchor;
        private bool freePanHasGroundAnchor;
        private float freePanSensitivity;

        // 正交拖拽和正交缩放的状态缓存。
        private bool orthoPanBlockedByUI;
        private Vector3 orthoPanStartMouse;
        private Vector3 orthoPanStartPosition;
        private float desiredOrthographicSize;

        // 目标环绕模式状态：角度、当前距离、目标距离、用户平移偏移。
        private float orbitYaw;
        private float orbitPitch;
        private float orbitCurrentDistance;
        private float orbitDesiredDistance;
        private Vector3 orbitPanOffset;
        // 目标环绕的输入锁定状态。
        private bool orbitRotateBlockedByUI;
        private bool orbitPanBlockedByUI;
        private Vector3 orbitPanLastMouse;
        // 空闲自动旋转用计时器；用户手动操作后会清零。
        private float orbitIdleTimer;
        private bool orbitStateInitialized;

        // 仅旋转漫游模式状态。
        private float rotateOnlyYaw;
        private float rotateOnlyPitch;
        private bool rotateOnlyBlockedByUI;
        private bool rotateOnlyPanBlockedByUI;
        private Vector3 rotateOnlyPanStartMouse;
        private Vector3 rotateOnlyPanStartPosition;

        // 统一时间来源。使用 unscaledDeltaTime 时，即使 Time.timeScale 为 0，镜头动画和输入平滑仍可运行。
        private float DeltaTime
        {
            get { return inputSettings != null && inputSettings.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; }
        }

        private void Awake()
        {
            CacheCamera();

            // 简单单例，方便业务代码快速访问；场景里有多个本组件时，以第一个 Awake 的为 Instance。
            if (Instance == null)
            {
                Instance = this;
            }

            // 初始化顺序不能随意调换：
            // 先修正配置，再同步投影，最后根据当前 Transform 建立角度/距离缓存。
            ValidateSettings();
            ApplyProjectionMode(true);
            CaptureFreeAngles();
            CaptureRotateOnlyAngles();
            InitializeModeState(true);
        }

        private void OnEnable()
        {
            CacheCamera();
            // 重新启用组件时清理鼠标按住状态，避免禁用前一次点击影响启用后的第一帧。
            ResetInputState();
            ValidateSettings();
            ApplyProjectionMode(true);
            InitializeModeState(true);
        }

        private void OnDisable()
        {
            ResetInputState();
            StopCameraTween();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            if (controlledCamera == null)
            {
                return;
            }

            // 运行时允许在 Inspector 或外部脚本中直接切换模式，下一帧自动同步内部状态。
            if (lastWorkMode != workMode)
            {
                InitializeModeState(true);
            }

            if (lastProjectionMode != projectionMode)
            {
                ApplyProjectionMode(true);
            }
            else if (syncProjectionAtRuntime)
            {
                ApplyProjectionMode(false);
            }

            if (!enableMouseInput)
            {
                return;
            }

            isTweening = activeTween != null && activeTween.IsActive() && activeTween.IsPlaying();
            if (isTweening && inputSettings.blockInputDuringTween)
            {
                // 相机定位动画期间如果继续读鼠标输入，会出现 Tween 和手动控制同时写 Transform 的抖动。
                return;
            }

            // 每种工作模式互相独立，避免自由相机和目标环绕同时处理同一个鼠标输入。
            switch (workMode)
            {
                case CameraWorkMode.FreeCamera:
                    HandleFreeCamera();
                    break;
                case CameraWorkMode.TargetOrbit:
                    HandleTargetOrbit();
                    break;
                case CameraWorkMode.RotateOnly:
                    HandleRotateOnly();
                    break;
            }
        }

        private void OnValidate()
        {
            CacheCamera();
            ValidateSettings();

            if (!Application.isPlaying && controlledCamera != null)
            {
                // 编辑器下改投影枚举时立即反馈到相机组件，方便直接预览正交/透视效果。
                ApplyProjectionMode(true);
            }
        }

        /// <summary>
        /// 自由相机：透视下处理旋转/绕点/滚轮/拖拽，正交下处理视口缩放/拖拽。
        /// </summary>
        private void HandleFreeCamera()
        {
            if (controlledCamera.orthographic)
            {
                HandleFreeOrthographicZoom();
                HandleFreeOrthographicPan();
                HandleFreeKeyboardMove();
                return;
            }

            HandleFreeRotationAndOrbit();
            HandleFreePerspectiveZoom();
            HandleFreePerspectivePan();
            HandleFreeKeyboardMove();
        }

        /// <summary>
        /// 透视自由旋转；如果启用绕点环绕，则右键旋转时同时维持到中心点的距离。
        /// </summary>
        private void HandleFreeRotationAndOrbit()
        {
            if (!freeSettings.enableRotation)
            {
                return;
            }

            MouseButton button = inputSettings.freeRotateButton;
            if (GetMouseButtonDown(button))
            {
                // UI 检测只在鼠标按下瞬间做一次，并把结果缓存到本次拖拽周期。
                freeRotateBlockedByUI = IsInputBlockedByUI();
                if (freeRotateBlockedByUI)
                {
                    return;
                }

                // 按下时重新捕获角度和绕点，避免外部脚本移动相机后缓存仍停留在旧状态。
                CaptureFreeAngles();
                freePivot = ResolveFreeOrbitPivot();
                currentOrbitPivot = freePivot;
                freePivotDistance = Mathf.Max(0.01f, Vector3.Distance(transform.position, freePivot));
            }

            if (GetMouseButton(button) && !freeRotateBlockedByUI)
            {
                // Legacy Input 的 Mouse X/Y 是帧内位移量，这里乘一个固定比例保持和旧模块接近的手感。
                float rotationScale = 0.02f;
                freeYaw += Input.GetAxis("Mouse X") * freeSettings.rotateHorizontalSpeed * rotationScale;
                freePitch -= Input.GetAxis("Mouse Y") * freeSettings.rotateVerticalSpeed * rotationScale;
                freePitch = CameraUtility.ClampPitch(freePitch, freeSettings.minPitch, freeSettings.maxPitch);

                Quaternion targetRotation = Quaternion.Euler(freePitch, freeYaw, 0f);
                ApplyRotation(targetRotation, freeSettings.smoothRotation, freeSettings.rotationSmoothSpeed);

                if (ShouldFreeOrbit())
                {
                    // 绕点旋转的核心：用“中心点 - 旋转后的 forward 反方向 * 距离”反推相机位置。
                    Vector3 targetPosition = freePivot - targetRotation * Vector3.forward * freePivotDistance;
                    targetPosition = ClampCameraPosition(targetPosition);
                    ApplyPosition(targetPosition, freeSettings.smoothRotation, freeSettings.rotationSmoothSpeed);
                }
            }

            if (GetMouseButtonUp(button))
            {
                freeRotateBlockedByUI = false;
            }
        }

        /// <summary>
        /// 透视滚轮缩放：按配置选择鼠标落点、画面中心或场景中心作为移动方向。
        /// </summary>
        private void HandleFreePerspectiveZoom()
        {
            if (!freeSettings.enablePerspectiveZoom)
            {
                return;
            }

            float scroll = ReadScroll();
            if (Mathf.Approximately(scroll, 0f) || IsInputBlockedByUI())
            {
                return;
            }

            float heightRatio = CameraUtility.GetHeightRatio(transform.position, rangeSettings);
            // 高度越高通常希望滚轮位移越大，因此通过曲线让缩放速度随高度变化。
            float curveValue = CameraUtility.EvaluateCurve(freeSettings.perspectiveZoomCurve, heightRatio, 1f);
            Vector3 direction = ResolvePerspectiveZoomDirection();
            Vector3 targetPosition = transform.position + direction * (scroll * freeSettings.perspectiveZoomStrength * curveValue);
            targetPosition = ClampCameraPosition(targetPosition);

            ApplyPosition(targetPosition, freeSettings.smoothPerspectiveZoom, freeSettings.perspectiveZoomSmoothSpeed);
        }

        /// <summary>
        /// 透视拖拽平移：优先使用地面锚点，使鼠标拖住的地面点尽量保持在光标下。
        /// </summary>
        private void HandleFreePerspectivePan()
        {
            if (!freeSettings.enablePerspectivePan)
            {
                return;
            }

            MouseButton button = inputSettings.freePanButton;
            if (GetMouseButtonDown(button))
            {
                freePanBlockedByUI = IsInputBlockedByUI();
                if (freePanBlockedByUI)
                {
                    return;
                }

                freePanStartMouse = Input.mousePosition;
                freePanStartPosition = transform.position;
                float heightRatio = CameraUtility.GetHeightRatio(transform.position, rangeSettings);
                // 拖拽速度同样受高度曲线影响，高空拖拽更快，低空拖拽更细。
                freePanSensitivity = freeSettings.perspectivePanStrength
                                     * CameraUtility.EvaluateCurve(freeSettings.perspectivePanCurve, heightRatio, 1f);
                freePanHasGroundAnchor = freeSettings.useGroundAnchorPan
                                         && CameraUtility.TryGetGroundPoint(
                                             controlledCamera,
                                             Input.mousePosition,
                                             rangeSettings.sceneCenter.y,
                                             out freePanGroundAnchor);
            }

            if (GetMouseButton(button) && !freePanBlockedByUI)
            {
                Vector3 targetPosition;

                if (freePanHasGroundAnchor
                    && CameraUtility.TryGetGroundPoint(controlledCamera, Input.mousePosition, rangeSettings.sceneCenter.y, out Vector3 currentGroundPoint))
                {
                    // 地面锚点拖拽按当前落点反推摄像机位移，场景比例变化时也能保持手感稳定。
                    targetPosition = transform.position + (freePanGroundAnchor - currentGroundPoint);
                }
                else
                {
                    Vector3 mouseDelta = -(Input.mousePosition - freePanStartMouse) * freePanSensitivity;
                    // 不允许抬升时，只取水平前向，避免拖拽导致相机高度变化。
                    Vector3 verticalDirection = freeSettings.panCanMoveUp
                        ? (transform.forward + transform.up).normalized
                        : CameraUtility.GetPlanarForward(transform);
                    targetPosition = freePanStartPosition + transform.right * mouseDelta.x + verticalDirection * mouseDelta.y;
                }

                targetPosition = ClampCameraPosition(targetPosition);
                ApplyPosition(targetPosition, freeSettings.smoothPerspectivePan, freeSettings.perspectivePanSmoothSpeed);
            }

            if (GetMouseButtonUp(button))
            {
                freePanBlockedByUI = false;
                freePanHasGroundAnchor = false;
            }
        }

        /// <summary>
        /// 正交滚轮缩放，只改变 orthographicSize，不移动摄像机位置。
        /// </summary>
        private void HandleFreeOrthographicZoom()
        {
            if (!freeSettings.enableOrthographicZoom)
            {
                return;
            }

            float scroll = ReadScroll();
            if (!Mathf.Approximately(scroll, 0f) && !IsInputBlockedByUI())
            {
                // 正交相机的“缩放”本质是改变 orthographicSize，数值越小画面越近。
                desiredOrthographicSize -= scroll * freeSettings.orthographicZoomStrength;
                desiredOrthographicSize = Mathf.Clamp(
                    desiredOrthographicSize,
                    freeSettings.minOrthographicSize,
                    freeSettings.maxOrthographicSize);
            }

            if (freeSettings.smoothOrthographicZoom)
            {
                float factor = CameraUtility.SmoothFactor(freeSettings.orthographicZoomSmoothSpeed, DeltaTime);
                controlledCamera.orthographicSize = Mathf.Lerp(controlledCamera.orthographicSize, desiredOrthographicSize, factor);
            }
            else
            {
                controlledCamera.orthographicSize = desiredOrthographicSize;
            }
        }

        /// <summary>
        /// 正交拖拽按屏幕像素换算世界距离，避免正交尺寸变化后拖拽比例失真。
        /// </summary>
        private void HandleFreeOrthographicPan()
        {
            if (!freeSettings.enableOrthographicPan)
            {
                return;
            }

            MouseButton button = inputSettings.freePanButton;
            if (GetMouseButtonDown(button))
            {
                orthoPanBlockedByUI = IsInputBlockedByUI();
                if (orthoPanBlockedByUI)
                {
                    return;
                }

                orthoPanStartMouse = Input.mousePosition;
                orthoPanStartPosition = transform.position;
            }

            if (GetMouseButton(button) && !orthoPanBlockedByUI)
            {
                Vector3 mouseDelta = Input.mousePosition - orthoPanStartMouse;
                float pixelHeight = Mathf.Max(1f, controlledCamera.pixelHeight);
                // 正交视口高度 = orthographicSize * 2，用它换算每个屏幕像素对应的世界距离。
                float worldPerPixel = controlledCamera.orthographicSize * 2f / pixelHeight;
                Vector3 move = -transform.right * (mouseDelta.x * worldPerPixel * freeSettings.orthographicPanStrength)
                               - transform.up * (mouseDelta.y * worldPerPixel * freeSettings.orthographicPanStrength);
                Vector3 targetPosition = ClampCameraPosition(orthoPanStartPosition + move);

                ApplyPosition(targetPosition, freeSettings.smoothOrthographicPan, freeSettings.orthographicPanSmoothSpeed);
            }

            if (GetMouseButtonUp(button))
            {
                orthoPanBlockedByUI = false;
            }
        }

        /// <summary>
        /// 自由相机 WASD 移动：按摄像机当前朝向的水平投影移动，避免镜头俯仰时 W/S 把相机带到空中或地下。
        /// </summary>
        private void HandleFreeKeyboardMove()
        {
            HandleKeyboardMove(
                freeSettings.enableKeyboardMove,
                freeSettings.keyboardMoveSpeed,
                freeSettings.smoothKeyboardMove,
                freeSettings.keyboardMoveSmoothSpeed);
        }

        /// <summary>
        /// 目标环绕模式：围绕目标或场景中心旋转，同时支持滚轮距离、平移目标偏移和空闲自动旋转。
        /// </summary>
        private void HandleTargetOrbit()
        {
            if (!orbitStateInitialized)
            {
                // 允许外部脚本直接切到 TargetOrbit 后不手动初始化，第一帧自动补齐状态。
                InitializeOrbitState(true);
            }

            bool hasManualInput = false;

            if (targetOrbitSettings.enableRotation)
            {
                MouseButton rotateButton = inputSettings.orbitRotateButton;
                if (GetMouseButtonDown(rotateButton))
                {
                    orbitRotateBlockedByUI = IsInputBlockedByUI();
                }

                if (GetMouseButton(rotateButton) && !orbitRotateBlockedByUI)
                {
                    // 目标环绕只改变 yaw/pitch 和距离，最终位置统一在 ApplyOrbitTransform 中计算。
                    float rotationScale = 0.02f;
                    orbitYaw += Input.GetAxis("Mouse X") * targetOrbitSettings.rotateHorizontalSpeed * rotationScale;
                    orbitPitch -= Input.GetAxis("Mouse Y") * targetOrbitSettings.rotateVerticalSpeed * rotationScale;
                    orbitPitch = CameraUtility.ClampPitch(orbitPitch, targetOrbitSettings.minPitch, targetOrbitSettings.maxPitch);
                    hasManualInput = true;
                }

                if (GetMouseButtonUp(rotateButton))
                {
                    orbitRotateBlockedByUI = false;
                }
            }

            if (targetOrbitSettings.enableZoom)
            {
                float scroll = ReadScroll();
                if (!Mathf.Approximately(scroll, 0f) && !IsInputBlockedByUI())
                {
                    // 距离越远时滚轮单步位移越大，避免大场景中缩放过慢。
                    orbitDesiredDistance -= scroll * targetOrbitSettings.zoomStrength * Mathf.Max(1f, orbitDesiredDistance);
                    orbitDesiredDistance = Mathf.Clamp(orbitDesiredDistance, targetOrbitSettings.minDistance, targetOrbitSettings.maxDistance);
                    hasManualInput = true;
                }
            }

            if (targetOrbitSettings.enablePan)
            {
                MouseButton panButton = inputSettings.orbitPanButton;
                if (GetMouseButtonDown(panButton))
                {
                    orbitPanBlockedByUI = IsInputBlockedByUI();
                    orbitPanLastMouse = Input.mousePosition;
                }

                if (GetMouseButton(panButton) && !orbitPanBlockedByUI)
                {
                    Vector3 mouseDelta = Input.mousePosition - orbitPanLastMouse;
                    // 平移的是环绕中心偏移，不是直接移动相机，这样旋转后仍然围绕新的中心。
                    float panScale = targetOrbitSettings.panStrength * Mathf.Max(1f, orbitCurrentDistance);
                    orbitPanOffset -= (transform.right * mouseDelta.x + transform.up * mouseDelta.y) * panScale;
                    orbitPanLastMouse = Input.mousePosition;
                    hasManualInput = true;
                }

                if (GetMouseButtonUp(panButton))
                {
                    orbitPanBlockedByUI = false;
                }
            }

            if (HandleTargetOrbitKeyboardMove())
            {
                hasManualInput = true;
            }

            if (hasManualInput)
            {
                // 用户操作优先级最高，任何手动输入都会重新计算空闲时间。
                orbitIdleTimer = 0f;
            }
            else
            {
                orbitIdleTimer += DeltaTime;
                if (targetOrbitSettings.enableIdleAutoRotate && orbitIdleTimer >= targetOrbitSettings.idleDelay)
                {
                    // 空闲自动旋转只改 yaw，不改变距离和俯仰角，避免视角越转越偏。
                    orbitYaw += targetOrbitSettings.idleRotateSpeed * DeltaTime;
                }
            }

            ApplyOrbitTransform();
        }

        /// <summary>
        /// 目标环绕 WASD 移动：移动的是环绕中心偏移量，不直接改相机 Transform，避免下一次 ApplyOrbitTransform 覆盖输入结果。
        /// </summary>
        private bool HandleTargetOrbitKeyboardMove()
        {
            if (!targetOrbitSettings.enableKeyboardMove || targetOrbitSettings.keyboardMoveSpeed <= 0f)
            {
                return false;
            }

            if (!TryGetKeyboardMoveDelta(targetOrbitSettings.keyboardMoveSpeed, out Vector3 moveDelta))
            {
                return false;
            }

            orbitPanOffset += moveDelta;
            return true;
        }

        /// <summary>
        /// 仅旋转漫游模式：右键原地转向，滚轮沿前向移动，中键沿本地坐标平移。
        /// </summary>
        private void HandleRotateOnly()
        {
            if (rotateOnlySettings.enableRotation)
            {
                MouseButton rotateButton = inputSettings.rotateOnlyButton;
                if (GetMouseButtonDown(rotateButton))
                {
                    rotateOnlyBlockedByUI = IsInputBlockedByUI();
                    CaptureRotateOnlyAngles();
                }

                if (GetMouseButton(rotateButton) && !rotateOnlyBlockedByUI)
                {
                    // 仅旋转模式不维护环绕中心，适合第一人称式观察或局部检查。
                    float rotationScale = 0.02f;
                    rotateOnlyYaw += Input.GetAxis("Mouse X") * rotateOnlySettings.rotateHorizontalSpeed * rotationScale;
                    rotateOnlyPitch -= Input.GetAxis("Mouse Y") * rotateOnlySettings.rotateVerticalSpeed * rotationScale;
                    rotateOnlyPitch = CameraUtility.ClampPitch(rotateOnlyPitch, rotateOnlySettings.minPitch, rotateOnlySettings.maxPitch);
                    Quaternion targetRotation = Quaternion.Euler(rotateOnlyPitch, rotateOnlyYaw, 0f);
                    ApplyRotation(targetRotation, rotateOnlySettings.smoothRotation, rotateOnlySettings.rotationSmoothSpeed);
                }

                if (GetMouseButtonUp(rotateButton))
                {
                    rotateOnlyBlockedByUI = false;
                }
            }

            if (rotateOnlySettings.enableScrollMove)
            {
                float scroll = ReadScroll();
                if (!Mathf.Approximately(scroll, 0f) && !IsInputBlockedByUI())
                {
                    // 只沿当前 forward 前后移动，不改变视野大小。
                    Vector3 targetPosition = transform.position + transform.forward * (scroll * rotateOnlySettings.scrollMoveSpeed);
                    ApplyPosition(ClampCameraPosition(targetPosition), false, 0f);
                }
            }

            if (rotateOnlySettings.enablePan)
            {
                MouseButton panButton = inputSettings.rotateOnlyPanButton;
                if (GetMouseButtonDown(panButton))
                {
                    rotateOnlyPanBlockedByUI = IsInputBlockedByUI();
                    rotateOnlyPanStartMouse = Input.mousePosition;
                    rotateOnlyPanStartPosition = transform.position;
                }

                if (GetMouseButton(panButton) && !rotateOnlyPanBlockedByUI)
                {
                    // 按本地 right/up 平移，适合在当前观察方向上做小范围构图调整。
                    Vector3 mouseDelta = -(Input.mousePosition - rotateOnlyPanStartMouse) * rotateOnlySettings.panSpeed;
                    Vector3 targetPosition = rotateOnlyPanStartPosition + transform.right * mouseDelta.x + transform.up * mouseDelta.y;
                    ApplyPosition(ClampCameraPosition(targetPosition), false, 0f);
                }

                if (GetMouseButtonUp(panButton))
                {
                    rotateOnlyPanBlockedByUI = false;
                }
            }

            HandleRotateOnlyKeyboardMove();
        }

        /// <summary>
        /// 仅旋转漫游 WASD 移动：复用统一键盘移动入口，使该模式能在保持当前视角的同时前后左右漫游。
        /// </summary>
        private void HandleRotateOnlyKeyboardMove()
        {
            HandleKeyboardMove(
                rotateOnlySettings.enableKeyboardMove,
                rotateOnlySettings.keyboardMoveSpeed,
                rotateOnlySettings.smoothKeyboardMove,
                rotateOnlySettings.keyboardMoveSmoothSpeed);
        }

        /// <summary>
        /// 统一处理 WASD 键盘移动。方向只取 XZ 平面，所有移动最终仍走 ClampCameraPosition，保证不会越过已有场景范围。
        /// </summary>
        private void HandleKeyboardMove(bool enabled, float moveSpeed, bool smooth, float smoothSpeed)
        {
            if (!enabled || moveSpeed <= 0f)
            {
                return;
            }

            if (!TryGetKeyboardMoveDelta(moveSpeed, out Vector3 moveDelta))
            {
                return;
            }

            Vector3 targetPosition = transform.position + moveDelta;
            targetPosition = ClampCameraPosition(targetPosition);
            ApplyPosition(targetPosition, smooth, smoothSpeed);
        }

        /// <summary>
        /// 计算本帧 WASD 应产生的位移量。只返回水平位移，具体是移动相机还是移动环绕中心由调用方决定。
        /// </summary>
        private bool TryGetKeyboardMoveDelta(float moveSpeed, out Vector3 moveDelta)
        {
            moveDelta = Vector3.zero;

            Vector2 moveInput = ReadWasdInput();
            if (moveInput.sqrMagnitude <= 0.0001f || IsInputBlockedByUI())
            {
                return false;
            }

            Vector3 forward = CameraUtility.GetPlanarForward(transform);
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up);
            if (right.sqrMagnitude <= 0.0001f)
            {
                // 极端俯视/仰视角下 right 的水平投影可能接近 0，用 forward 反推一个稳定的水平右方向。
                right = Vector3.Cross(Vector3.up, forward);
            }

            // 对角移动时限制长度，避免 W+D 比单方向移动更快。
            Vector3 moveDirection = forward * moveInput.y + right.normalized * moveInput.x;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

            moveDelta = moveDirection * (moveSpeed * DeltaTime);
            return true;
        }

        /// <summary>
        /// 根据当前配置计算自由相机右键旋转时的环绕中心。
        /// </summary>
        private Vector3 ResolveFreeOrbitPivot()
        {
            switch (freeSettings.orbitPivotMode)
            {
                case OrbitPivotMode.SceneCenter:
                    return rangeSettings.sceneCenter;
                case OrbitPivotMode.Target:
                    return targetObject != null ? targetObject.position : rangeSettings.sceneCenter;
                default:
                    if (CameraUtility.TryGetViewportGroundPoint(
                            controlledCamera,
                            new Vector2(0.5f, 0.5f),
                            rangeSettings.sceneCenter.y,
                            out Vector3 groundPoint))
                    {
                        Vector3 offset = groundPoint - rangeSettings.sceneCenter;
                        offset.y = 0f;
                        if (offset.magnitude <= freeSettings.maxPivotOffsetFromCenter)
                        {
                            return groundPoint;
                        }
                    }

                    return rangeSettings.sceneCenter;
            }
        }

        /// <summary>
        /// 低空且靠近中心时只允许原地旋转，避免环绕半径过小导致镜头翻滚感过强。
        /// </summary>
        private bool ShouldFreeOrbit()
        {
            if (!freeSettings.orbitWhileRotating)
            {
                return false;
            }

            if (transform.position.y >= freeSettings.lowHeightOrbitThreshold)
            {
                return true;
            }

            return freePivotDistance >= freeSettings.lowHeightMinOrbitRadius;
        }

        /// <summary>
        /// 根据透视缩放配置计算滚轮移动方向。
        /// </summary>
        private Vector3 ResolvePerspectiveZoomDirection()
        {
            Vector3 direction;
            switch (freeSettings.zoomFocusMode)
            {
                case ZoomFocusMode.SceneCenter:
                    direction = rangeSettings.sceneCenter - transform.position;
                    break;
                case ZoomFocusMode.CameraForward:
                    direction = transform.forward;
                    break;
                default:
                    if (CameraUtility.TryGetGroundPoint(
                            controlledCamera,
                            Input.mousePosition,
                            rangeSettings.sceneCenter.y,
                            out Vector3 mouseGroundPoint))
                    {
                        if (rangeSettings.zoomMouseOutOfRangeUseCenter
                            && !CameraUtility.IsPointInsideSceneRectangle(mouseGroundPoint, rangeSettings))
                        {
                            direction = rangeSettings.sceneCenter - transform.position;
                        }
                        else
                        {
                            direction = mouseGroundPoint - transform.position;
                        }
                    }
                    else
                    {
                        direction = transform.forward;
                    }
                    break;
            }

            return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        }

        /// <summary>
        /// 将目标环绕状态应用到 Transform。
        /// </summary>
        private void ApplyOrbitTransform()
        {
            Vector3 center = ResolveOrbitCenter();
            currentOrbitPivot = center;

            if (targetOrbitSettings.smoothOrbit)
            {
                // 距离单独平滑，避免滚轮一下子把镜头推得太近或太远。
                float factor = CameraUtility.SmoothFactor(targetOrbitSettings.orbitSmoothSpeed, DeltaTime);
                orbitCurrentDistance = Mathf.Lerp(orbitCurrentDistance, orbitDesiredDistance, factor);
            }
            else
            {
                orbitCurrentDistance = orbitDesiredDistance;
            }

            Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            // 环绕相机的位置始终由“中心点 + 角度 + 距离”推导，不累计位移误差。
            Vector3 position = center - rotation * Vector3.forward * orbitCurrentDistance;
            position = ClampCameraPosition(position);

            ApplyRotation(rotation, targetOrbitSettings.smoothOrbit, targetOrbitSettings.orbitSmoothSpeed);
            ApplyPosition(position, targetOrbitSettings.smoothOrbit, targetOrbitSettings.orbitSmoothSpeed);
        }

        /// <summary>
        /// 目标为空时使用场景中心，保证目标环绕模式也能直接用于全场景查看。
        /// </summary>
        private Vector3 ResolveOrbitCenter()
        {
            Vector3 baseCenter = targetObject != null ? targetObject.position : rangeSettings.sceneCenter;
            return baseCenter + targetOrbitSettings.targetOffset + orbitPanOffset;
        }

        /// <summary>
        /// 初始化目标环绕状态。切换到目标环绕模式或更换目标时调用。
        /// </summary>
        private void InitializeOrbitState(bool fitDistance)
        {
            Vector3 center = ResolveOrbitCenter();
            Vector3 toCenter = center - transform.position;

            if (toCenter.sqrMagnitude > 0.0001f)
            {
                // 根据当前相机朝向目标中心的方向反推 yaw/pitch，切模式时视角不会突然跳变。
                Quaternion lookRotation = Quaternion.LookRotation(toCenter.normalized, Vector3.up);
                orbitYaw = lookRotation.eulerAngles.y;
                orbitPitch = CameraUtility.ClampPitch(
                    lookRotation.eulerAngles.x,
                    targetOrbitSettings.minPitch,
                    targetOrbitSettings.maxPitch);
                orbitCurrentDistance = toCenter.magnitude;
            }
            else
            {
                orbitYaw = transform.eulerAngles.y;
                orbitPitch = CameraUtility.ClampPitch(
                    transform.eulerAngles.x,
                    targetOrbitSettings.minPitch,
                    targetOrbitSettings.maxPitch);
                orbitCurrentDistance = Mathf.Max(1f, targetOrbitSettings.minDistance);
            }

            if (fitDistance && targetOrbitSettings.fitDistanceFromTargetBounds && targetObject != null
                && CameraUtility.TryCollectBounds(targetObject, out Bounds bounds))
            {
                // 首次绑定目标或主动取景时，用包围盒估算能看全目标的初始距离。
                orbitCurrentDistance = CameraUtility.GetFrameDistance(
                    controlledCamera,
                    bounds,
                    targetOrbitSettings.boundsDistanceScale);
            }

            orbitCurrentDistance = Mathf.Clamp(
                orbitCurrentDistance,
                targetOrbitSettings.minDistance,
                targetOrbitSettings.maxDistance);
            orbitDesiredDistance = orbitCurrentDistance;
            orbitIdleTimer = 0f;
            orbitStateInitialized = true;
        }

        /// <summary>
        /// 模式切换时重置输入状态，并按新模式重新同步角度/距离缓存。
        /// </summary>
        private void InitializeModeState(bool force)
        {
            if (!force && lastWorkMode == workMode)
            {
                return;
            }

            ResetInputState();
            CaptureFreeAngles();
            CaptureRotateOnlyAngles();

            if (workMode == CameraWorkMode.TargetOrbit)
            {
                // 进入目标环绕时重新计算环绕中心和距离；离开时保留自由相机当前 Transform。
                InitializeOrbitState(true);
            }
            else
            {
                orbitStateInitialized = false;
            }

            lastWorkMode = workMode;
        }

        /// <summary>
        /// 将枚举投影模式同步到 Camera 组件。
        /// </summary>
        private void ApplyProjectionMode(bool force)
        {
            if (controlledCamera == null)
            {
                return;
            }

            bool shouldBeOrthographic = projectionMode == CameraProjectionMode.Orthographic;
            if (force || controlledCamera.orthographic != shouldBeOrthographic)
            {
                controlledCamera.orthographic = shouldBeOrthographic;
                // 正交缩放使用 desiredOrthographicSize 做目标值，切换投影时需要同步当前尺寸。
                desiredOrthographicSize = Mathf.Clamp(
                    controlledCamera.orthographicSize,
                    freeSettings.minOrthographicSize,
                    freeSettings.maxOrthographicSize);
            }

            lastProjectionMode = projectionMode;
        }

        /// <summary>
        /// 记录当前世界旋转为自由相机的 yaw/pitch 缓存。
        /// </summary>
        private void CaptureFreeAngles()
        {
            Vector3 euler = transform.eulerAngles;
            freeYaw = euler.y;
            freePitch = CameraUtility.ClampPitch(euler.x, freeSettings.minPitch, freeSettings.maxPitch);
        }

        /// <summary>
        /// 记录当前世界旋转为仅旋转模式的 yaw/pitch 缓存。
        /// </summary>
        private void CaptureRotateOnlyAngles()
        {
            Vector3 euler = transform.eulerAngles;
            rotateOnlyYaw = euler.y;
            rotateOnlyPitch = CameraUtility.ClampPitch(euler.x, rotateOnlySettings.minPitch, rotateOnlySettings.maxPitch);
        }

        /// <summary>
        /// 位置约束统一入口，后续增加碰撞避让或区域规则时只需要改这里。
        /// </summary>
        private Vector3 ClampCameraPosition(Vector3 position)
        {
            return CameraUtility.ClampPositionByRange(position, rangeSettings);
        }

        /// <summary>
        /// 应用位置变化。所有模式最终都走这里写入 Transform，便于统一处理平滑和范围约束。
        /// </summary>
        private void ApplyPosition(Vector3 targetPosition, bool smooth, float smoothSpeed)
        {
            if (!smooth)
            {
                transform.position = targetPosition;
                return;
            }

            float factor = CameraUtility.SmoothFactor(smoothSpeed, DeltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, factor);
            // 平滑插值后再做一次范围收口，避免插值路径越过边界。
            transform.position = ClampCameraPosition(transform.position);
        }

        /// <summary>
        /// 应用旋转变化。旋转不做边界限制，只根据配置决定立即应用或平滑插值。
        /// </summary>
        private void ApplyRotation(Quaternion targetRotation, bool smooth, float smoothSpeed)
        {
            if (!smooth)
            {
                transform.rotation = targetRotation;
                return;
            }

            float factor = CameraUtility.SmoothFactor(smoothSpeed, DeltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, factor);
        }

        /// <summary>
        /// 判断本次输入是否应被 UI 拦截。具体 UI 检测由工具类完成，这里只负责读取配置开关。
        /// </summary>
        private bool IsInputBlockedByUI()
        {
            return inputSettings != null
                   && inputSettings.blockWhenPointerOverUI
                   && CameraUtility.IsPointerOverUI();
        }

        // 以下三个方法把自定义鼠标枚举转换为 Unity Legacy Input 的按键编号。
        private static bool GetMouseButtonDown(MouseButton button)
        {
            return Input.GetMouseButtonDown((int)button);
        }

        private static bool GetMouseButton(MouseButton button)
        {
            return Input.GetMouseButton((int)button);
        }

        private static bool GetMouseButtonUp(MouseButton button)
        {
            return Input.GetMouseButtonUp((int)button);
        }

        private static float ReadScroll()
        {
            return Input.GetAxis("Mouse ScrollWheel");
        }

        /// <summary>
        /// 直接读取 WASD 键，避免依赖 Project Settings 中 Horizontal/Vertical 轴是否配置正确。
        /// </summary>
        private static Vector2 ReadWasdInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.A))
            {
                horizontal -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                horizontal += 1f;
            }

            if (Input.GetKey(KeyCode.S))
            {
                vertical -= 1f;
            }

            if (Input.GetKey(KeyCode.W))
            {
                vertical += 1f;
            }

            return new Vector2(horizontal, vertical);
        }

        /// <summary>
        /// 缓存 Camera 组件。放在 Awake/OnEnable/OnValidate 中调用，兼顾运行时和编辑器调参。
        /// </summary>
        private void CacheCamera()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }
        }

        /// <summary>
        /// 对外暴露配置前做基础修正，避免 Inspector 中填出无效范围。
        /// </summary>
        private void ValidateSettings()
        {
            if (inputSettings == null)
            {
                inputSettings = new CameraInputSettings();
            }

            if (rangeSettings == null)
            {
                rangeSettings = new CameraRangeSettings();
            }

            if (freeSettings == null)
            {
                freeSettings = new FreeCameraSettings();
            }

            if (targetOrbitSettings == null)
            {
                targetOrbitSettings = new TargetOrbitSettings();
            }

            if (rotateOnlySettings == null)
            {
                rotateOnlySettings = new RotateOnlySettings();
            }

            rangeSettings.minDistance = Mathf.Max(0.01f, rangeSettings.minDistance);
            rangeSettings.maxDistance = Mathf.Max(rangeSettings.minDistance, rangeSettings.maxDistance);
            rangeSettings.rectangleSize.x = Mathf.Max(0.01f, rangeSettings.rectangleSize.x);
            rangeSettings.rectangleSize.y = Mathf.Max(0.01f, rangeSettings.rectangleSize.y);
            rangeSettings.planeRadius = Mathf.Max(0.01f, rangeSettings.planeRadius);

            freeSettings.minOrthographicSize = Mathf.Max(0.01f, freeSettings.minOrthographicSize);
            freeSettings.maxOrthographicSize = Mathf.Max(freeSettings.minOrthographicSize, freeSettings.maxOrthographicSize);
            freeSettings.keyboardMoveSpeed = Mathf.Max(0f, freeSettings.keyboardMoveSpeed);
            freeSettings.keyboardMoveSmoothSpeed = Mathf.Max(0f, freeSettings.keyboardMoveSmoothSpeed);

            targetOrbitSettings.minDistance = Mathf.Max(0.01f, targetOrbitSettings.minDistance);
            targetOrbitSettings.maxDistance = Mathf.Max(targetOrbitSettings.minDistance, targetOrbitSettings.maxDistance);
            targetOrbitSettings.boundsDistanceScale = Mathf.Max(0.01f, targetOrbitSettings.boundsDistanceScale);
            targetOrbitSettings.keyboardMoveSpeed = Mathf.Max(0f, targetOrbitSettings.keyboardMoveSpeed);

            rotateOnlySettings.keyboardMoveSpeed = Mathf.Max(0f, rotateOnlySettings.keyboardMoveSpeed);
            rotateOnlySettings.keyboardMoveSmoothSpeed = Mathf.Max(0f, rotateOnlySettings.keyboardMoveSmoothSpeed);
        }

        private void ResetInputState()
        {
            // 所有“按下时是否点到 UI”的缓存都在这里清理，防止模式切换后残留阻塞状态。
            freeRotateBlockedByUI = false;
            freePanBlockedByUI = false;
            freePanHasGroundAnchor = false;
            orthoPanBlockedByUI = false;
            orbitRotateBlockedByUI = false;
            orbitPanBlockedByUI = false;
            rotateOnlyBlockedByUI = false;
            rotateOnlyPanBlockedByUI = false;
        }

        [TitleGroup("调试")]
        [Button("保存当前到调试相机状态")]
        public void SaveCurrentToDebugPose()
        {
            // 调试字段只作为 Inspector 临时预设，不会自动写配置文件。
            debugPose = GetCurrentPose();
        }

        [TitleGroup("调试")]
        [Button("立即应用调试相机状态")]
        public void ApplyDebugPoseImmediately()
        {
            ApplyPose(debugPose);
        }

        [TitleGroup("调试")]
        [Button("动画应用调试相机状态")]
        public void TweenToDebugPose()
        {
            MoveToPose(debugPose, 0.8f);
        }

        [TitleGroup("调试")]
        [Button("根据目标重新取景")]
        public void FitTargetNow()
        {
            if (workMode != CameraWorkMode.TargetOrbit)
            {
                // 手动取景默认切到目标环绕模式，因为取景距离依赖环绕中心。
                workMode = CameraWorkMode.TargetOrbit;
            }

            InitializeOrbitState(true);
            ApplyOrbitTransform();
        }

        /// <summary>
        /// 外部脚本切换工作模式时使用。
        /// </summary>
        public void SwitchWorkMode(CameraWorkMode newMode)
        {
            workMode = newMode;
            InitializeModeState(true);
        }

        /// <summary>
        /// 外部脚本切换投影模式时使用。
        /// </summary>
        public void SwitchProjectionMode(CameraProjectionMode newMode)
        {
            projectionMode = newMode;
            ApplyProjectionMode(true);
        }

        /// <summary>
        /// 外部脚本设置环绕目标。resetView 为 true 时会按目标包围盒重新计算距离。
        /// </summary>
        public void SetTarget(Transform newTarget, bool resetView = true)
        {
            targetObject = newTarget;
            orbitPanOffset = Vector3.zero;

            if (workMode == CameraWorkMode.TargetOrbit)
            {
                InitializeOrbitState(resetView);
            }
        }

        /// <summary>
        /// 外部脚本设置场景边界，常用于加载不同建筑或区域后同步控制范围。
        /// </summary>
        public void SetSceneRange(Vector3 center, Vector3 size)
        {
            rangeSettings.sceneCenter = center;
            rangeSettings.sceneSize = size;
            rangeSettings.rectangleSize = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.z));
            rangeSettings.planeRadius = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.z)) * 0.5f;
            ValidateSettings();
        }

        /// <summary>
        /// 获取当前摄像机状态，便于保存到配置或作为预设。
        /// </summary>
        public CameraPose GetCurrentPose()
        {
            return new CameraPose(controlledCamera);
        }

        /// <summary>
        /// 不带动画地应用相机状态。
        /// </summary>
        public void ApplyPose(CameraPose pose)
        {
            StopCameraTween();

            transform.position = pose.position;
            transform.eulerAngles = pose.eulerAngles;
            controlledCamera.orthographic = pose.isOrthographic;
            controlledCamera.orthographicSize = Mathf.Max(0.01f, pose.orthographicSize);
            controlledCamera.fieldOfView = Mathf.Clamp(pose.fieldOfView, 1f, 179f);
            // 位姿恢复会同步 projectionMode，保证 Inspector 枚举和 Camera 真实状态一致。
            projectionMode = pose.isOrthographic
                ? CameraProjectionMode.Orthographic
                : CameraProjectionMode.Perspective;
            desiredOrthographicSize = controlledCamera.orthographicSize;

            CaptureFreeAngles();
            CaptureRotateOnlyAngles();
            RefreshModeStateAfterPoseApplied();
        }

        /// <summary>
        /// 使用 DOTween 平滑定位到指定相机状态。
        /// </summary>
        public Tween MoveToPose(CameraPose pose, float duration)
        {
            StopCameraTween();

            duration = Mathf.Max(0f, duration);
            if (duration <= 0f)
            {
                ApplyPose(pose);
                return null;
            }

            controlledCamera.orthographic = pose.isOrthographic;
            projectionMode = pose.isOrthographic
                ? CameraProjectionMode.Orthographic
                : CameraProjectionMode.Perspective;

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this);
            // 位置、旋转、正交尺寸、透视 FOV 同步播放，动画结束后再刷新模式缓存。
            sequence.Join(transform.DOMove(pose.position, duration));
            sequence.Join(transform.DORotate(pose.eulerAngles, duration));
            sequence.Join(DOTween.To(
                () => controlledCamera.orthographicSize,
                value => controlledCamera.orthographicSize = Mathf.Max(0.01f, value),
                Mathf.Max(0.01f, pose.orthographicSize),
                duration));
            sequence.Join(DOTween.To(
                () => controlledCamera.fieldOfView,
                value => controlledCamera.fieldOfView = Mathf.Clamp(value, 1f, 179f),
                Mathf.Clamp(pose.fieldOfView, 1f, 179f),
                duration));
            sequence.OnComplete(() =>
            {
                desiredOrthographicSize = controlledCamera.orthographicSize;
                CaptureFreeAngles();
                CaptureRotateOnlyAngles();
                RefreshModeStateAfterPoseApplied();
                isTweening = false;
            });

            activeTween = sequence;
            isTweening = true;
            return activeTween;
        }

        /// <summary>
        /// 停止当前相机定位动画，不会回滚已经移动到的位置。
        /// </summary>
        public void StopCameraTween()
        {
            if (activeTween != null && activeTween.IsActive())
            {
                // complete=false 表示停止在当前位置，不强制跳到动画终点。
                activeTween.Kill(false);
            }

            activeTween = null;
            isTweening = false;
        }

        /// <summary>
        /// 应用保存位姿后只同步内部缓存，不按目标包围盒重新取景，避免覆盖刚恢复的相机位置。
        /// </summary>
        private void RefreshModeStateAfterPoseApplied()
        {
            ResetInputState();

            if (workMode == CameraWorkMode.TargetOrbit)
            {
                InitializeOrbitState(false);
            }
            else
            {
                orbitStateInitialized = false;
            }

            lastWorkMode = workMode;
            lastProjectionMode = projectionMode;
        }

        private void OnDrawGizmos()
        {
            if (drawRangeOnlyWhenSelected)
            {
                return;
            }

            DrawSceneRangeGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawRangeOnlyWhenSelected)
            {
                return;
            }

            DrawSceneRangeGizmos();
        }

        /// <summary>
        /// 绘制场景范围调试线框。
        /// 这里用 Gizmos 而不是运行时对象，保证只服务编辑器调试，不会污染场景层级。
        /// </summary>
        private void DrawSceneRangeGizmos()
        {
            if (!drawSceneRangeGizmos || rangeSettings == null)
            {
                return;
            }

            Gizmos.color = sceneRangeGizmoColor;
            // 中心点是所有边界、缩放距离、目标为空时环绕点的共同基准，先画出来便于定位。
            Gizmos.DrawWireSphere(rangeSettings.sceneCenter, 1.2f);

            if (drawSceneSizeBox)
            {
                DrawSceneSizeBox();
            }

            if (drawPlaneLimit)
            {
                DrawPlaneLimit();
            }

            if (drawDistanceLimit)
            {
                DrawDistanceLimit();
            }

            if (drawMinHeightPlane)
            {
                DrawMinHeightPlane();
            }

            if (drawOrbitPivot)
            {
                Gizmos.color = orbitPivotGizmoColor;
                // 当前环绕中心运行时会持续更新，编辑器未运行时显示默认值也方便检查初始配置。
                Gizmos.DrawWireSphere(currentOrbitPivot, 0.8f);
            }
        }

        /// <summary>
        /// 绘制 sceneSize 对应的三维场景尺寸框。
        /// </summary>
        private void DrawSceneSizeBox()
        {
            Vector3 size = new Vector3(
                Mathf.Abs(rangeSettings.sceneSize.x),
                Mathf.Abs(rangeSettings.sceneSize.y),
                Mathf.Abs(rangeSettings.sceneSize.z));

            if (size.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Gizmos.color = sceneRangeGizmoColor;
            Gizmos.DrawWireCube(rangeSettings.sceneCenter, size);
        }

        /// <summary>
        /// 绘制 XZ 平面限制区域，矩形和半径两种模式分开展示。
        /// </summary>
        private void DrawPlaneLimit()
        {
            Gizmos.color = sceneRangeGizmoColor;

            switch (rangeSettings.planeLimitMode)
            {
                case PlaneLimitMode.Rectangle:
                    DrawPlaneRectangle(rangeSettings.sceneCenter, GetPlaneRectangleSize());
                    break;
                case PlaneLimitMode.Radius:
                    DrawPlaneCircle(rangeSettings.sceneCenter, Mathf.Max(0.01f, rangeSettings.planeRadius));
                    break;
            }
        }

        /// <summary>
        /// 绘制距离限制。球形线框表达真实约束，水平圆帮助在顶视图中判断范围。
        /// </summary>
        private void DrawDistanceLimit()
        {
            if (!rangeSettings.limitDistanceAndHeight)
            {
                return;
            }

            float minDistance = Mathf.Max(0.01f, rangeSettings.minDistance);
            float maxDistance = Mathf.Max(minDistance, rangeSettings.maxDistance);

            Gizmos.color = distanceLimitGizmoColor;
            Gizmos.DrawWireSphere(rangeSettings.sceneCenter, minDistance);
            Gizmos.DrawWireSphere(rangeSettings.sceneCenter, maxDistance);
            DrawPlaneCircle(rangeSettings.sceneCenter, minDistance);
            DrawPlaneCircle(rangeSettings.sceneCenter, maxDistance);
        }

        /// <summary>
        /// 绘制最低高度参考线。该高度是相机 y 坐标下限，不是地形或建筑实际高度。
        /// </summary>
        private void DrawMinHeightPlane()
        {
            if (!rangeSettings.limitDistanceAndHeight)
            {
                return;
            }

            Vector3 center = new Vector3(
                rangeSettings.sceneCenter.x,
                rangeSettings.minHeight,
                rangeSettings.sceneCenter.z);

            Gizmos.color = minHeightGizmoColor;

            switch (rangeSettings.planeLimitMode)
            {
                case PlaneLimitMode.Radius:
                    DrawPlaneCircle(center, Mathf.Max(0.01f, rangeSettings.planeRadius));
                    break;
                default:
                    DrawPlaneRectangle(center, GetPlaneRectangleSize());
                    break;
            }
        }

        /// <summary>
        /// 获取平面矩形尺寸。矩形范围没有配置时，用 sceneSize 的 X/Z 做兜底。
        /// </summary>
        private Vector2 GetPlaneRectangleSize()
        {
            Vector2 size = rangeSettings.rectangleSize;
            if (size.x <= 0f || size.y <= 0f)
            {
                size = new Vector2(Mathf.Abs(rangeSettings.sceneSize.x), Mathf.Abs(rangeSettings.sceneSize.z));
            }

            size.x = Mathf.Max(0.01f, size.x);
            size.y = Mathf.Max(0.01f, size.y);
            return size;
        }

        /// <summary>
        /// 绘制水平矩形边界，Vector2.x 对应世界 X，Vector2.y 对应世界 Z。
        /// </summary>
        private static void DrawPlaneRectangle(Vector3 center, Vector2 size)
        {
            float halfX = Mathf.Max(0.01f, size.x) * 0.5f;
            float halfZ = Mathf.Max(0.01f, size.y) * 0.5f;

            Vector3 leftUp = center + new Vector3(-halfX, 0f, halfZ);
            Vector3 rightUp = center + new Vector3(halfX, 0f, halfZ);
            Vector3 rightDown = center + new Vector3(halfX, 0f, -halfZ);
            Vector3 leftDown = center + new Vector3(-halfX, 0f, -halfZ);

            Gizmos.DrawLine(leftUp, rightUp);
            Gizmos.DrawLine(rightUp, rightDown);
            Gizmos.DrawLine(rightDown, leftDown);
            Gizmos.DrawLine(leftDown, leftUp);
        }

        /// <summary>
        /// Gizmos 没有内置的水平圆绘制方法，这里用线段近似画出半径边界。
        /// </summary>
        private static void DrawPlaneCircle(Vector3 center, float radius)
        {
            radius = Mathf.Max(0.01f, radius);
            const int segmentCount = 64;
            Vector3 previous = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segmentCount; i++)
            {
                float angle = Mathf.PI * 2f * i / segmentCount;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
