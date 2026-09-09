using UnityEngine;

public class BuyArmor : MonoBehaviour
{
    [SerializeField] private GameObject GlassArmour;
    [SerializeField] private GameObject Armour;
    
    public void BuyArmour()
    {
        if (GameManager.Money < 5) return;
        GameManager.Money -= 5;
        Armour.SetActive(true);
        GlassArmour.SetActive(true);
    }
}