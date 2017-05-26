using System.Linq;

using UnityEngine;
using UnityEditor;

using UnityEditor.SceneManagement;

using NewtonVR.Network;
using System.Collections.Generic;

namespace NewtonVR.NetworkPhoton
{
    public class NVRExamplePhotonEditor : MonoBehaviour
    {
        [MenuItem("Tools/NewtonVR/Select all gameobjects with missing components in scene")]
        private static void SelectMissingComponentsInScene()
        {
            Transform[] ts = FindObjectsOfType<Transform>();
            List<GameObject> selection = new List<GameObject>();
            foreach (Transform t in ts)
            {
                Component[] cs = t.gameObject.GetComponents<Component>();
                foreach (Component c in cs)
                {
                    if (c == null)
                    {
                        selection.Add(t.gameObject);
                    }
                }
            }
            Selection.objects = selection.ToArray();

            Debug.Log("Selected gameobjects with missing components: " + selection.Count + " in the scene.");
        }

        [MenuItem("Tools/NewtonVR/Select objects that inherit from NVRInteractableItem in scene")]
        private static void SelectInheritedInteractableItems()
        {
            List<GameObject> nonitems = new List<GameObject>();
            NVRInteractableItem[] interactables = GameObject.FindObjectsOfType<NVRInteractableItem>();

            for (int itemIndex = 0; itemIndex < interactables.Length; itemIndex++)
            {
                NVRInteractableItem item = interactables[itemIndex];

                if (item.GetType() == typeof(NVRInteractableItem) || item is NVRNetworkObject)
                {
                    continue; //only apply to actual interactableitems.
                }

                nonitems.Add(item.gameObject);
            }

            Selection.objects = nonitems.ToArray();

            Debug.Log("Completed adding photon components to " + nonitems.Count + " NVRInteractables in the scene.");
        }

        [MenuItem("Tools/NewtonVR/Replace NVRInteractableItems with NVRPhotonInteractableItems in scene")]
        private static void ReplaceItemsWithPhotonItemsInScene()
        {
            NVRInteractableItem[] interactables = GameObject.FindObjectsOfType<NVRInteractableItem>();

            for (int itemIndex = 0; itemIndex < interactables.Length; itemIndex++)
            {
                NVRInteractableItem item = interactables[itemIndex];

                if (item.GetType() != typeof(NVRInteractableItem))
                {
                    continue; //only apply to actual interactableitems.
                }

                NVRPhotonInteractableItem photonItem = item.gameObject.GetComponent<NVRPhotonInteractableItem>();
                if (photonItem == null)
                {
                    photonItem = item.gameObject.AddComponent<NVRPhotonInteractableItem>();
                }

                photonItem.Rigidbody = item.Rigidbody;
                photonItem.InteractionPoint = item.InteractionPoint;
                photonItem.CanAttach = item.CanAttach;
                photonItem.AllowTwoHanded = item.AllowTwoHanded;
                photonItem.DisableKinematicOnAttach = item.DisableKinematicOnAttach;
                photonItem.EnableKinematicOnDetach = item.EnableKinematicOnDetach;
                photonItem.EnableGravityOnDetach = item.EnableGravityOnDetach;

                EditorUtility.SetDirty(photonItem);
                EditorUtility.SetDirty(photonItem.gameObject);

                DestroyImmediate(item, true);
            }

            EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("Completed adding photon components to " + interactables.Length + " NVRInteractables in the scene.");
        }

        [MenuItem("Tools/NewtonVR/Add photon components to all NVRInteractables in scene")]
        private static void AddPhotonComponentsToScene()
        {
            NVRInteractable[] interactables = GameObject.FindObjectsOfType<NVRInteractable>();

            foreach (NVRInteractable interactable in interactables)
            {
                if (interactable.Rigidbody != null)
                {
                    SetupForPhoton(interactable.Rigidbody.gameObject);
                }

                SetupForPhoton(interactable.gameObject);
            }

            EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("Completed adding photon components to " + interactables.Length + " NVRInteractables in the scene.");
        }

        [MenuItem("Tools/NewtonVR/Add photon components to selected prefab")]
        private static void AddPhotonComponentsToPrefab()
        {
            GameObject[] selection = Selection.gameObjects;

            NVRInteractable[] interactables;
            if (selection != null && selection.Length > 0)
            {
                interactables = selection.Select(element => element.GetComponent<NVRInteractable>()).Where(element => element != null).ToArray();
            }
            else
            {
                Debug.Log("No game objects selected.");
                return;
            }

            foreach (NVRInteractable interactable in interactables)
            {
                if (interactable.Rigidbody != null)
                {
                    SetupForPhoton(interactable.Rigidbody.gameObject);
                }

                SetupForPhoton(interactable.gameObject);
            }

            Debug.Log("Completed adding photon components to " + interactables.Length + " NVRInteractables in the scene.");
        }

        private static void SetupForPhoton(GameObject gameobject)
        {
            PhotonView photonView = gameobject.GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameobject.AddComponent<PhotonView>();
            }

            //if (photonView.synchronization == ViewSynchronization.Off)
            {
                photonView.synchronization = ViewSynchronization.UnreliableOnChange;
                EditorUtility.SetDirty(photonView);
            }
            photonView.ownershipTransfer = OwnershipOption.Takeover;

            if (photonView.ObservedComponents == null)
            {
                photonView.ObservedComponents = new System.Collections.Generic.List<Component>();
                EditorUtility.SetDirty(photonView);
            }

            if (gameobject.GetComponent<Rigidbody>() != null)
            {
                PhotonTransformView transformView = gameobject.GetComponent<PhotonTransformView>();
                if (transformView == null)
                {
                    transformView = gameobject.AddComponent<PhotonTransformView>();
                    photonView.ObservedComponents.Add(transformView);
                    EditorUtility.SetDirty(photonView);

                    PhotonTransformViewPositionModel positionModel = new PhotonTransformViewPositionModel();
                    positionModel.SynchronizeEnabled = true;
                    positionModel.TeleportEnabled = true;
                    positionModel.TeleportIfDistanceGreaterThan = 3;
                    positionModel.InterpolateOption = PhotonTransformViewPositionModel.InterpolateOptions.Lerp;
                    positionModel.InterpolateLerpSpeed = 5;
                    positionModel.ExtrapolateOption = PhotonTransformViewPositionModel.ExtrapolateOptions.Disabled;
                    positionModel.DrawErrorGizmo = true;

                    PhotonTransformViewRotationModel rotationModel = new PhotonTransformViewRotationModel();
                    rotationModel.SynchronizeEnabled = true;
                    rotationModel.InterpolateOption = PhotonTransformViewRotationModel.InterpolateOptions.Lerp;
                    rotationModel.InterpolateLerpSpeed = 5;

                    NVRHelpers.SetField(transformView, "m_PositionModel", positionModel, false);
                    NVRHelpers.SetField(transformView, "m_RotationModel", rotationModel, false);
                    EditorUtility.SetDirty(transformView);
                }

                PhotonRigidbodyView rigidbodyView = gameobject.GetComponent<PhotonRigidbodyView>();
                if (rigidbodyView == null)
                {
                    rigidbodyView = gameobject.AddComponent<PhotonRigidbodyView>();
                    photonView.ObservedComponents.Add(rigidbodyView);
                    EditorUtility.SetDirty(photonView);

                    NVRHelpers.SetField(rigidbodyView, "m_SynchronizeVelocity", true, false);
                    NVRHelpers.SetField(rigidbodyView, "m_SynchronizeAngularVelocity", true, false);
                    EditorUtility.SetDirty(rigidbodyView);
                }
            }

            EditorUtility.SetDirty(gameobject);
        }
    }
}