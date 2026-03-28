using UnityEngine;

public class PawnService : MonoBehaviour
{
    void OnEnable()
    {
        ServicesLocator.Register(this);
    }

    void OnDisable()
    {
        ServicesLocator.Unregister(this);    
    }
}
