﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace NewtonVR
{
    public class NVRCanvasInput : BaseInputModule
    {
        public bool GeometryBlocksLaser = true;
        public LayerMask LayersThatBlockLaser = Physics.AllLayers;

        public Sprite CursorSprite;
        public Material CursorMaterial;
        public float NormalCursorScale = 0.05f;

        public bool LaserEnabled = true;
        public Color LaserColor = Color.blue;
        public float LaserStartWidth = 0.02f;
        public float LaserEndWidth = 0.001f;

        public bool OnCanvas;
        public bool CanvasUsed;
        
        private RectTransform[] Cursors;

        private LineRenderer[] Lasers;

        private GameObject[] CurrentPoint;
        private GameObject[] CurrentPressed;
        private GameObject[] CurrentDragging;

        private PointerEventData[] PointEvents;

        private bool Initialized = false;
        private bool DelayedInitialized = false;
        
        private Camera ControllerCamera;

        private NVRPlayer Player;

        protected override void Start()
        {
            base.Start();

            if (Initialized == false)
            {
                Player = this.GetComponent<NVRPlayer>();

                Cursors = new RectTransform[Player.Hands.Length];
                Lasers = new LineRenderer[Cursors.Length];

                ControllerCamera = new GameObject("Controller UI Camera").AddComponent<Camera>();
                ControllerCamera.transform.parent = this.transform;

                for (int index = 0; index < Cursors.Length; index++)
                {
                    GameObject cursor = new GameObject("Cursor for " + Player.Hands[index].gameObject.name);
                    cursor.transform.parent = this.transform;
                    cursor.transform.localPosition = Vector3.zero;
                    cursor.transform.localRotation = Quaternion.identity;

                    Canvas canvas = cursor.AddComponent<Canvas>();
                    cursor.AddComponent<CanvasRenderer>();
                    cursor.AddComponent<CanvasScaler>();
                    cursor.AddComponent<NVRUIIgnoreRaycast>();
                    cursor.AddComponent<GraphicRaycaster>();

                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.sortingOrder = 1000; //set to be on top of everything

                    Image image = cursor.AddComponent<Image>();
                    image.sprite = CursorSprite;
                    image.material = CursorMaterial;

                    if (LaserEnabled == true)
                    {
                        Lasers[index] = cursor.AddComponent<LineRenderer>();
                        Lasers[index].material = new Material(Shader.Find("Standard"));
                        Lasers[index].material.color = LaserColor;
                        NVRHelpers.LineRendererSetColor(Lasers[index], LaserColor, LaserColor);
                        NVRHelpers.LineRendererSetWidth(Lasers[index], LaserStartWidth, LaserEndWidth);
                        Lasers[index].useWorldSpace = true;
                        Lasers[index].enabled = false;
                    }

                    if (CursorSprite == null)
                        Debug.LogError("Set CursorSprite on " + this.gameObject.name + " to the sprite you want to use as your cursor.", this.gameObject);

                    Cursors[index] = cursor.GetComponent<RectTransform>();
                }

                CurrentPoint = new GameObject[Cursors.Length];
                CurrentPressed = new GameObject[Cursors.Length];
                CurrentDragging = new GameObject[Cursors.Length];
                PointEvents = new PointerEventData[Cursors.Length];

                Initialized = true;
            }
        }

        //this is broken up into two steps because of a unity bug. https://issuetracker.unity3d.com/issues/gl-dot-end-error-is-thrown-if-a-cameras-clear-flags-is-set-to-depth-only
        protected void DelayedCameraInit()
        {
            ControllerCamera.clearFlags = CameraClearFlags.Nothing;
            ControllerCamera.cullingMask = 0; // 1 << LayerMask.NameToLayer("UI"); 
            ControllerCamera.stereoTargetEye = StereoTargetEyeMask.None;

            Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                canvas.worldCamera = ControllerCamera;
            }

            DelayedInitialized = true;
        }

        // use screen midpoint as locked pointer location, enabling look location to be the "mouse"
        private bool GetLookPointerEventData(int index)
        {
            if (PointEvents[index] == null)
            {
                PointEvents[index] = new PointerEventData(base.eventSystem);
            }
            else
            {
                PointEvents[index].Reset();
            }

            PointEvents[index].delta = Vector2.zero;
            PointEvents[index].position = new Vector2(ControllerCamera.pixelWidth * 0.5f, ControllerCamera.pixelHeight * 0.5f);
            PointEvents[index].scrollDelta = Vector2.zero;

            base.eventSystem.RaycastAll(PointEvents[index], m_RaycastResultCache);
            PointEvents[index].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);

            if (PointEvents[index].pointerCurrentRaycast.gameObject != null)
            {
                OnCanvas = true; //gets set to false at the beginning of the process event
                m_RaycastResultCache.Clear();
                return true;
            }
            else
            {
                m_RaycastResultCache.Clear();
                return false;
            }
        }

        // update the cursor location and whether it is enabled
        // this code is based on Unity's DragMe.cs code provided in the UI drag and drop example
        private bool UpdateCursor(int index, PointerEventData pointData)
        {
            bool cursorState = false;

            if (PointEvents[index].pointerCurrentRaycast.gameObject != null && pointData.pointerEnter != null)
            {
                RectTransform draggingPlane = pointData.pointerEnter.GetComponent<RectTransform>();
                Vector3 globalLookPos;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, pointData.position, pointData.enterEventCamera, out globalLookPos))
                {
                    //do real physics raycast.
                    Vector3 origin = Player.Hands[index].CurrentPosition;
                    Vector3 direction = Player.Hands[index].CurrentForward;
                    Vector3 endPoint = globalLookPos;
                    float distance = Vector3.Distance(origin, endPoint);

                    bool blockedByGeometry = false;

                    if (GeometryBlocksLaser == true)
                    {
                        blockedByGeometry = Physics.Raycast(origin, direction, distance, LayersThatBlockLaser);
                    }

                    if (blockedByGeometry == false)
                    {
                        cursorState = true;

                        Cursors[index].position = globalLookPos;
                        Cursors[index].rotation = draggingPlane.rotation;
                        Cursors[index].localScale = Vector3.one * (NormalCursorScale / 100);

                        if (LaserEnabled == true)
                        {
                            Lasers[index].enabled = true;
                            Lasers[index].SetPositions(new Vector3[] { origin, endPoint });
                        }
                    }

                }
            }

            Cursors[index].gameObject.SetActive(cursorState);
            return cursorState;
        }
        
        public void ClearSelection()
        {
            if (base.eventSystem.currentSelectedGameObject)
            {
                base.eventSystem.SetSelectedGameObject(null);
            }
        }
        
        private void Select(GameObject go)
        {
            ClearSelection();

            if (ExecuteEvents.GetEventHandler<ISelectHandler>(go))
            {
                base.eventSystem.SetSelectedGameObject(go);
            }
        }
        
        private bool SendUpdateEventToSelectedObject()
        {
            if (base.eventSystem.currentSelectedGameObject == null)
                return false;

            BaseEventData data = GetBaseEventData();

            ExecuteEvents.Execute(base.eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);

            return data.used;
        }

        private void UpdateCameraPosition(int index)
        {
            ControllerCamera.transform.position = Player.Hands[index].CurrentPosition;
            ControllerCamera.transform.forward = Player.Hands[index].CurrentForward;
        }

        // Process is called by UI system to process events
        public override void Process() { } //seems to be broken in unity 5.5.0f3  //todo: Assess
        private void Update()
        {
            OnCanvas = false;
            CanvasUsed = false;

            if (Initialized == false)
                return;
            if (DelayedInitialized == false)
            {
                DelayedCameraInit();
            }

            // send update events if there is a selected object - this is important for InputField to receive keyboard events
            SendUpdateEventToSelectedObject();

            // see if there is a UI element that is currently being looked at
            for (int index = 0; index < Cursors.Length; index++)
            {
                if (Player.Hands[index].gameObject.activeInHierarchy == false || Player.Hands[index].IsCurrentlyTracked == false)
                {
                    if (Cursors[index].gameObject.activeInHierarchy == true)
                    {
                        Cursors[index].gameObject.SetActive(false);
                    }
                    continue;
                }

                UpdateCameraPosition(index);

                bool hit = GetLookPointerEventData(index);

                CurrentPoint[index] = PointEvents[index].pointerCurrentRaycast.gameObject;

                // handle enter and exit events (highlight)
                base.HandlePointerExitAndEnter(PointEvents[index], CurrentPoint[index]);

                // update cursor
                bool cursorActive = UpdateCursor(index, PointEvents[index]);

                if (hit == false)
                    continue;

                if (Player.Hands[index] != null && cursorActive == true)
                {
                    if (ButtonDown(index))
                    {
                        ClearSelection();

                        PointEvents[index].pressPosition = PointEvents[index].position;
                        PointEvents[index].pointerPressRaycast = PointEvents[index].pointerCurrentRaycast;
                        PointEvents[index].pointerPress = null;

                        if (CurrentPoint[index] != null)
                        {
                            CurrentPressed[index] = CurrentPoint[index];

                            GameObject newPressed = ExecuteEvents.ExecuteHierarchy(CurrentPressed[index], PointEvents[index], ExecuteEvents.pointerDownHandler);

                            if (newPressed == null)
                            {
                                // some UI elements might only have click handler and not pointer down handler
                                newPressed = ExecuteEvents.ExecuteHierarchy(CurrentPressed[index], PointEvents[index], ExecuteEvents.pointerClickHandler);
                                if (newPressed != null)
                                {
                                    CurrentPressed[index] = newPressed;
                                }
                            }
                            else
                            {
                                CurrentPressed[index] = newPressed;
                            }

                            if (newPressed != null)
                            {
                                PointEvents[index].pointerPress = newPressed;
                                CurrentPressed[index] = newPressed;
                                Select(CurrentPressed[index]);
                                CanvasUsed = true;
                            }

                            ExecuteEvents.Execute(CurrentPressed[index], PointEvents[index], ExecuteEvents.beginDragHandler);
                            PointEvents[index].pointerDrag = CurrentPressed[index];
                            CurrentDragging[index] = CurrentPressed[index];
                        }
                    }

                    if (ButtonUp(index))
                    {
                        if (CurrentDragging[index])
                        {
                            ExecuteEvents.Execute(CurrentDragging[index], PointEvents[index], ExecuteEvents.endDragHandler);
                            if (CurrentPoint[index] != null)
                            {
                                ExecuteEvents.ExecuteHierarchy(CurrentPoint[index], PointEvents[index], ExecuteEvents.dropHandler);
                            }
                            PointEvents[index].pointerDrag = null;
                            CurrentDragging[index] = null;
                        }
                        if (CurrentPressed[index])
                        {
                            //todo maybe mouse up and click should go in different places / times?
                            ExecuteEvents.Execute(CurrentPressed[index], PointEvents[index], ExecuteEvents.pointerClickHandler);

                            ExecuteEvents.Execute(CurrentPressed[index], PointEvents[index], ExecuteEvents.pointerUpHandler);
                            PointEvents[index].rawPointerPress = null;
                            PointEvents[index].pointerPress = null;
                            CurrentPressed[index] = null;
                        }
                    }

                    // drag handling
                    if (CurrentDragging[index] != null)
                    {
                        ExecuteEvents.Execute(CurrentDragging[index], PointEvents[index], ExecuteEvents.dragHandler);
                    }
                }
            }
        }

        private bool ButtonDown(int index)
        {
            return Player.Hands[index].Inputs[NVRButtons.Trigger].PressDown;
        }

        private bool ButtonUp(int index)
        {
            return Player.Hands[index].Inputs[NVRButtons.Trigger].PressUp;
        }
    }
}