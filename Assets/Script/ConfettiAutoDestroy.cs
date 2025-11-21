using UnityEngine;
using UnityEngine.UI;

public class ConfettiAutoDestroy : MonoBehaviour
{

    private float lifetime = 5f;

    public void SetLifetime(float time)
    {
        lifetime = time;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Confetti"))
        {
            Destroy(other.gameObject);
        }
    }
}
