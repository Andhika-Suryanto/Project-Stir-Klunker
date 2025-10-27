using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IndividualTMPTextChanger : MonoBehaviour
{
    [Header("Button Text Pairs")]
    [Tooltip("Each button can change specific TMP texts")]
    public ButtonTextPair[] buttonTextPairs;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    void Start()
    {
        foreach (var pair in buttonTextPairs)
        {
            if (pair.button != null)
            {
                ButtonTextPair currentPair = pair;
                pair.button.onClick.AddListener(() => OnButtonClicked(currentPair));
            }
        }
    }
    
    void OnButtonClicked(ButtonTextPair pair)
    {
        if (enableDebugLogs)
            Debug.Log($"Button clicked: {pair.button.name}");
        
        foreach (var textMapping in pair.textMappings)
        {
            if (textMapping.textComponent != null)
            {
                textMapping.textComponent.text = textMapping.newText;
                
                if (enableDebugLogs)
                    Debug.Log($"Changed {textMapping.textComponent.name} to: {textMapping.newText}");
            }
        }
    }
    
    void OnDestroy()
    {
        foreach (var pair in buttonTextPairs)
        {
            if (pair.button != null)
            {
                pair.button.onClick.RemoveAllListeners();
            }
        }
    }
}

[System.Serializable]
public class ButtonTextPair
{
    [Tooltip("The button to monitor")]
    public Button button;
    
    [Tooltip("What text changes when this button is clicked")]
    public TextMapping[] textMappings;
}

[System.Serializable]
public class TextMapping
{
    [Tooltip("The TMP text component to change")]
    public TextMeshProUGUI textComponent;
    
    [Tooltip("The new text to display")]
    public string newText;
}