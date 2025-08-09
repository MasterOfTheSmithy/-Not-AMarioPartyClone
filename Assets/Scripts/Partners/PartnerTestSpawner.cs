using UnityEngine;

public class PartnerTestSpawner : MonoBehaviour
{
    public PartnerData testData;

    void Start()
    {
        GameObject obj = new GameObject("TestPartner");
        PartnerInstance inst = obj.AddComponent<PartnerInstance>();

        // You might want to call Initialize here later once PlayerMover is ready
        // inst.Initialize(testData, playerMoverReference, offset);
    }
}