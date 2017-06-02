using UnityEngine;
using System.Collections.Generic;

/// <summary>
///     Handles the network culling.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class NetworkCullingHandler : MonoBehaviour, IPunObservable
{
    #region VARIABLES
    
    private int orderIndex;

    private CullArea cullArea;

    private List<int> previousActiveCells, activeCells;
    
    private PhotonView pView;

    private Vector3 lastPosition, currentPosition;

    #endregion

    #region UNITY_FUNCTIONS

    /// <summary>
    ///     Gets references to the PhotonView component and the cull area game object.
    /// </summary>
    private void OnEnable()
    {
        if (pView == null)
        {
            pView = GetComponent<PhotonView>();

            if (!pView.isMine)
            {
                return;
            }
        }
        
        if (cullArea == null)
        {
            cullArea = GameObject.FindObjectOfType<CullArea>();
        }
        
        previousActiveCells = new List<int>(0);
        activeCells = new List<int>(0);

        currentPosition = lastPosition = transform.position;
    }

    /// <summary>
    ///     Initializes the right interest group or prepares the permanent change of the interest group of the PhotonView component.
    /// </summary>
    private void Start()
    {
        if (!pView.isMine)
        {
            return;
        }

        if (PhotonNetwork.inRoom)
        {
            if (cullArea.NumberOfSubdivisions == 0)
            {
                pView.group = cullArea.FIRST_GROUP_ID;

                PhotonNetwork.SetReceivingEnabled(cullArea.FIRST_GROUP_ID, true);
                PhotonNetwork.SetSendingEnabled(cullArea.FIRST_GROUP_ID, true);
            }
            else
            {
                // This is used to continuously update the active group.
                pView.ObservedComponents.Add(this);
            }
        }
    }

    /// <summary>
    ///     Checks if the player has moved previously and updates the interest groups if necessary.
    /// </summary>
    private void Update()
    {
        if (!pView.isMine)
        {
            return;
        }

        lastPosition = currentPosition;
        currentPosition = transform.position;

        // This is a simple position comparison of the current and the previous position. 
        // When using Network Culling in a bigger project keep in mind that there might
        // be more transform-related options, e.g. the rotation, or other options to check.
        if (currentPosition != lastPosition)
        {
            if (HaveActiveCellsChanged())
            {
                UpdateInterestGroups();
            }
        }
    }

    /// <summary>
    ///     Drawing informations.
    /// </summary>
    private void OnGUI()
    {
        if (!pView.isMine)
        {
            return;
        }

        string subscribedAndActiveCells = "Inside cells:\n";
        string subscribedCells = "Subscribed cells:\n";

        for (int index = 0; index < activeCells.Count; ++index)
        {
            if (index <= cullArea.NumberOfSubdivisions)
            {
                subscribedAndActiveCells += activeCells[index] + "  ";
            }

            subscribedCells += activeCells[index] + "  ";
        }

        GUI.Label(new Rect(20.0f, Screen.height - 100.0f, 200.0f, 40.0f), "<color=white>" + subscribedAndActiveCells + "</color>", new GUIStyle() { alignment = TextAnchor.UpperLeft, fontSize = 16 });
        GUI.Label(new Rect(20.0f, Screen.height - 60.0f, 200.0f, 40.0f), "<color=white>" + subscribedCells + "</color>", new GUIStyle() { alignment = TextAnchor.UpperLeft, fontSize = 16 });
    }

    #endregion

    /// <summary>
    ///     Checks if the previously active cells have changed.
    /// </summary>
    /// <returns>True if the previously active cells have changed and false otherwise.</returns>
    private bool HaveActiveCellsChanged()
    {
        if (cullArea.NumberOfSubdivisions == 0)
        {
            return false;
        }

        previousActiveCells = new List<int>(activeCells);
        activeCells = cullArea.GetActiveCells(transform.position);

        // If the player leaves the area we insert the whole area itself as an active cell.
        // This can be removed if it is sure that the player is not able to leave the area.
        while (activeCells.Count <= cullArea.NumberOfSubdivisions)
        {
            activeCells.Add(cullArea.FIRST_GROUP_ID);
        }

        if (activeCells.Count != previousActiveCells.Count)
        {
            return true;
        }

        if (activeCells[cullArea.NumberOfSubdivisions] != previousActiveCells[cullArea.NumberOfSubdivisions])
        {
            return true;
        }

        return false;
    }
    
    /// <summary>
    ///     Unsubscribes from old and subscribes to new interest groups.
    /// </summary>
    private void UpdateInterestGroups()
    {
        List<int> disable = new List<int>(0);

        foreach (int groupId in previousActiveCells)
        {
            if (!activeCells.Contains(groupId))
            {
                disable.Add(groupId);
            }
        }
        
        PhotonNetwork.SetReceivingEnabled(activeCells.ToArray(), disable.ToArray());
        PhotonNetwork.SetSendingEnabled(activeCells.ToArray(), disable.ToArray());
    }

    #region IPunObservable implementation

    /// <summary>
    ///     This time OnPhotonSerializeView is not used to send or receive any kind of data.
    ///     It is used to change the currently active group of the PhotonView component, making it work together with PUN more directly.
    ///     Keep in mind that this function is only executed, when there is at least one more player in the room.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // If the player leaves the area we insert the whole area itself as an active cell.
        // This can be removed if it is sure that the player is not able to leave the area.
        while (activeCells.Count <= cullArea.NumberOfSubdivisions)
        {
            activeCells.Add(cullArea.FIRST_GROUP_ID);
        }

        if (cullArea.NumberOfSubdivisions == 1)
        {
            orderIndex = (++orderIndex % cullArea.SUBDIVISION_FIRST_LEVEL_ORDER.Length);
            pView.group = activeCells[cullArea.SUBDIVISION_FIRST_LEVEL_ORDER[orderIndex]];
        }
        else if (cullArea.NumberOfSubdivisions == 2)
        {
            orderIndex = (++orderIndex % cullArea.SUBDIVISION_SECOND_LEVEL_ORDER.Length);
            pView.group = activeCells[cullArea.SUBDIVISION_SECOND_LEVEL_ORDER[orderIndex]];
        }
        else if (cullArea.NumberOfSubdivisions == 3)
        {
            orderIndex = (++orderIndex % cullArea.SUBDIVISION_THIRD_LEVEL_ORDER.Length);
            pView.group = activeCells[cullArea.SUBDIVISION_THIRD_LEVEL_ORDER[orderIndex]];
        }
    }

    #endregion
}