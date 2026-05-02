using UnityEngine;
using GTAG.CosmeticSDK;
using Photon.Pun;

public class ToggleCosmeticButton : MonoBehaviour
{
    public string cosmeticId = "TopHat";
    private float cooldown;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < cooldown) return;
        cooldown = Time.time + 0.5f;

        PhotonCosmeticSync localSync = null;

        foreach (PhotonCosmeticSync sync in FindObjectsOfType<PhotonCosmeticSync>())
        {
            PhotonView view = sync.GetComponent<PhotonView>();

            if (view != null && view.IsMine)
            {
                localSync = sync;
                break;
            }
        }

        if (localSync == null)
        {
            Debug.LogError("NO LOCAL PLAYER FOUND");
            return;
        }

        localSync.isLocalPlayer = true;

        CosmeticDefinition def = CosmeticRegistry.Instance.Get(cosmeticId);

        if (def == null)
        {
            Debug.LogError("Cosmetic not found: " + cosmeticId);
            return;
        }

        AvatarCosmeticController controller = localSync.GetComponent<AvatarCosmeticController>();

        if (controller == null)
        {
            Debug.LogError("No AvatarCosmeticController on local player");
            return;
        }

        CosmeticLoadout loadout = controller.GetCurrentLoadout();

        if (loadout.HasItem(def.slot) && loadout.GetItemId(def.slot) == cosmeticId)
        {
            localSync.NetworkUnequip(def.slot);
            Debug.Log("UNEQUIPPED " + cosmeticId);
        }
        else
        {
            localSync.NetworkEquip(cosmeticId);
            Debug.Log("EQUIPPED " + cosmeticId);
        }
    }
}