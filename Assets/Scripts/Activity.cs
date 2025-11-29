using UnityEngine;

public abstract class Activity : MonoBehaviour
{
    public bool IsActive { get; private set; }

    public void StartActivity()
    {
        if (IsActive) return;
        IsActive = true;
        OnStart();
    }

    public void StopActivity()
    {
        if (!IsActive) return;
        IsActive = false;
        OnStop();
    }

    public void ResetActivity()
    {
        StopActivity();
        OnReset();
    }

    protected abstract void OnStart();
    protected abstract void OnStop();
    protected abstract void OnReset();
}