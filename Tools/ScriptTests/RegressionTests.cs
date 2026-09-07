// Workspace-only logic tests. Minimal Unity substitutes avoid requiring an open scene.
using System;
using System.Reflection;
namespace UnityEngine
{
    public class MonoBehaviour
    {
        public string name = "Test";
        public Transform transform = new Transform();
        public T GetComponent<T>() where T : class => null;
        public T[] GetComponents<T>() => new T[0];
    }
    public class Transform { public Vector3 localScale; }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float sqrMagnitude => x*x+y*y;
        public static Vector2 right => new Vector2(1,0);
        public static Vector2 left => new Vector2(-1,0);
        public static Vector2 up => new Vector2(0,1);
        public static Vector2 down => new Vector2(0,-1);
    }
    public struct Vector3
    {
        public static Vector3 operator *(Vector3 value, float scale) => value;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => b;
    }
    public struct Color
    {
        public Color(float r, float g, float b, float a) { }
        public static Color white => new Color();
    }
    public class SpriteRenderer { public Color color; }
    public class AnimationCurve
    {
        public static AnimationCurve EaseInOut(float a, float b, float c, float d) => new AnimationCurve();
        public float Evaluate(float value) => value;
    }
    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public static float Abs(float value) => Math.Abs(value);
        public static float Atan2(float a, float b) => (float)Math.Atan2(a,b);
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Clamp01(float value) => Math.Max(0, Math.Min(1, value));
    }
    public static class Time { public static float deltaTime; }
    public static class Debug { public static void LogWarning(string message, object context) { } }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public class SerializeField : Attribute { }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class DisallowMultipleComponent : Attribute { }
}
public static class ProvenanceTests
{
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    public static void Main()
    {
        ModelTests.Run();
        foreach (Type type in new[] { typeof(GiantErrorEffect), typeof(AccelerationErrorEffect), typeof(ReflectionErrorEffect) })
        {
            object sourceA = Activator.CreateInstance(type);
            object targetB = Activator.CreateInstance(type);
            object sourceC = Activator.CreateInstance(type);
            object[] args = type == typeof(ReflectionErrorEffect) ? new object[0] : new object[] { 3f };
            MethodInfo trigger = type.GetMethod("Trigger");
            MethodInfo paste = type.GetMethod("ApplyPaste");
            MethodInfo reset = type.GetMethod(type == typeof(GiantErrorEffect) ? "ResetScale"
                : type == typeof(AccelerationErrorEffect) ? "ResetAcceleration" : "ResetReflection");
            Func<object, bool> canCut = value => (bool)type.GetProperty("CanCut").GetValue(value);
            Assert(!canCut(sourceA), "Inactive source must not be cut");
            trigger.Invoke(sourceA, args);
            Assert(canCut(sourceA), "Original source must be available");
            reset.Invoke(sourceA, null);
            Assert(!canCut(sourceA), "Cut source must become inactive");
            paste.Invoke(targetB, args);
            Assert(!canCut(targetB), "Pasted target must not be cut");
            Assert((bool)type.GetProperty("IsActive").GetValue(targetB), "Pasted effect must stay active");
            trigger.Invoke(sourceC, args);
            Assert(canCut(sourceC), "Different source of same type must remain available");
            Assert(!canCut(targetB), "Different source must not unlock pasted target");
            reset.Invoke(targetB, null);
            paste.Invoke(targetB, args);
            Assert(!canCut(targetB), "Repeated Paste must remain blocked");
            Console.WriteLine(type.Name + ": 8 assertions PASS");
        }
    }
}

public static class ModelTests
{
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception(message);
    }

    public static void Run()
    {
        var inventory = new ErrorInventory();
        Check(inventory.Capacity == 2 && inventory.Count == 0, "empty");
        Check(!inventory.TryStore(StoredErrorType.None, 1), "none rejected");
        Check(!inventory.TryStore((StoredErrorType)99, 1), "invalid rejected");
        Check(inventory.TryStore(StoredErrorType.Reflection, 0), "reflection");
        Check(inventory.TryStore(StoredErrorType.Giant, 3), "giant");
        Check(inventory.GetTypeAt(0) == StoredErrorType.Giant && inventory.GetTypeAt(1) == StoredErrorType.Reflection, "stable order");
        Check(!inventory.TryStore(StoredErrorType.Acceleration, 5), "full");
        Check(!inventory.TryReplace(StoredErrorType.Giant, StoredErrorType.Reflection, 4), "duplicate replacement");
        Check(inventory.Count == 2 && inventory.GetMultiplier(StoredErrorType.Giant) == 3, "failed replace preserves data");
        Check(inventory.TryReplace(StoredErrorType.Giant, StoredErrorType.Acceleration, 5), "replace");
        Check(inventory.GetMultiplier(StoredErrorType.Acceleration) == 5 && !inventory.Contains(StoredErrorType.Giant), "replacement multiplier");
        Check(inventory.Remove(StoredErrorType.Acceleration) && inventory.TryStore(StoredErrorType.Acceleration, 7), "reacquire");
        Check(inventory.GetTypeAt(-1) == StoredErrorType.None && inventory.GetTypeAt(2) == StoredErrorType.None, "index bounds");

        // Random transitions compared with an independent dictionary model.
        var random = new Random(203);
        var expected = new System.Collections.Generic.Dictionary<StoredErrorType,float>();
        inventory = new ErrorInventory();
        for (int i=0; i<3000; i++)
        {
            var type = (StoredErrorType)random.Next(1,4);
            var incoming = (StoredErrorType)random.Next(1,4);
            int action = random.Next(3);
            bool wanted, actual;
            if (action == 0)
            {
                wanted = expected.Count < 2 && !expected.ContainsKey(type);
                actual = inventory.TryStore(type, i);
                if (wanted) expected.Add(type, i);
            }
            else if (action == 1)
            {
                wanted = expected.Remove(type);
                actual = inventory.Remove(type);
            }
            else
            {
                wanted = expected.ContainsKey(type) && !expected.ContainsKey(incoming);
                actual = inventory.TryReplace(type, incoming, i);
                if (wanted) { expected.Remove(type); expected.Add(incoming, i); }
            }
            Check(actual == wanted && inventory.Count == expected.Count, "random operation");
            int slot = 0;
            for(int kind=1; kind<=3; kind++)
            {
                var error = (StoredErrorType)kind;
                Check(inventory.Contains(error) == expected.ContainsKey(error), "membership");
                if (expected.ContainsKey(error))
                {
                    Check(inventory.GetMultiplier(error) == expected[error], "multiplier");
                    Check(inventory.GetTypeAt(slot++) == error, "sorted index");
                }
            }
        }

        bool[,] compatibility = { {true,true,false,false,true}, {true,true,true,false,true}, {false,true,false,true,true} };
        for(int kind=1;kind<=3;kind++)
            for(int target=0;target<5;target++)
                Check(ErrorRules.CanPasteTo((StoredErrorType)kind,(PasteTargetType)target) == compatibility[kind-1,target], "compatibility");
        Check(!ErrorRules.CanPasteTo(StoredErrorType.None,PasteTargetType.Object), "no error");
        Check(!ErrorRules.CanPasteTo(StoredErrorType.Giant,(PasteTargetType)99), "unknown target");
        Check(ErrorInventory.StepSelection(0,2,-1)==1 && ErrorInventory.StepSelection(1,2,-1)==0, "scroll down wrap");
        Check(ErrorInventory.StepSelection(0,2,1)==1 && ErrorInventory.StepSelection(0,0,-1)==0, "scroll up/empty");

        float[,] directions = { {0,1,0,1}, {0,-1,0,-1}, {1,0,1,0}, {-1,0,-1,0}, {1,1,1,0}, {-1,1,-1,0}, {1,-1,1,0}, {-1,-1,-1,0}, {0,0,1,0} };
        for(int i=0;i<directions.GetLength(0);i++)
        {
            var result = AttackMath.CardinalDirection(new UnityEngine.Vector2(directions[i,0],directions[i,1]),22.5f);
            Check(result.x==directions[i,2] && result.y==directions[i,3],"attack direction");
        }
        foreach(int frames in new[]{1,2,3,7,12})
            foreach(float fps in new[]{12f,24f,60f})
                Check(Math.Abs(AttackMath.ImpactTime(frames/fps,fps) - Math.Max(0f,(frames-3f)/frames)) < 0.00001f,"third last frame");
        Check(AttackMath.ImpactTime(0,24)==1 && AttackMath.ImpactTime(1,0)==1,"empty clip");
        Console.WriteLine("Model checks: " + assertions + " PASS");
    }
}
