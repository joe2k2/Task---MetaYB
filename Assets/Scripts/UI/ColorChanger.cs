using UnityEngine;
using UnityEngine.UI;

public class ColorChanger : MonoBehaviour
{
    public Renderer targetRenderer;
    public Button buttonPrefab; 
    public Transform buttonParent;  
    public Color[] colors;

    private void Start()
    {
        targetRenderer.material = new Material(targetRenderer.material);

        SpawnButtons();
    }

    void SpawnButtons()
    {
        for (int i = 0; i < colors.Length; i++)
        {
            int index = i;

            Button newButton = Instantiate(buttonPrefab, buttonParent);

            Image img = newButton.GetComponent<Image>();
            img.color = colors[index];

            newButton.onClick.AddListener(() =>
            {
                targetRenderer.material.color = colors[index];
            });
        }
    }
}
