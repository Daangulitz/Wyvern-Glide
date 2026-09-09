using UnityEngine;

public class BuyFuel : MonoBehaviour
{
    public void BuyGas()
    {
        if (GameManager.Money < 10) return;
        GameManager.Money -= 10;
        Fuel.AddMaxFuel(100);
        Debug.Log("bought Fuel");
    }
}
