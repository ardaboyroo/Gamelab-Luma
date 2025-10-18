using System;
using UnityEngine;

public class _SEnvVars31GZ12 : MonoBehaviour
{
    [SerializeField] private string titleId;
    [SerializeField] private string secretKey;

    private void Awake()
    {
        Environment.SetEnvironmentVariable("PLAYFAB_TITLE_ID", titleId);
        Environment.SetEnvironmentVariable("PLAYFAB_SECRET_KEY", secretKey);
    }
}