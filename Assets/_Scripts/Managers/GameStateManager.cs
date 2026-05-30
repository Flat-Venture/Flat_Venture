using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public Enums.GamePlayState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

        CurrentState = Enums.GamePlayState.Playing;
    }

    public bool CanPlayerControl()
    {
        return CurrentState == Enums.GamePlayState.Playing;
    }

    public void SetState(Enums.GamePlayState state)
    {
        CurrentState = state;
    }
}
