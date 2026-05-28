using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    public static class XRFilterDebugger
    {
        public static void DebugSelectFilters(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            XRBaseInteractor baseInteractor = interactor as XRBaseInteractor;
            if (baseInteractor == null)
            {
                Debug.LogError("Interactor is not XRBaseInteractor");
                return;
            }

            XRInteractionManager manager = baseInteractor.interactionManager;
            if (manager == null)
            {
                Debug.LogError("Interactor has no InteractionManager");
                return;
            }

            Debug.Log("====== XR SELECT FILTER DEBUG ======");

            if (baseInteractor.selectFilters != null && baseInteractor.selectFilters.count > 0)
            {
                Debug.Log($"Interactor '{baseInteractor.transform.gameObject.name}' Select Filters:");
                for (int i = 0; i < baseInteractor.selectFilters.count; i++)
                {
                    IXRSelectFilter filter = baseInteractor.selectFilters.GetAt(i);
                    DebugSelectFilter(filter, interactor, interactable);
                }
            }
            else Debug.Log("  <No Filters>");

            if (interactable is XRBaseInteractable baseInteractable)
            {
                Debug.Log($"Interactable '{baseInteractable.transform.gameObject.name}' Select Filters:");
                if (baseInteractable.selectFilters != null && baseInteractable.selectFilters.count > 0)
                {
                    for (int i = 0; i < baseInteractable.selectFilters.count; i++)
                    {
                        IXRSelectFilter filter = baseInteractable.selectFilters.GetAt(i);
                        DebugSelectFilter(filter, interactor, interactable);
                    }
                }
                else Debug.Log("  <No Filters>");
            }
            else
            {
                Debug.Log("Interactable Select Filters: <not XRBaseInteractable>");
            }

            bool finalResult = manager.CanSelect(interactor, interactable);
            Debug.Log($"FINAL CanSelect RESULT → {finalResult}");
            Debug.Log("===================================");
        }

        public static void DebugSelectFilter(IXRSelectFilter filter, IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            string filterName = filter.GetType().Name;
            //If filter is a delegate, get the method name for better clarity, otherwise name will be "XRSelectFilterDelegate"
            if (filter is XRSelectFilterDelegate filterDelegate)
                filterName = $"XRSelectFilterDelegate: {filterDelegate.delegateToProcess.Method.Name}()";
                    
            if (!filter.canProcess)
                Debug.Log($"  - {filterName} → SKIPPED (canProcess = false)");
            else
                Debug.Log($"  - {filterName} → {filter.Process(interactor, interactable)}");
        }
    }
}
