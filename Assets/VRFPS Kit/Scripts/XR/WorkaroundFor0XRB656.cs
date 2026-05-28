using UnityEngine;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;

[UnityEditor.XR.OpenXR.Features.OpenXRFeature(
TargetOpenXRApiVersion = "1.1.53",
UiName = "Workaround for issue OXRB-656",
BuildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
DocumentationLink = "https://issuetracker.unity3d.com/issues/xr-interaction-toolkit-xr-controllers-are-inverted-by-y-axis-when-using-meta-quest-3-with-openxr-plugin")
]
#endif

public class WorkaroundForOXRB656 : OpenXRFeature
{
    protected override bool OnInstanceCreate(ulong xrInstance)
    {
        return true;
    }
}