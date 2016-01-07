## Newton VR
Our system allows players to pick up, drop, throw, and use held objects. Items don't pass through other items (rigidbodies), or the environment (non-rigidbodies). Held items interact with other rigidbodies naturally - taking mass into account. For example, if you have two boxes of the same mass they can push each other equally, but a balloon, with considerably less mass, can't push a box. For more information on this style of mass based interaction [see this post by Nick Abel](http://www.vrinflux.com/newton-vr-physics-based-interaction-on-the-vive/). 
<img class='gfyitem' data-id='DistantPitifulAfricanhornbill' />

Items can be configured to be picked up at any point, or when grabbed can rotate and position themselves to match a predefined orientation. This lets you pick up a box from its corner as well as pick up a gun and have it orient to the grip. 
<img class='gfyitem' data-id='ImpureTautBergerpicard' />

We've created a few physical UI elements to help with basic configuration and menu type scenarios. We also give you the option to dynamically let the controllers turn into physical objects on a button press.
<img class='gfyitem' data-id='PointlessImperturbableBorzoi' />

<br>
## Grip buttons
A hotly debated issue is whether or not to use the grip buttons to pick things up. We feel like the benefit gained by using the grip buttons outweighs the trouble users can have with them. One of the benefits of releasing the code with this system is that if you disagree you're welcome to change the mappings. But, if you use the system with the defaults, then pressing grip button(s) will let you pick something up and releasing it will drop (or throw) the item. Using the grip buttons to hold an item frees up other buttons on the Vive controller for items that are designed to be used while held (for example holding a gun and then pressing the trigger button to fire). If your controller is *not* hovering over an interactable object, and you hit the grip button, your controller becomes a physical object that you can use to interact with the world. This allows you to press a button on a control panel for example.

<br>
## Implementation
Clone or download our repo here: https://github.com/TomorrowTodayLabs/NewtonVR/

We've included SteamVR so the project compiles and will try and keep the version updated. The meat of the project is in the NewtonVR folder. I recommend you clone the repo locally and create a symbolic link to your project so you can get updates and merge changes cleanly. You can get the desktop github client here: http://desktop.github.com. On windows, open a command line as administrator and use the following command to create a link: `mklink /D c:\git\MyProject\Assets\NewtonVR c:\git\NewtonVR\Assets\NewtonVR` The first parameter is the location you want to put NewtonVR and the second parameter is the location of your local NewtonVR repo. This is not required, just recommended.

After you've got the project you can check out our example scene in `NewtonVR/Example/NVRExampleScene`. We've got everything scaled up by a factor of 10 because PhysX seems to work more reliably with larger colliders. The scene includes one of each of everything:

##### NVRInteractableItem
There's some stacked boxes which have `NVRInteractableItem` components on them. There's a tiny box on top that you can use to try and push over the stack of boxes with to see an example of the mass based system. In the drawer there's a gun that has a configured `NVRInteractableItem.InteractionPoint` set to the handle. When you pick it up the system tries to rotate and position the gun in your hand, and keep it at that orientation.

##### NVRInteractableRotator
The door is an example of an object with a hinge that has a static position but that you want to rotate by dragging an edge. We've got the interaction script on just the door knobs, and `NVRInteractableRotator.Rigidbody` is set to the door's rigidbody. You could also just stick the actual script on the whole door if that makes more sense for your application. To get the currently selected angle (from a zeroed rotation) you can access `NVRInteractableRotator.CurrentAngle`.

##### NVRLetterSpinner
There's a letter selection spinner that inherits from NVRInteractableRotator. You can grab and spin it to select a letter. This isn't necessarily the best text input method for VR but it is a fun one. You can get the currently selected letter by calling `NVRLetterSpinner.GetLetter()`. 

##### NVRSlider
There's a slider example that lerps the color of a sphere between black and yellow. To get the slider's value you can check `NVRSlider.CurrentValue`. To setup this slider outside of the example you need to set the transforms `NVRSlider.StartPoint` to the slider's starting location, and `NVRSlider.EndPoint` to the slider's ending location. Like a lot of these UI Elements we've got a [Configurable Joint](http://docs.unity3d.com/Manual/class-ConfigurableJoint.html) attached to it to handle the limits and lock position / rotation.

##### NVRInteractableItem
The interactable item class can also be used to create dial or knob type elements. There's an example of this that reports the current angle of the knob. You can get the current rotation from simply accessing the local euler angles `NVRInteractableItem.transform.localEulerAngles.y`. 

##### NVRButton
To interact with a button you can either enable `NVRPlayer.PhysicalHands` and then press the grip buttons to turn your controllers physical, or put pressure on it with another object. The button in the example scene here has a script on it called `NVRExampleSpawner` which will spawn a cube when the button registers as pressed. Button presses are based off `NVRButton.DistanceToEngage`. If you move a button far enough from it's initial location then `NVRButton.ButtonDown` will trigger for a single frame. `NVRButton.ButtonIsPushed` will be true for as long as the button is down. Then, when the button moves back into its initial position, `NVRButton.ButtonUp` and `NVRButton.ButtonWasPushed` will trigger for that frame.

#### NVRSwitch
Like with `NVRButton`, `NVRSwitch` requires either physical hands, or another physical object to interact with it. The switch example in the scene controls a point light next to it. On Awake() it will set it's rotation to match the value of `NVRSwitch.CurrentState`. 

##### NVRExampleGun
There's a gun in the drawer that is a nice example of how to use pickup points with `NVRInteractableItem` as well as how to get input from that component. You can pick up the gun with the grip buttons and shoot with the trigger.

<br>
## Basic Integration
To integrate NewtonVR into a project you can use our included player prefab in `NewtonVR\NVRCameraRig`. This is a copy of the steamvr camerarig prefab with the newtonvr scripts added. Specifically, there's a `NVRPlayer` component on the root, a `NVRHead` component on the head, and `NVRHand` components on both hands. Alternatively, you can just add those components to your player. Take note though, if you're not using the standard controllers in your project then the physical hand option will not work correctly.

When you've got an item you'd like to pick up, simply drop a `NVRInteractableItem` component on it. You'll need to give it a Rigidbody (and ideally set the mass) if you haven't already. If the item has a specific point that you'd like to pick it up at you can create a new GameObject, parent it to your item, and position it in the location and at the rotation that you'd like the controller to be. Then set `NVRInteractableItem.InteractionPoint` to that new gameobject.

<br>
## Closing
We hope that this system can help you make an awesome VR experience! If you do be sure to let us know. Anybody is free to use it for basically any purpose: game jams, commercial games, educational apps, etc. Check the license for more info. We are actively using this system in our game and plan to update it as development continues. If you have questions or comments about this system you can contact us by leaving a message below, on twitter at [@TTLabsVR](http://www.twitter.com/ttlabsvr), or creating issues on the github.

<br>
*NewtonVR*<br>*Development: Keith Bradner, Nick Abel*<br>*UX: Adrienne Hunter*
