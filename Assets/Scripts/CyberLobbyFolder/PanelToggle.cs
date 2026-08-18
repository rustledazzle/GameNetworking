using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private CanvasGroup leftPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    private bool isPanelVisible = false;

    private void Start()
    {
        // Start with panel hidden
        if (leftPanel != null)
        {
            leftPanel.alpha = 0;
            leftPanel.interactable = false;
            leftPanel.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;

        if (leftPanel != null)
        {
            leftPanel.alpha = isPanelVisible ? 1 : 0;
            leftPanel.interactable = isPanelVisible;
            leftPanel.blocksRaycasts = isPanelVisible;
        }
    }

    public void ShowPanel()
    {
        isPanelVisible = true;
        if (leftPanel != null)
        {
            leftPanel.alpha = 1;
            leftPanel.interactable = true;
            leftPanel.blocksRaycasts = true;
        }
    }

    public void HidePanel()
    {
        isPanelVisible = false;
        if (leftPanel != null)
        {
            leftPanel.alpha = 0;
            leftPanel.interactable = false;
            leftPanel.blocksRaycasts = false;
        }
    }
}