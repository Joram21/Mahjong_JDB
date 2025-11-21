using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class SpwanerEditMode : MonoBehaviour
{
    public Sprite[] sprites;
    public GameObject template;
    [ContextMenu("Spawn")]
    public void Spawn()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            var go = Instantiate(template, transform);
            go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = i < 9 ? "0" + (i + 1) : (i + 1).ToString();
            go.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().sprite = sprites[i];
            go.name = i.ToString();
        }
    }
}
