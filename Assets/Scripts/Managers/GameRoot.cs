using UnityEngine;

/// <summary>Lightweight global service locator for small projects.</summary>
public sealed class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }
    public IRng Rng { get; private set; }

    [SerializeField] private int rngSeed = 12345;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Ensure()
    {
        if (Instance != null) return;
        var go = new GameObject("GameRoot");
        Instance = go.AddComponent<GameRoot>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Rng = new RngService(rngSeed);
    }
}

public interface IRng
{
    int Range(int minInclusive, int maxExclusive);
}

public sealed class RngService : IRng
{
    private System.Random _r;
    public RngService(int seed) => _r = new System.Random(seed);
    public int Range(int minInclusive, int maxExclusive) => _r.Next(minInclusive, maxExclusive);
}
