using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class ExitDoorway : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Rigidbody2D>().isKinematic = true;

        var box = GetComponent<BoxCollider2D>();
        box.size = Vector2.one * 0.2f;
        box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // On player collision, reload level
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
