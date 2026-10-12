using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>튜토리얼 퍼즐 사물의 위치/크기/오류 출처를 보관하는 런타임 데이터. 부착하지 않습니다.</summary>
internal sealed class TutorialWorldCheckpoint
{
    private sealed class ObjectState
    {
        public GameObject Target;
        public Vector3 Position, Scale;
        public Quaternion Rotation;
        public bool Active;
        public int Depth;
        public Action Giant, Acceleration, Reflection;
        public SpriteRenderer Renderer;
        public Color Color;
    }

    private readonly List<ObjectState> objects = new List<ObjectState>();

    public TutorialWorldCheckpoint(Scene scene, PlayerMove player, HashSet<GameObject> targets)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component is PasteTarget || component is MovableInteractable || component is IErrorSource)
                    targets.Add(component.gameObject);

        var all = new HashSet<GameObject>();
        foreach (var target in targets)
        {
            if (target == null || target.scene != scene || target.GetComponentInParent<PlayerMove>() != null) continue;
            // 대상 자식의 오류/렌더러도 함께 복원하며 런타임에 새로 추가된 오류는 제거합니다.
            foreach (var child in target.GetComponentsInChildren<Transform>(true))
                if (child.GetComponentInParent<PlayerMove>() == null) all.Add(child.gameObject);
        }
        foreach (var target in all)
        {
            var transform = target.transform;
            var giant = target.GetComponent<GiantErrorEffect>();
            var acceleration = target.GetComponent<AccelerationErrorEffect>();
            var reflection = target.GetComponent<ReflectionErrorEffect>();
            var renderer = target.GetComponent<SpriteRenderer>();
            int depth = 0;
            for (var parent = transform.parent; parent != null; parent = parent.parent) depth++;
            objects.Add(new ObjectState
            {
                Target = target, Position = transform.position, Rotation = transform.rotation,
                Scale = transform.localScale, Active = target.activeSelf, Depth = depth,
                Giant = giant != null ? giant.CaptureTutorialState() : null,
                Acceleration = acceleration != null ? acceleration.CaptureTutorialState() : null,
                Reflection = reflection != null ? reflection.CaptureTutorialState() : null,
                Renderer = renderer, Color = renderer != null ? SpriteTint.GetColor(renderer) : Color.white
            });
        }
        objects.Sort((a, b) => a.Depth.CompareTo(b.Depth));
    }

    public void Restore()
    {
        foreach (var saved in objects)
        {
            if (saved.Target == null)
            {
                Debug.LogWarning("튜토리얼 체크포인트의 사물이 파괴되어 복구할 수 없습니다. 퍼즐 사물은 Destroy 대신 비활성화하세요.");
                continue;
            }
            var target = saved.Target;
            // 컴포넌트는 남겨 재사용하되, 이번 시도에서 Paste로 추가된 효과를 비활성화합니다.
            if (saved.Giant != null) saved.Giant();
            else target.GetComponent<GiantErrorEffect>()?.ResetScale();
            if (saved.Acceleration != null) saved.Acceleration();
            else target.GetComponent<AccelerationErrorEffect>()?.ResetAcceleration();
            if (saved.Reflection != null) saved.Reflection();
            else target.GetComponent<ReflectionErrorEffect>()?.ResetReflection();
            target.transform.SetPositionAndRotation(saved.Position, saved.Rotation);
            target.transform.localScale = saved.Scale;
            if (target.TryGetComponent<Rigidbody2D>(out var body))
            {
                body.position = saved.Position;
                body.rotation = saved.Rotation.eulerAngles.z;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            if (saved.Renderer != null) SpriteTint.SetColor(saved.Renderer, saved.Color);
            target.SetActive(saved.Active);
        }
        Physics2D.SyncTransforms();
    }
}
