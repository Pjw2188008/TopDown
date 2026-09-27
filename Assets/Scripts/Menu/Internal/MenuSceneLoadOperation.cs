using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메뉴에서 씬을 전환하는 동안만 로딩 우선순위를 높입니다.
/// 메뉴가 언로드되어도 완료를 추적하고 이전 우선순위를 복원하는 임시 런타임 오브젝트입니다.
/// 씬에 직접 붙이지 않습니다.
/// </summary>
public sealed class MenuSceneLoadOperation : MonoBehaviour
{
    private static MenuSceneLoadOperation active;
    private AsyncOperation operation;
    private ThreadPriority previousPriority;
    private bool ownsPriority, finished;
    private double startedAt;
    private string targetScene;
    public float Progress => operation != null ? operation.progress : 0f;
    public static bool IsBusy => active != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        if (active != null) active.RestorePriority();
        active = null;
    }

    public static MenuSceneLoadOperation Begin(string scenePath, bool prioritizeLoading)
    {
        if (IsBusy) throw new InvalidOperationException("A menu scene transition is already running.");
        if (!Application.CanStreamedLevelBeLoaded(scenePath))
            throw new ArgumentException("Scene is not enabled in the build list: " + scenePath);

        var root = new GameObject("Menu Scene Loading (Runtime)");
        DontDestroyOnLoad(root);
        var loader = root.AddComponent<MenuSceneLoadOperation>();
        active = loader;
        loader.targetScene = scenePath;
        loader.previousPriority = Application.backgroundLoadingPriority;
        loader.startedAt = Time.realtimeSinceStartupAsDouble;
        try
        {
            if (prioritizeLoading)
            {
                loader.ownsPriority = true;
                Application.backgroundLoadingPriority = ThreadPriority.High;
            }
            loader.operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (loader.operation == null) throw new InvalidOperationException("Scene load did not start.");
            loader.operation.completed += loader.OnCompleted;
            Debug.Log($"[SceneLoad] 시작: {scenePath}, 우선순위={Application.backgroundLoadingPriority}");
            return loader;
        }
        catch
        {
            loader.RestorePriority();
            Destroy(root);
            throw;
        }
    }

    private void OnCompleted(AsyncOperation completed)
    {
        if (finished) return;
        finished = true;
        operation.completed -= OnCompleted;
        double seconds = Time.realtimeSinceStartupAsDouble - startedAt;
        RestorePriority();
        Debug.Log($"[SceneLoad] 완료: {targetScene}, {seconds:F3}초 (씬 활성화 포함)");
        Destroy(gameObject);
    }

    private void RestorePriority()
    {
        if (ownsPriority)
        {
            Application.backgroundLoadingPriority = previousPriority;
            ownsPriority = false;
        }
        if (active == this) active = null;
    }

    private void OnApplicationQuit() => RestorePriority();
    private void OnDestroy()
    {
        if (operation != null) operation.completed -= OnCompleted;
        RestorePriority();
    }
}
