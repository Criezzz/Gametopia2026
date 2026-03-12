using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Manages available resolutions. Provides list for UI dropdown and applies user's choice.
public class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager Instance { get; private set; }

    private Resolution[] _availableResolutions;

    public Resolution[] AvailableResolutions => _availableResolutions;
    public int CurrentIndex { get; private set; }
    public bool IsFullscreen => Screen.fullScreen;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        CacheResolutions();
    }

    private void CacheResolutions()
    {
        // Filter duplicates (same w×h) and sort ascending
        var unique = new Dictionary<string, Resolution>();
        foreach (var res in Screen.resolutions)
        {
            string key = $"{res.width}x{res.height}";
            if (!unique.ContainsKey(key))
                unique[key] = res;
        }

        _availableResolutions = unique.Values
            .OrderBy(r => r.width)
            .ThenBy(r => r.height)
            .ToArray();

        // Find the current resolution index
        CurrentIndex = 0;
        for (int i = 0; i < _availableResolutions.Length; i++)
        {
            if (_availableResolutions[i].width == Screen.width &&
                _availableResolutions[i].height == Screen.height)
            {
                CurrentIndex = i;
                break;
            }
        }
    }

    public void ApplyResolution(int index, bool fullscreen)
    {
        if (index < 0 || index >= _availableResolutions.Length) return;

        var res = _availableResolutions[index];
        CurrentIndex = index;

        FullScreenMode mode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(res.width, res.height, mode);

        // Persist
        var data = SaveManager.Data;
        data.resolutionWidth = res.width;
        data.resolutionHeight = res.height;
        data.fullscreenMode = (int)mode;
        SaveManager.Save();
    }

    public string[] GetResolutionLabels()
    {
        return _availableResolutions
            .Select(r => $"{r.width} x {r.height}")
            .ToArray();
    }
}
