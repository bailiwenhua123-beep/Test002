using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShangHaiSYS.CameraControl
{
    /// <summary>
    /// 摄像机控制中复用的数学、边界、UI 检测工具。
    /// 这里不保存任何业务状态，主控制器只负责调用这些确定性函数。
    /// </summary>
    public static class CameraUtility
    {
        // UI 射线检测会在每帧或每次按下时触发，复用列表可以避免频繁产生 GC。
        private static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>(16);

        /// <summary>
        /// 将 0-360 的欧拉角转换到 -180 到 180，方便做俯仰角限制。
        /// </summary>
        public static float NormalizeAngle180(float angle)
        {
            // Unity 的 eulerAngles 通常返回 0-360，这里转成 -180-180 后更符合“俯仰角上下限”的直觉。
            while (angle > 180f)
            {
                angle -= 360f;
            }

            while (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }

        /// <summary>
        /// 俯仰角限制，内部会自动处理最小值大于最大值的异常配置。
        /// </summary>
        public static float ClampPitch(float pitch, float minPitch, float maxPitch)
        {
            if (minPitch > maxPitch)
            {
                // 防御性处理：如果 Inspector 里把最小/最大填反，不让控制逻辑直接失效。
                float temp = minPitch;
                minPitch = maxPitch;
                maxPitch = temp;
            }

            return Mathf.Clamp(NormalizeAngle180(pitch), minPitch, maxPitch);
        }

        /// <summary>
        /// 指数平滑系数，比直接 speed * deltaTime 更稳定，不容易因为帧率波动产生过冲。
        /// </summary>
        public static float SmoothFactor(float speed, float deltaTime)
        {
            if (speed <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Exp(-speed * Mathf.Max(0f, deltaTime));
        }

        /// <summary>
        /// 读取曲线值；曲线为空时返回兜底值，避免未配置曲线导致输入完全失效。
        /// </summary>
        public static float EvaluateCurve(AnimationCurve curve, float time, float fallback)
        {
            if (curve == null || curve.length == 0)
            {
                return fallback;
            }

            return curve.Evaluate(Mathf.Clamp01(time));
        }

        /// <summary>
        /// 鼠标是否压在 UI 上。先走 EventSystem 的快速判断，再做一次 GraphicRaycaster 结果兜底。
        /// </summary>
        public static bool IsPointerOverUI()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                // 场景里没有 EventSystem 时，说明没有可检测的 UGUI 事件系统，直接认为不在 UI 上。
                return false;
            }

            if (eventSystem.IsPointerOverGameObject())
            {
                // Unity 内置的快速判断，能覆盖大多数 StandaloneInputModule 场景。
                return true;
            }

            PointerEventData eventData = new PointerEventData(eventSystem)
            {
                position = Input.mousePosition
            };

            UiRaycastResults.Clear();
            // 兜底做一次完整 UI Raycast，避免某些自定义 Canvas 或输入模块下快速判断漏判。
            eventSystem.RaycastAll(eventData, UiRaycastResults);
            return UiRaycastResults.Count > 0;
        }

        /// <summary>
        /// 从屏幕点向指定高度平面发射射线，常用于获取鼠标在地面的落点。
        /// </summary>
        public static bool TryGetGroundPoint(Camera camera, Vector2 screenPosition, float groundHeight, out Vector3 point)
        {
            point = Vector3.zero;
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            // 使用数学平面而不是 Physics.Raycast，地面没有碰撞体时也能得到落点。
            Plane plane = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
            if (!plane.Raycast(ray, out float enter))
            {
                // 射线与平面平行或朝反方向时没有可用交点。
                return false;
            }

            point = ray.GetPoint(enter);
            return true;
        }

        /// <summary>
        /// 从视口点向指定高度平面发射射线，适合取画面中心落地位置。
        /// </summary>
        public static bool TryGetViewportGroundPoint(Camera camera, Vector2 viewportPosition, float groundHeight, out Vector3 point)
        {
            point = Vector3.zero;
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ViewportPointToRay(viewportPosition);
            // viewportPosition 使用 0-1 坐标，例如 (0.5,0.5) 表示画面中心。
            Plane plane = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
            if (!plane.Raycast(ray, out float enter))
            {
                return false;
            }

            point = ray.GetPoint(enter);
            return true;
        }

        /// <summary>
        /// 获取摄像机水平前向；当摄像机几乎垂直朝上/朝下时，用 up 的投影兜底。
        /// </summary>
        public static Vector3 GetPlanarForward(Transform transform)
        {
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.0001f)
            {
                return forward.normalized;
            }

            // 当镜头正对天空或地面时，forward 的水平投影几乎为零，此时用 up 的水平投影兜底。
            Vector3 upProjected = Vector3.ProjectOnPlane(transform.up, Vector3.up);
            return upProjected.sqrMagnitude > 0.0001f ? upProjected.normalized : Vector3.forward;
        }

        /// <summary>
        /// 判断一个世界点是否落在配置的场景矩形内，只比较 XZ 平面。
        /// </summary>
        public static bool IsPointInsideSceneRectangle(Vector3 point, CameraRangeSettings range)
        {
            Vector2 size = range.rectangleSize;
            if (size.x <= 0f || size.y <= 0f)
            {
                // 矩形范围未配置时，用场景尺寸的 X/Z 作为默认边界。
                size = new Vector2(Mathf.Abs(range.sceneSize.x), Mathf.Abs(range.sceneSize.z));
            }

            float halfX = Mathf.Max(0.01f, size.x * 0.5f);
            float halfZ = Mathf.Max(0.01f, size.y * 0.5f);
            return point.x >= range.sceneCenter.x - halfX
                   && point.x <= range.sceneCenter.x + halfX
                   && point.z >= range.sceneCenter.z - halfZ
                   && point.z <= range.sceneCenter.z + halfZ;
        }

        /// <summary>
        /// 按场景范围约束摄像机位置：先限制平面范围，再限制距离和高度。
        /// </summary>
        public static Vector3 ClampPositionByRange(Vector3 position, CameraRangeSettings range)
        {
            // 先限制平面范围，再限制球形距离/高度；这样相机不会因为距离钳制被推到平面范围外。
            position = ClampPositionByPlane(position, range);

            if (!range.limitDistanceAndHeight)
            {
                return position;
            }

            float minDistance = Mathf.Max(0.01f, range.minDistance);
            float maxDistance = Mathf.Max(minDistance, range.maxDistance);
            Vector3 fromCenter = position - range.sceneCenter;

            if (fromCenter.sqrMagnitude < 0.0001f)
            {
                // 相机刚好在中心点时无法归一化方向，给一个向上的默认方向。
                fromCenter = Vector3.up * minDistance;
            }

            float distance = fromCenter.magnitude;
            if (distance > maxDistance)
            {
                position = range.sceneCenter + fromCenter.normalized * maxDistance;
            }
            else if (distance < minDistance)
            {
                position = range.sceneCenter + fromCenter.normalized * minDistance;
            }

            if (position.y < range.minHeight)
            {
                // 最低高度是世界坐标高度，不依赖距离方向，避免相机穿到地面以下。
                position.y = range.minHeight;
            }

            return position;
        }

        /// <summary>
        /// 只限制 XZ 平面范围，不改动高度；用于平移、拖拽后的统一收口。
        /// </summary>
        public static Vector3 ClampPositionByPlane(Vector3 position, CameraRangeSettings range)
        {
            switch (range.planeLimitMode)
            {
                case PlaneLimitMode.Rectangle:
                {
                    Vector2 size = range.rectangleSize;
                    if (size.x <= 0f || size.y <= 0f)
                    {
                        // 如果矩形尺寸没有显式填写，就使用 sceneSize 自动推导一个可用矩形。
                        size = new Vector2(Mathf.Abs(range.sceneSize.x), Mathf.Abs(range.sceneSize.z));
                    }

                    float halfX = Mathf.Max(0.01f, size.x * 0.5f);
                    float halfZ = Mathf.Max(0.01f, size.y * 0.5f);
                    position.x = Mathf.Clamp(position.x, range.sceneCenter.x - halfX, range.sceneCenter.x + halfX);
                    position.z = Mathf.Clamp(position.z, range.sceneCenter.z - halfZ, range.sceneCenter.z + halfZ);
                    break;
                }
                case PlaneLimitMode.Radius:
                {
                    Vector2 offset = new Vector2(position.x - range.sceneCenter.x, position.z - range.sceneCenter.z);
                    float radius = Mathf.Max(0.01f, range.planeRadius);
                    if (offset.sqrMagnitude > radius * radius)
                    {
                        // 半径限制只影响 XZ，不改变高度，避免缩放或拖拽时产生额外上下跳动。
                        offset = offset.normalized * radius;
                        position.x = range.sceneCenter.x + offset.x;
                        position.z = range.sceneCenter.z + offset.y;
                    }
                    break;
                }
            }

            return position;
        }

        /// <summary>
        /// 根据配置的最大距离计算高度比例，用于滚轮和拖拽速度曲线。
        /// </summary>
        public static float GetHeightRatio(Vector3 position, CameraRangeSettings range)
        {
            float maxDistance = Mathf.Max(0.01f, range.maxDistance);
            // 结果钳制到 0-1，便于直接作为 AnimationCurve 的横轴输入。
            return Mathf.Clamp01((position.y - range.sceneCenter.y) / maxDistance);
        }

        /// <summary>
        /// 汇总目标下 Renderer 和 Collider 的世界包围盒。
        /// </summary>
        public static bool TryCollectBounds(Transform root, out Bounds bounds)
        {
            bounds = new Bounds(root != null ? root.position : Vector3.zero, Vector3.zero);
            if (root == null)
            {
                return false;
            }

            bool hasBounds = false;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                // Renderer 更接近视觉尺寸，优先纳入取景范围。
                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                // 某些业务对象没有 Renderer 但有 Collider，也需要能被正确取景。
                if (!hasBounds)
                {
                    bounds = colliders[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
            }

            return hasBounds;
        }

        /// <summary>
        /// 根据包围盒和摄像机 FOV 估算能完整看到目标的距离。
        /// </summary>
        public static float GetFrameDistance(Camera camera, Bounds bounds, float distanceScale)
        {
            if (camera == null)
            {
                return bounds.extents.magnitude * Mathf.Max(0.01f, distanceScale);
            }

            float radius = Mathf.Max(0.01f, bounds.extents.magnitude);
            float halfFov = Mathf.Max(1f, camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            // 用包围盒外接球半径估算距离，结果偏保守，但能降低模型边角被裁掉的概率。
            return radius / Mathf.Sin(halfFov) * Mathf.Max(0.01f, distanceScale);
        }
    }
}
