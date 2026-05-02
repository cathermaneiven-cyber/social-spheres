using UnityEngine;
using GTAG.CosmeticSDK;

public class AutoSetLocalCosmeticSync : MonoBehaviour
{
    private void Start()
    {
        PhotonCosmeticSync sync = GetComponent<PhotonCosmeticSync>();

        if (sync == null) return;

        if (FindObjectsOfType<PhotonCosmeticSync>().Length == 1)
        {
            sync.isLocalPlayer = true;
            Debug.Log("Set as LOCAL player (only player in scene)");
        }
    }
}