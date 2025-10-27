using UnityEngine;
using UnityEngine.UI;

public class ImageChangerBasedOnText : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("The Text component to monitor")]
    public Text textToMonitor;
    
    [Tooltip("The Image component that will change")]
    public Image targetImage;
    
    [Header("Text to Image Mappings")]
    [Tooltip("Map specific text values to specific sprites")]
    public TextImageMapping[] textImageMappings;
    
    [Header("Settings")]
    [Tooltip("Check text every frame (if false, only checks when manually called)")]
    public bool autoUpdate = true;
    
    [Tooltip("Case sensitive text matching")]
    public bool caseSensitive = false;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private string lastCheckedText = "";
    
    void Start()
    {
        if (textToMonitor == null)
        {
            Debug.LogError("Text To Monitor is not assigned!");
            enabled = false;
            return;
        }
        
        if (targetImage == null)
        {
            Debug.LogError("Target Image is not assigned!");
            enabled = false;
            return;
        }
        
        // Check immediately on start
        CheckAndUpdateImage();
    }
    
    void Update()
    {
        if (autoUpdate)
        {
            // Only update if text changed (optimization)
            if (textToMonitor.text != lastCheckedText)
            {
                CheckAndUpdateImage();
            }
        }
    }
    
    void CheckAndUpdateImage()
    {
        string currentText = textToMonitor.text;
        lastCheckedText = currentText;
        
        // Compare text and find matching sprite
        foreach (var mapping in textImageMappings)
        {
            bool isMatch = caseSensitive 
                ? (currentText == mapping.textValue) 
                : (currentText.ToLower() == mapping.textValue.ToLower());
            
            if (isMatch)
            {
                if (mapping.sprite != null)
                {
                    targetImage.sprite = mapping.sprite;
                    
                    if (enableDebugLogs)
                        Debug.Log($"Text '{currentText}' matched! Changed image to: {mapping.sprite.name}");
                    
                    return;
                }
                else
                {
                    Debug.LogWarning($"Mapping for text '{currentText}' has no sprite assigned!");
                    return;
                }
            }
        }
        
        // No match found
        if (enableDebugLogs)
            Debug.Log($"No image mapping found for text: '{currentText}'");
    }
    
    // Public method to manually trigger update
    public void ForceUpdate()
    {
        CheckAndUpdateImage();
    }
}

[System.Serializable]
public class TextImageMapping
{
    [Tooltip("The text value to match (e.g., '3', 'GO', 'READY')")]
    public string textValue;
    
    [Tooltip("The sprite to show when text matches")]
    public Sprite sprite;
}