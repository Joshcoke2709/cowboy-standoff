using System;
using System.Collections;
using UnityEngine;

public class AmmoMagazine : MonoBehaviour
{
    public bool isPlayerOne = true;
    public int maxAmmo = 6;
    public float reloadSeconds = 1.5f;

    public int Current { get; private set; }
    public bool IsReloading { get; private set; }
    public float ReloadProgress { get; private set; }   // 0 to 1 while reloading

    public event Action Changed;

    private Coroutine reloadRoutine;

    private void Awake()
    {
        Current = maxAmmo;
    }

    public bool TryConsume()
    {
        if (IsReloading || Current <= 0) return false;

        Current--;
        Changed?.Invoke();

        if (Current <= 0)
            reloadRoutine = StartCoroutine(Reload());

        return true;
    }

    private IEnumerator Reload()
    {
        IsReloading = true;
        ReloadProgress = 0f;
        Changed?.Invoke();

        float elapsed = 0f;
        while (elapsed < reloadSeconds)
        {
            elapsed += Time.deltaTime;
            ReloadProgress = Mathf.Clamp01(elapsed / reloadSeconds);
            yield return null;
        }

        Current = maxAmmo;
        IsReloading = false;
        ReloadProgress = 0f;
        reloadRoutine = null;
        Changed?.Invoke();
    }

    public void Refill()
    {
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }
        Current = maxAmmo;
        IsReloading = false;
        ReloadProgress = 0f;
        Changed?.Invoke();
    }
}