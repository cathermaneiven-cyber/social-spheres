using UnityEngine;
using GTAG.CosmeticSDK;

public class SelfCosmeticGiver : MonoBehaviour
{
    [SerializeField] private PhotonCosmeticSync localSync;

    public void GiveCosmetic(string cosmeticId)
    {
        if (localSync == null) return;
        localSync.NetworkEquip(cosmeticId);
    }

    public void GiveCosmeticWithColor(string cosmeticId, Color color)
    {
        if (localSync == null) return;
        localSync.NetworkEquip(cosmeticId, color);
    }

    public void RemoveCosmetic(EquipSlot slot)
    {
        if (localSync == null) return;
        localSync.NetworkUnequip(slot);
    }
}