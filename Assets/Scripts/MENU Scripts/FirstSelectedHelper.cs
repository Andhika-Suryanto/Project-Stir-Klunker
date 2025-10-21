using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Universal script to select a UI element when a page becomes active
/// Attach this to any UI page GameObject
/// </summary>
public class FirstSelectedHelper : MonoBehaviour
{
    [Header("First Selected Element")]
    [Tooltip("The button/selectable to select when this page activates")]
    public Selectable firstSelected;
    
    [Header("Settings")]
    [Tooltip("Delay before selecting (helps with transitions)")]
    public float selectionDelay = 0.1f;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private EventSystem eventSystem;
    
    void Awake()
    {
        eventSystem = EventSystem.current;
        
        if (eventSystem == null)
        {
            Debug.LogError("No EventSystem found! FirstSelectedHelper needs an EventSystem.");
        }
    }
    
    void OnEnable()
    {
        // When this page is enabled, select the first element
        StartCoroutine(SelectFirstElement());
    }
    
    IEnumerator SelectFirstElement()
    {
        // Small delay to ensure everything is initialized
        if (selectionDelay > 0)
        {
            yield return new WaitForSeconds(selectionDelay);
        }
        else
        {
            yield return null; // Wait at least one frame
        }
        
        SelectFirst();
    }
    
    public void SelectFirst()
    {
        if (firstSelected == null)
        {
            if (enableDebugLogs) Debug.LogWarning($"[{gameObject.name}] No first selected element assigned!");
            return;
        }
        
        if (!firstSelected.interactable)
        {
            if (enableDebugLogs) Debug.LogWarning($"[{gameObject.name}] First selected element '{firstSelected.name}' is not interactable!");
            return;
        }
        
        // Clear current selection first
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
        
        // Select the element
        firstSelected.Select();
        
        if (enableDebugLogs) 
            Debug.Log($"[{gameObject.name}] Selected first element: {firstSelected.name}");
    }
    
    /// <summary>
    /// Call this method to manually trigger first selection
    /// Useful if you need to reselect after some action
    /// </summary>
    public void ReselectFirst()
    {
        SelectFirst();
    }
    
    /// <summary>
    /// Set a new first selected element at runtime
    /// </summary>
    public void SetFirstSelected(Selectable newFirstSelected)
    {
        firstSelected = newFirstSelected;
        if (enableDebugLogs) 
            Debug.Log($"[{gameObject.name}] First selected changed to: {firstSelected?.name}");
    }
}