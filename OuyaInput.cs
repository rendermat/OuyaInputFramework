using UnityEngine;
using System;

/* GLOBAL ENUM DATA TYPES */ 
// defining button states and events
// pressed: the button is pressed down
// upFrame: the moment (frame) the button goes up
// downFrame: the moment (frame) the button goes down
public enum ButtonAction {Pressed, UpFrame, DownFrame}
// describing the Ouya controller buttons
public enum OuyaButton {O, U, Y, A, LB, RB, L3, R3, LT, RT, DU, DD, DL, DR, START, SELECT, SYSTEM}
// describing the Ouya controller axis
public enum OuyaAxis {LX, LY, RX, RY, LT, RT, DX, DY}
// describing the three joystick types
public enum OuyaJoystick {LeftStick, RightStick, DPad}
// describing the two triggers on the Ouya controller
public enum OuyaTrigger {Left, Right}
// describing players on the console
public enum OuyaPlayer {None = 0, P01 = 1, P02 = 2, P03 = 3, P04 = 4, P05 = 5, P06 = 6, P07 = 7, P08 = 8, P09 = 9, P10 = 10, P11 = 11}
// defining known controller types
public enum OuyaControllerType {Broadcom, GameStick, MogaPro, Ouya, PS3, XBox360, TattieBogle, Unknown, None}
// defining test platform types
public enum EditorWorkPlatform {MacOS, Windows}
// defining deadzone types
public enum DeadzoneType {AxialClip, CircularClip, CircularMap}
// defining angular quadrants of joystick input
public enum Quadrant {I, II, III, IV, OnXPos, OnXNeg, OnYPos, OnYNeg, Zero}

public static class OuyaInput
{
	/* PROPERTIES */ 
	// adjust this value to set the check interval (consider performance)
	// this is just for checking if a controller was unplugged or added
	private const int playersMax = 11;
	
	// setting for platform specific djustments
	private static float plugCheckInterval = 3;
	private static bool scanContiniously = true;
	private static float deadzoneRadius = 0.25f;
	private static float triggerThreshold = 0.10f;
	
	private static EditorWorkPlatform editorWorkPlatform = EditorWorkPlatform.MacOS;
	private static DeadzoneType deadzoneType = DeadzoneType.CircularClip;
	
	/* TEMPORARY */ 
	// the time of the last joystick lis update
	private static float lastPlugCheckTime = 0;
	// a list of all available controller names
	private static string[] controllerNames = null;
	// a list of all available controller types derived from names
	private static OuyaControllerType[] controllerTypes = null;
	// a list of active controller mapping containers
	private static PlayerController[] playerControllers = null;
	// counting all connected controllers
	private static int controllerCount = 0;
	
	
	/* -----------------------------------------------------------------------------------
	 * INITIALIZATION
	 */
	
	static OuyaInput() {
		/* static constructor
		 */
		// create an array of classes caching input mapping strings
		playerControllers = new PlayerController[playersMax];
		// create an array for storing the controller types
		controllerTypes = new OuyaControllerType[playersMax];
		// initialize every field of that array
		for (int i = 0; i < playersMax; i++) {
			controllerTypes[i] = OuyaControllerType.None;
		}
	}
	
	/* -----------------------------------------------------------------------------------
	 * NESTED TRANSLATOR CONTAINERS
	 */
	
	#region CACHED MAPPING CLASS
	
	private class PlayerController {
		/* container class caching the axis name strings for one controller
		 * we use that to cache mapping strings and prevent gabage collection
		 */
		public OuyaPlayer player;
		public OuyaControllerType controllerType;
		
		// axis strings caching
		public string map_LX = null;
		public string map_LY = null; 
		public string map_RX = null; 
		public string map_RY = null; 
		public string map_LT = null; 
		public string map_RT = null; 
		public string map_DX = null; 
		public string map_DY = null; 
		
		// invert flags for axis
		public bool invert_LX = false;
		public bool invert_LY = false;
		public bool invert_RX = false;
		public bool invert_RY = false;
		public bool invert_LT = false;
		public bool invert_RT = false;
		public bool invert_DX = false;
		public bool invert_DY = false;
		
		// flags for trigger button events from axis
		public bool upEventLT = false;
		public bool upEventRT = false;
		public bool downEventLT = false;
		public bool downEventRT = false;
		public bool downStateLT = false;
		public bool downStateRT = false;
		
		private bool initAxisLT = false;
		private bool initAxisRT = false;
		
		// flags for d-pad button events from axis
		public bool upEventDU = false;
		public bool upEventDD = false;
		public bool upEventDR = false;
		public bool upEventDL = false;
		public bool downEventDU = false;
		public bool downEventDD = false;
		public bool downEventDR = false;
		public bool downEventDL = false;
		public bool downStateDU = false;
		public bool downStateDD = false;
		public bool downStateDR = false;
		public bool downStateDL = false;
		
		
		public PlayerController(OuyaPlayer player, OuyaControllerType controllerType) {
			/* constructor
			 */
			this.controllerType = controllerType;
			this.player = player;
		}
		
		public bool checkControllerType(OuyaControllerType againstType) {
			/* returns true if the controller type equals that of the given one
			 */
			if (againstType == controllerType) return true;
			else return false;
		}
		
		public void setController(OuyaPlayer player, OuyaControllerType controllerType) {
			/* allows to reinitialize the ID of an existing controller mapping
			 */
			this.controllerType = controllerType;
			this.player = player;
		}
		
		public string getAxisID(OuyaAxis ouyaButton) {
			/* returns the cached axis ID called for
			 */
			switch (ouyaButton) {
			case OuyaAxis.LX: return map_LX;
            case OuyaAxis.LY: return map_LY;
            case OuyaAxis.RX: return map_RX;
            case OuyaAxis.RY: return map_RY;
            case OuyaAxis.LT: return map_LT;
            case OuyaAxis.RT: return map_RT;
            case OuyaAxis.DX: return map_DX;
            case OuyaAxis.DY: return map_DY;
			default: return null;
			}
		}
		
		public bool getAxisInvert(OuyaAxis ouyaButton) {
			/* returns a flag indicating whether the axis needs inversion
			 */
			switch (ouyaButton) {
			case OuyaAxis.LX: return invert_LX;
            case OuyaAxis.LY: return invert_LY;
            case OuyaAxis.RX: return invert_RX;
            case OuyaAxis.RY: return invert_RY;
            case OuyaAxis.LT: return invert_LT;
            case OuyaAxis.RT: return invert_RT;
            case OuyaAxis.DX: return invert_DX;
            case OuyaAxis.DY: return invert_DY;
			default: return false;
			}
		}
		
		public float rangeMapTriggerAxis(float axisValue, OuyaAxis triggerAxis) {
			/* this remapping method allows to convert trigger axis values
			 * we want to bring all trigger axis values into arange on 0 to 1
			 * this is needed for the MacOSX XBOX360 driver which provides values from -1 to 1
			 */
			// check if the trigger axis was initialized
			switch (triggerAxis) {
			case OuyaAxis.LT: if (!initAxisLT && axisValue != 0f) initAxisLT = true; break;
			case OuyaAxis.RT: if (!initAxisRT && axisValue != 0f) initAxisRT = true; break;
			}
			// we check if the controller we have need range mapping
			switch (controllerType) {
			case OuyaControllerType.TattieBogle:
				// remap values from range -1to1 onto range 0to1
				switch (triggerAxis) {
				case OuyaAxis.LT:
					// we only retrun a converted value if the trigger was initialized
					// otherwise we would get out 0.5 initially without pressing the trigger
					if (initAxisLT) return ((axisValue + 1) / 2f);
					else return 0f;
				case OuyaAxis.RT:
					// we only retrun a converted value if the trigger was initialized
					// otherwise we would get out 0.5 initially without pressing the trigger
					if (initAxisRT) return ((axisValue + 1) / 2f);
					else return 0f;
				default: return 0f;
				}
			default:
				// just retrun the value without changes
				return axisValue;
			}
		}
		
		public void scanController() {
			/* this method can be called every frame to gather button events for single frames
			 * it is the core of a button state manager
			 */
			// we start with consuming all older events
			downEventLT = false; upEventLT = false;
			downEventRT = false; upEventRT = false;
			downEventDU = false; upEventDU = false;
			downEventDD = false; upEventDD = false;
			downEventDR = false; upEventDR = false;
			downEventDL = false; upEventDL = false;
			
			// prepare a field for temporary storage
			bool down = false;
			
			/* LEFT TRIGGER */ 
			// get the current state of the left trigger
			down = OuyaInput.GetButton(OuyaButton.LT, ButtonAction.Pressed, player);
			// compare that state to the state of the last frame
			if (down) {
				// set an down event if the button was not down before
				if (!downStateLT) {downStateLT = true; downEventLT = true;}
			} else {
				// set an up event if the button was down before
				if (downStateLT) {downStateLT = false; upEventLT = true;}
			}
			/* RIGHT TRIGGER */ 
			// get the current state of the right trigger
			down = OuyaInput.GetButton(OuyaButton.RT, ButtonAction.Pressed, player);
			// compare that state to the state of the last frame
			if (down) {
				// set an down event if the button was not down before
				if (!downStateRT) {downStateRT = true; downEventRT = true;}
			} else {
				// set an up event if the button was down before
				if (downStateRT) {downStateRT = false; upEventRT = true;}
			}
			/* D-PAD UP */ 
			// get the current state of the d-pad up
			down = OuyaInput.GetButton(OuyaButton.DU, ButtonAction.Pressed, player);
			// compare that state to the state of the last frame
			if (down) {
				// set an down event if the button was not down before
				if (!downStateDU) {downStateDU = true; downEventDU = true;}
			} else {
				// set an up event if the button was down before
				if (downStateDU) {downStateDU = false; upEventDU = true;}
			}
			/* D-PAD DOWN */ 
			// get the current state of the d-pad up
			down = OuyaInput.GetButton(OuyaButton.DD, ButtonAction.Pressed, player);
			// compare that state to the state of the last frame
			if (down) {
				// set an down event if the button was not down before
				if (!downStateDD) {downStateDD = true; downEventDD = true;}
			} else {
				// set an up event if the button was down before
				if (downStateDD) {downStateDD = false; upEventDD = true;}
			}
			/* D-PAD RIGHT */ 
			// get the current state of the d-pad up
			down = OuyaInput.GetButton(OuyaButton.DR, ButtonAction.Pressed, player);
			// compare that state to the state of the last frame
			if (down) {
				// set an down event if the button was not down before
				if (!downStateDR) {downStateDR = true; downEventDR = true;}
			} else {
				// set an up event if the button was down before
				if (downStateDR) {downStateDR = false; upEventDR = true;}
			}
			/* D-PAD LEFT */ 
			// get the current state of the d-pad up
			down = OuyaInput.GetButton(OuyaButton.DL, ButtonAction.Pressed, player);
			// compare that state to the state of the last frame
			if (down) {
				// set an down event if the button was not down before
				if (!downStateDL) {downStateDL = true; downEventDL = true;}
			} else {
				// set an up event if the button was down before
				if (downStateDL) {downStateDL = false; upEventDL = true;}
			}
		}
		
		public void resetControllerCache() {
			/* method to reset all flags we use when scanning continiously for trigger events
			 */
			downEventLT = false; upEventLT = false; downStateLT = false;
			downEventRT = false; upEventRT = false; downStateRT = false;
			
			upEventDU = false; downEventDU = false; downStateDU = false;
			upEventDD = false; downEventDD = false;	downStateDD = false;
			upEventDR = false; downEventDR = false;	downStateDR = false;
			upEventDL = false; downEventDL = false;	downStateDL = false;
		}
	}
	#endregion
	
	/* -----------------------------------------------------------------------------------
	 * CONTROLLER UPDATE
	 */
	
	#region CONTROLLER SETUP & UPDATE
	
	public static void SetEditorPlatform(EditorWorkPlatform workPlatform) {
		/* sets a work platform for the editor
		 * this allows to manage platform specific input for different working environments
		 * this is necessary as precompile macros do not work for finding out on which
		 * platform our editor is currently running – the check only standalone builds
		 */
		editorWorkPlatform = workPlatform;
	}
	
	public static void SetContiniousScanning(bool active) {
		/* allows to activate continious controller scanning
		 * this is needed to retreive ButtonUp and BottonDown events for triggers and d-pads
		 */
		scanContiniously = active;
	}
	
	public static void SetPlugCheckInterval(float seconds) {
		/* allows to adjust the interval for controller plug checks
		 * the smaller the interval is the earlier new controllers get noticed
		 */
		plugCheckInterval = seconds;
	}
	
	public static void UpdateControllers() {
		/* method to check if joystick where plugged or unplugged
		 * updates the list of joysticks
		 * this should be called at Start() and in Update()
		 */
		// we only do a joystick plug check every 3 seconds
        if ((Time.time - lastPlugCheckTime) > plugCheckInterval) {
			// store the time of the current plug check
            lastPlugCheckTime = Time.time;
			
			/* GET CONTROLLERS */
			// get joystick names from Unity
            controllerNames = Input.GetJoystickNames();
			// count the connected controllers
			controllerCount = controllerNames.Length;
			
			/* MAP CONTROLLERS */
			// create a controller types eum array
			// we do that to avoid string comparissons every frame for each axis
			// the mirror conversion into enums avoids excessive string creation and GC
			if (controllerNames != null) {
				// convert each string in the array into the corresponding enum
				// create mapping container objects for each new controller
				for (int i = 0; i < controllerCount; i++)
				{
					// set the controller type for each controller name
					controllerTypes[i] = findControllerType(i, controllerNames[i]);
					
					/* (RE)USE EXISTING MAPPING */ 
					// check if we already have a map for this controller
					if (playerControllers[i] != null)
					{
						// if we have a map we make sure that it is still of the same controller type
						if (!playerControllers[i].checkControllerType(controllerTypes[i])) {
							// reinitialize the type of the existing mapping if needed
							playerControllers[i].setController((OuyaPlayer)(i+1), controllerTypes[i]);
							// redo the mapping in the existing container for the new controller
							mapController(playerControllers[i]);
						}
					}
					/* CREATE NEW MAPPING */ 
					else {
						// if not we create a new one
						playerControllers[i] = new  PlayerController((OuyaPlayer)(i+1), controllerTypes[i]);
						// set the mappings for the new controller
						mapController(playerControllers[i]);
					}
				}
				// we reset all the controller type fields that are not used anymore
				for (int i = controllerCount; i < playersMax; i++) {
					controllerTypes[i] = OuyaControllerType.None;
				}
			}
			/* CLEAR CONTROLLER MAPS */
			else {
				// if we found no controller names we reset the counter
				controllerCount = 0;
				
				for (int i = 0; i < playersMax; i++) {
					// we reset all controller types in the array
					controllerTypes[i] = OuyaControllerType.None;
					// we also clear all mappings that are not used anymore
					playerControllers[i] = null;
				}
			}
        }
		/* CONTINIOUS JOYSTICK LIST */
    	// this block is a state manager that allows to get button events for native axes buttons
		if (scanContiniously && controllerCount > 0)
		{
			// scan controllers to gather button down or up events for triggers
			for (int i = 0; i < controllerCount; i++) {
				playerControllers[i].scanController();
			}
		}
	}
	
			
	public static void ResetInput() {
		/* resets the Unity input as well as any cached button events here
		 */
		// reset the axis input in Unity
		Input.ResetInputAxes();
		
		// reset all the player controllers
		for (int i = 0; i < playerControllers.Length; i++) {
			// check if this controller mapping exists
			if (playerControllers[i] != null) {
				// reset the controller mapping in case
				playerControllers[i].resetControllerCache();
			}
		}
	}
	#endregion
	
	/* -----------------------------------------------------------------------------------
	 * CONTROLLER MAPPING CACHE
	 */
	
	#region CONTROLLER MAPPING
	
	private static OuyaControllerType findControllerType(int index, string controllerName) {
		/* a helper for the updateJoystickList() method
		 * converts controller string names into enum descriptions
		 */
		switch (controllerName.ToUpper()) {
			case "BROADCOM BLUETOOTH HID":
				controllerTypes[index] = OuyaControllerType.Broadcom;
				return OuyaControllerType.Broadcom;
			case "MOGA PRO HID":
				controllerTypes[index] = OuyaControllerType.MogaPro;
				return OuyaControllerType.MogaPro;
			case "OUYA GAME CONTROLLER":
				controllerTypes[index] = OuyaControllerType.Ouya;
				return OuyaControllerType.Ouya;
			case "GAMESTICK CONTROLLER 1":
			case "GAMESTICK CONTROLLER 2": 
			case "GAMESTICK CONTROLLER 3":
			case "GAMESTICK CONTROLLER 4":
				controllerTypes[index] = OuyaControllerType.GameStick;
				return OuyaControllerType.GameStick;
			case "XBOX 360 WIRELESS RECEIVER":
            case "CONTROLLER (AFTERGLOW GAMEPAD FOR XBOX 360)":
            case "CONTROLLER (ROCK CANDY GAMEPAD FOR XBOX 360)":
            case "CONTROLLER (XBOX 360 WIRELESS RECEIVER FOR WINDOWS)":
            case "MICROSOFT X-BOX 360 PAD":
            case "CONTROLLER (XBOX 360 FOR WINDOWS)":
            case "CONTROLLER (XBOX360 GAMEPAD)":
            case "XBOX 360 FOR WINDOWS (CONTROLLER)":
				controllerTypes[index] = OuyaControllerType.XBox360;
				return OuyaControllerType.XBox360;
			case "MOTIONINJOY VIRTUAL GAME CONTROLLER":
			case "SONY PLAYSTATION(R)3 CONTROLLER":
			case "PLAYSTATION(R)3 CONTROLLER":
				controllerTypes[index] = OuyaControllerType.PS3;
				return OuyaControllerType.PS3;
			case "":
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
				controllerTypes[index] = OuyaControllerType.TattieBogle;
				return OuyaControllerType.TattieBogle;
#else
				controllerTypes[index] = OuyaControllerType.Unknown;
				return OuyaControllerType.Unknown;
#endif
			default:
				controllerTypes[index] = OuyaControllerType.Unknown;
				return OuyaControllerType.Unknown;		
		}
	}
	
	private static void mapController(PlayerController playerController) {
		/* constructs a specific string for every axis the controller type has
		 * these strings can be used to acces Unity input directly later
		 * the strins are stored in the given mapping container
		 * we also set invert flags for each axis here
		 * ToDo test and adjust all invert flags
		 */
		// this is a good point to reset the input in Unity
		Input.ResetInputAxes();
		// we also reset all cached button states
		playerController.resetControllerCache();
		
		// convert the player enum into an integer
		int player = (int)playerController.player;
	
		// construct axis name for the controller type and player number
		// the generic axis are different for Android ports and other platforms (Editor)
		// the inversed encoding for axis values is also cared for here
		// this is solved by precompile makros
		switch (playerController.controllerType)
		{
#if !UNITY_EDITOR && UNITY_ANDROID
            case OuyaControllerType.Broadcom:
            case OuyaControllerType.MogaPro:
           		playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;
                playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;
                playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = false;
                playerController.map_LT = string.Format("Joy{0} Axis 8", player); playerController.invert_LT = false;
                playerController.map_RT = string.Format("Joy{0} Axis 7", player); playerController.invert_RT = false;
                playerController.map_DX = string.Format("Joy{0} Axis 5", player); playerController.invert_DX = false;
                playerController.map_DY = string.Format("Joy{0} Axis 6", player); playerController.invert_DY = false;
				break;
			
			case OuyaControllerType.GameStick:
           		playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
                playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
                playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
                playerController.map_DX = string.Format("Joy{0} Axis 5", player); playerController.invert_DX = false;		// checked
                playerController.map_DY = string.Format("Joy{0} Axis 6", player); playerController.invert_DY = true;		// checked
				// the GameStick controller has no triggers at all
				playerController.map_LT = null; playerController.invert_LT = false;
                playerController.map_RT = null; playerController.invert_RT = false;
                break;

            case OuyaControllerType.Ouya:
                playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
                playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
                playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
                playerController.map_LT = string.Format("Joy{0} Axis 5", player); playerController.invert_LT = false;		// checked
                playerController.map_RT = string.Format("Joy{0} Axis 6", player); playerController.invert_RT = false;		// checked
                // the dpad is not analog and therefore not mapped as an axis		
				playerController.map_DX = null; playerController.invert_DX = false;
				playerController.map_DY = null; playerController.invert_DY = false;	
				break;
#endif
			case OuyaControllerType.XBox360:
#if !UNITY_EDITOR && UNITY_ANDROID 
				playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
                playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
                playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
                playerController.map_LT = string.Format("Joy{0} Axis 7", player); playerController.invert_LT = false;		// checked
                playerController.map_RT = string.Format("Joy{0} Axis 8", player); playerController.invert_RT = false;		// checked
                playerController.map_DX = string.Format("Joy{0} Axis 5", player); playerController.invert_DX = false;		// checked
                playerController.map_DY = string.Format("Joy{0} Axis 6", player); playerController.invert_DY = true;		// checked
#else
                playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
                playerController.map_RX = string.Format("Joy{0} Axis 4", player); playerController.invert_RX = false;		// checked		
                playerController.map_RY = string.Format("Joy{0} Axis 5", player); playerController.invert_RY = true;		// checked
                playerController.map_LT = string.Format("Joy{0} Axis 9", player); playerController.invert_LT = false;		// checked
                playerController.map_RT = string.Format("Joy{0} Axis 10", player); playerController.invert_RT = false;		// checked
                playerController.map_DX = string.Format("Joy{0} Axis 6", player); playerController.invert_DX = false;		// checked
                playerController.map_DY = string.Format("Joy{0} Axis 7", player); playerController.invert_DY = false;		// checked
#endif
                break;
			case OuyaControllerType.PS3:
#if !UNITY_EDITOR && UNITY_ANDROID
                playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
                playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
                playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
                playerController.map_LT = string.Format("Joy{0} Axis 5", player); playerController.invert_LT = false;		// checked
                playerController.map_RT = string.Format("Joy{0} Axis 6", player); playerController.invert_RT = false;		// checked
				// the dpad is not analog and therefore not mapped as an axis	
				playerController.map_DX = null; playerController.invert_DX = false;
				playerController.map_DY = null; playerController.invert_DY = false;	
#elif !UNITY_EDITOR && UNITY_STANDALONE_WIN
                playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
                playerController.map_RX = string.Format("Joy{0} Axis 4", player); playerController.invert_RX = false;		// checked
                playerController.map_RY = string.Format("Joy{0} Axis 5", player); playerController.invert_RY = true;		// checked
                playerController.map_LT = string.Format("Joy{0} Axis 3", player); playerController.invert_LT = false;		// checked
                playerController.map_RT = string.Format("Joy{0} Axis 6", player); playerController.invert_RT = false;		// checked	
				// the dpad is not analog and therefore not mapped as an axis	
				playerController.map_DX = null; playerController.invert_DX = false;
				playerController.map_DY = null; playerController.invert_DY = false;					
#elif !UNITY_EDITOR && UNITY_STANDALONE_OSX
 				playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
                playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
                playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
				// d-pad and triggers are not analog and therefore not mapped as an axis
                playerController.map_LT = null; playerController.invert_LT = false;
                playerController.map_RT = null; playerController.invert_RT = false;	
				playerController.map_DX = null; playerController.invert_DX = false;
				playerController.map_DY = null; playerController.invert_DY = false;
#elif UNITY_EDITOR
				// in the editor we have to set on which platform we are working
				// different editor working environments are not covered by Unity's macros
				if (editorWorkPlatform == EditorWorkPlatform.MacOS) // MacOSX standard Bluetooth connection
				{
					playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
	                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
	                playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
	                playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
					// d-pad and triggers are not analog and therefore not mapped as an axis	
					playerController.map_LT = null; playerController.invert_LT = false;		
	                playerController.map_RT = null; playerController.invert_RT = false;		
					playerController.map_DX = null; playerController.invert_DX = false;
					playerController.map_DY = null; playerController.invert_DY = false;
				}
				else // this is for Windows7 using the MotionInJoy driver with custom settings
				{
					playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
               		playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
                	playerController.map_RX = string.Format("Joy{0} Axis 4", player); playerController.invert_RX = false;		// checked
               		playerController.map_RY = string.Format("Joy{0} Axis 5", player); playerController.invert_RY = true;		// checked
                	playerController.map_LT = string.Format("Joy{0} Axis 3", player); playerController.invert_LT = false;		// checked
                	playerController.map_RT = string.Format("Joy{0} Axis 6", player); playerController.invert_RT = false;		// checked	
					// the dpad is not analog and therefore not mapped as an axis	
					playerController.map_DX = null; playerController.invert_DX = false;
					playerController.map_DY = null; playerController.invert_DY = false;
				}
#endif
				break;
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
            case OuyaControllerType.TattieBogle: //this is the driver for the XBOX360 controller on MacOSX
                playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
                playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
                playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
                playerController.map_LT = string.Format("Joy{0} Axis 5", player); playerController.invert_LT = false;		// checked -1 > 1
                playerController.map_RT = string.Format("Joy{0} Axis 6", player); playerController.invert_RT = false;		// checked -1 > 1
                // the dpad is not analog and therefore not mapped as an axis
				playerController.map_DX = null; playerController.invert_DX = false;
				playerController.map_DY = null; playerController.invert_DY = false;
                break;
#endif
			case OuyaControllerType.Unknown: // we hope to catch any unkown bluetooth controllers
                playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;
                playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;
                playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;
                playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;
                playerController.map_DX = string.Format("Joy{0} Axis 5", player); playerController.invert_DX = false;
                playerController.map_DY = string.Format("Joy{0} Axis 6", player); playerController.invert_DY = true;
				playerController.map_LT = string.Format("Joy{0} Axis 7", player); playerController.invert_LT = false;
                playerController.map_RT = string.Format("Joy{0} Axis 8", player); playerController.invert_RT = false;
				break;
        }
	}
	#endregion
	
	/* -----------------------------------------------------------------------------------
	 * CONTROLLER ACCESS METHODS
	 */
	
	#region GENERAL CONTROLLER INFO
	
	public static bool GetControllerConnection() {
		/* returns if we have any controller connected
		 * helpful for switching input types (keypoard or gamepad) according to controller availability
		 */
		if (controllerNames == null || controllerNames.Length == 0) return false;
		else return true;
	}
	
	public static string GetControllerName(OuyaPlayer player) {
		/* returns the Unity name of the controller for a designated player
		 */
		int index = (int)player -1;
		if (controllerNames == null || index >= controllerCount) return "No controller found!";
		else return controllerNames[index];
	}
	
	public static OuyaControllerType GetControllerType(OuyaPlayer player) {
		/* returns the controller type (enum) for a designated player
		 */
		int index = (int)player -1;
		if (controllerCount == 0) return OuyaControllerType.None;
		else return controllerTypes[index];
	}
	
	#endregion
	
	#region AXIS STATE ACCESS
	
	public static float GetAxis(OuyaAxis axis, OuyaPlayer player) {
		/* for retreiving joystick axis values from mapped Unity Input
		 */
		
		/* NULL SECURITY */ 
		// check if there is no joystick connected
		if (controllerCount == 0) return 0f;
        
		// check if there have more players than controllers
		// we consider that the array position is starting at 0 for player 1
        int playerIndex = (int)player - 1;
        if (playerIndex < 0 || playerIndex >= controllerCount) return 0f;
		
		// finally check if we really found a controller for the player
        OuyaControllerType controllerType = controllerTypes[playerIndex];
	
		/* GET MAPPED AXIS NAME */ 
		// prepare fields for storing the results
        string axisName = null; bool invert = false;
		
		// get the controller mapping for the player
		PlayerController playerController = playerControllers[playerIndex];
		// secure that we have found a mapping
		if (playerController == null) return 0f;
		else
		{
			/* JOYSTICKS */ 
			// get the axis name for the player controller
			switch (axis) {
	        case OuyaAxis.LX: axisName = playerController.map_LX; invert = playerController.invert_LX; break;
	       	case OuyaAxis.LY: axisName = playerController.map_LY; invert = playerController.invert_LY; break;
	        case OuyaAxis.RX: axisName = playerController.map_RX; invert = playerController.invert_RX; break;
	       	case OuyaAxis.RY: axisName = playerController.map_RY; invert = playerController.invert_RY; break;
	       
			/* TRIGGERS & D-PAD */ 
			// the dpad and triggers are sometimes treated like an axis joystick
			// sometimes however it is just a set of buttons and the pressure senitivity is missing
			// this was observed for MacOS XBOX360 (TattieBogle) / Android Ouya / Android + MacOS PS3	
			/* LT-TRIGGER */
			case OuyaAxis.LT: axisName = playerController.map_LT; invert = playerController.invert_LT;
				// if the trigger is treated like a button we convert button press bool flags into axis values
				if (axisName == null)
				{
					switch (controllerType) {
					case OuyaControllerType.PS3:
						if (GetButton(8, ButtonAction.Pressed, player)) return 1f; else return 0f;
					}
				}
				break;
			/* RT-TRIGGER */
			case OuyaAxis.RT: axisName = playerController.map_RT; invert = playerController.invert_RT;
				// if the trigger is treated like a button we convert button press bool flags into axis values
				if (axisName == null)
				{
					switch (controllerType) {
					case OuyaControllerType.PS3:
						if (GetButton(9, ButtonAction.Pressed, player)) return 1f; else return 0f;
					}
				}
				break;
			/* DX-AXIS */
			case OuyaAxis.DX: axisName = playerController.map_DX; invert = playerController.invert_DX;
				// if the dpad is treated like a button we convert button press bool flags into axis values
				if (axisName == null)
				{
					switch (controllerType) {
#if !UNITY_EDITOR && UNITY_ANDROID
					case OuyaControllerType.Ouya:
						if (GetButton(10, ButtonAction.Pressed, player)) return -1f;
						else if (GetButton(11, ButtonAction.Pressed, player)) return 1f;
						break;
#endif
					case OuyaControllerType.PS3:
#if !UNITY_EDITOR && UNITY_ANDROID
						if (GetButton(7, ButtonAction.Pressed, player)) return -1f;
						else if (GetButton(5, ButtonAction.Pressed, player)) return 1f;
#elif !UNITY_EDITOR && UNITY_STANDALONE_OSX
						if (GetButton(7, ButtonAction.Pressed, player)) return -1f;
						else if (GetButton(5, ButtonAction.Pressed, player)) return 1f;
#elif !UNITY_EDITOR && UNITY_STANDALONE_WIN
						if (GetButton(16, ButtonAction.Pressed, player)) return -1f;
						else if (GetButton(14, ButtonAction.Pressed, player)) return 1f;
#elif UNITY_EDITOR
						// in the editor we have to set on which platform we are working
						// different editor working environments are not covered by Unity's macros
						if (editorWorkPlatform == EditorWorkPlatform.MacOS)
						{
							if (GetButton(7, ButtonAction.Pressed, player)) return -1f;
							else if (GetButton(5, ButtonAction.Pressed, player)) return 1f;
						}
						else {
							if (GetButton(16, ButtonAction.Pressed, player)) return -1f;
							else if (GetButton(14, ButtonAction.Pressed, player)) return 1f;
						}						
#endif
						break;	
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
					case OuyaControllerType.TattieBogle:
						if (GetButton(7, ButtonAction.Pressed, player)) return -1f;
						else if (GetButton(8, ButtonAction.Pressed, player)) return 1f;
						break;
#endif
					}
				} break;
			/* DY-AXIS */
	        case OuyaAxis.DY: axisName = playerController.map_DY; invert = playerController.invert_DY;
				// if the dpad is treated like a button we convert button press bool flags into axis values
				if (axisName == null)
				{
					switch (controllerType) {					
#if !UNITY_EDITOR && UNITY_ANDROID
					case OuyaControllerType.Ouya:
						if (GetButton(8, ButtonAction.Pressed, player)) return 1f;
						else if (GetButton(9, ButtonAction.Pressed, player)) return -1f;
						break;
#endif
					case OuyaControllerType.PS3:
#if !UNITY_EDITOR && UNITY_ANDROID
						if (GetButton(6, ButtonAction.Pressed, player)) return -1f;
						else if (GetButton(4, ButtonAction.Pressed, player)) return 1f;
#elif !UNITY_EDITOR && UNITY_STANDALONE_OSX
						if (GetButton(6, ButtonAction.Pressed, player)) return -1f;
						else if (GetButton(4, ButtonAction.Pressed, player)) return 1f;
#elif !UNITY_EDITOR && UNITY_STANDALONE_WIN
						if (GetButton(15, ButtonAction.Pressed, player)) return -1f;
						else if (GetButton(16, ButtonAction.Pressed, player)) return 1f;
#elif UNITY_EDITOR
						// in the editor we have to set on which platform we are working (Win or MacOS)
						// different editor working environments are not covered by Unity's macros
						if (editorWorkPlatform == EditorWorkPlatform.MacOS)
						{
							if (GetButton(6, ButtonAction.Pressed, player)) return -1f;
							else if (GetButton(4, ButtonAction.Pressed, player)) return 1f;
						}
						else {
							if (GetButton(15, ButtonAction.Pressed, player)) return -1f;
							else if (GetButton(16, ButtonAction.Pressed, player)) return 1f;
						}
#endif
						break;
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
					case OuyaControllerType.TattieBogle:
						if (GetButton(5, ButtonAction.Pressed, player)) return 1f;
						else if (GetButton(6, ButtonAction.Pressed, player)) return -1f;
						break;
#endif
					}
				} break;
	        
			default: return 0f;
			}
        }
		/* FINAL SECURITY CHECK */ 
		// we return 0 if we didn't find a valid axis
        if (axisName == null) return 0f;
		
		/* TRIGGER AXIS RANGE MAPPING */ 
		if (axis == OuyaAxis.LT || axis == OuyaAxis.RT) {
			// some trigger axis need to be remapped inrange before we can return the Unity Input
			if (invert) return playerController.rangeMapTriggerAxis(-Input.GetAxisRaw(axisName), axis);
        	else return playerController.rangeMapTriggerAxis(Input.GetAxisRaw(axisName), axis);
		}
		/* AXIS FLOAT RESULT FROM UNITY INPUT */ 
		if (invert) return -Input.GetAxisRaw(axisName);
        else return Input.GetAxisRaw(axisName);
    }
	#endregion
	
	#region BUTTON ACTION ACCESS
	
	public static bool GetButton(OuyaButton button,OuyaPlayer player) {
		/* this serves as the OUYA eguivalent to UNITY's Input.GetButton()
		 * returns true if the button is pressed
		 */
		// return the button state for pressed (down state)
		return GetButton(button, ButtonAction.Pressed, player);
	}
	
	public static bool GetButtonDown(OuyaButton button, OuyaPlayer player) {
		/* this serves as the OUYA eguivalent to UNITY's Input.GetButtonDown()
		 * returns true for the frame the button actually goes down
		 */
		// return the button state for the frame it goes down (down event)
		return GetButton(button, ButtonAction.DownFrame, player);
	}
	
	public static bool GetButtonUp(OuyaButton button, OuyaPlayer player) {
		/* this serves as the OUYA eguivalent to UNITY's Input.GetButtonUp()
		 * returns true for the frame the button actually goes up
		 */
		// return the button state for the frame it goes down (down event)
		return GetButton(button, ButtonAction.UpFrame, player);
	}
	#endregion
	
	/* -----------------------------------------------------------------------------------
	 * PRIVATE HELPER PROCEDURES
	 */
	
	#region PRIVATE HELPER PROCEDURES
	
	private static bool GetButton(OuyaButton button, ButtonAction buttonAction, OuyaPlayer player) {
		/* for retreiving joystick button values from mapped Unity Input
		 */
		
		/* NULL SECURITY */ 
		// check if there is no joystick connected
        if (controllerNames == null) return false;
        
		// check if there are more players than joysticks
		// this is not really needed in CLARK – just framework coherence
        int playerIndex = (int) player - 1;
        if (playerIndex >= controllerCount) return false;
		
		// finally check if we really found a joystick for the player
        OuyaControllerType controllerType = controllerTypes[playerIndex];
		
		// get the controller mapping for the player
		PlayerController playerController = playerControllers[playerIndex];
		
		// secure that we have found a mapping
		if (playerController != null) {
       
		/* FIND THE CORRECT MAPPED BUTTON KEY */ 
        	switch (controllerType) {
#if !UNITY_EDITOR && UNITY_ANDROID
        	case OuyaControllerType.Broadcom:
        	case OuyaControllerType.MogaPro:
				// this device was not tested yet
				// the setting were just extracted from some examples I found
				// please feedback if you find a way to test it
                switch (button)
				{
				// shoulder buttons
                case OuyaButton.LB:		return GetButton(6, buttonAction, player);
                case OuyaButton.RB:		return GetButton(7, buttonAction, player);
				
				// OUYA buttons
                case OuyaButton.O:		return GetButton(0, buttonAction, player);
                case OuyaButton.U:		return GetButton(3, buttonAction, player);
                case OuyaButton.Y:		return GetButton(4, buttonAction, player);
                case OuyaButton.A:		return GetButton(1, buttonAction, player);
					
				// stick buttons	
                case OuyaButton.L3:		return GetButton(13, buttonAction, player);
                case OuyaButton.R3:		return GetButton(14, buttonAction, player);
						
				// d-pad buttons and trigger buttons
				// these buttons are two axis and do not give out UP or DOWN events natively
				// we use button state management and continious scanning to provide these	
                case OuyaButton.DU: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DD: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DL: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DR: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.LT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);														
                case OuyaButton.RT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);														
			
				// not defined so far
				case OuyaButton.START: 	return false;
				case OuyaButton.SELECT: return false;
				case OuyaButton.SYSTEM:	return false;	
                default: return false;
                }
				break;

			case OuyaControllerType.GameStick:
				// tested on the real GameStick DevKit
				// never succeded in pairing the controller with the Ouya, Mac, Windows
				// strangely enough the flat d-pad has pressure sensitive axis output
				// triggers do not exist at all on this controller
				switch (button)
				{
				// OUYA buttons
				case OuyaButton.O: 		return GetButton(0, buttonAction, player);		// checked
                case OuyaButton.U: 		return GetButton(3, buttonAction, player);		// checked
                case OuyaButton.Y: 		return GetButton(4, buttonAction, player);		// checked
                case OuyaButton.A: 		return GetButton(1, buttonAction, player);		// checked
					
				// shoulder buttons	
                case OuyaButton.LB: 	return GetButton(6, buttonAction, player);		// checked
                case OuyaButton.RB: 	return GetButton(7, buttonAction, player);		// checked
                	
				// stick buttons	
                case OuyaButton.L3: 	return GetButton(13, buttonAction, player);		// checked
                case OuyaButton.R3: 	return GetButton(14, buttonAction, player);		// checked
					
				// center buttons	
				case OuyaButton.SELECT: return GetButton(27, buttonAction, player);		// checked			
                case OuyaButton.START: 	return GetButton(11, buttonAction, player);		// checked	
										
				// d-pad buttons
				// these buttons are two axis and do not give out UP or DOWN events natively
				// we use button state management and continious scanning to provide these	
                case OuyaButton.DU: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DD: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DL: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DR: return GetCachedButtonEvent(button, buttonAction, playerIndex);													
			
				// the GameStick has no triggers
				case OuyaButton.LT: return false;
				case OuyaButton.RT: return false;
					
				// SYSTEN is according to GameStick documents: joystick button 20
				// but this is not valid in Unity (see script reference Keycode)
                case OuyaButton.SYSTEM: return false;
				default: return false;
                }
            	break;

			case OuyaControllerType.Ouya:
				// tested on the real Ouya Developers Console
				// never succeded in pairing the controller with the Mac / Windows
				// the d-pad has no pressure sensitive output although the hardware looks like it
				// triggers have both: pressure sensitive axis and button event output (nice)
				switch (button)
				{
				// shoulder buttons
                case OuyaButton.LB: 	return GetButton(4, buttonAction, player);		// checked
                case OuyaButton.RB: 	return GetButton(5, buttonAction, player);		// checked
					
				// OUYA buttons	
                case OuyaButton.O:		return GetButton(0, buttonAction, player);		// checked
                case OuyaButton.U:		return GetButton(1, buttonAction, player);		// checked
                case OuyaButton.Y:		return GetButton(2, buttonAction, player);		// checked
                case OuyaButton.A:		return GetButton(3, buttonAction, player);		// checked
					
				// stick buttons
                case OuyaButton.L3:		return GetButton(6, buttonAction, player);		// checked
                case OuyaButton.R3:		return GetButton(7, buttonAction, player);		// checked
					
				// d-pad buttons
                case OuyaButton.DU:		return GetButton(8, buttonAction, player);		// checked
                case OuyaButton.DD:		return GetButton(9, buttonAction, player);		// checked
                case OuyaButton.DL:		return GetButton(10, buttonAction, player);		// checked
                case OuyaButton.DR:		return GetButton(11, buttonAction, player);		// checked
					
				// trigger buttons
                case OuyaButton.LT:		return GetButton(12, buttonAction, player);		// checked
                case OuyaButton.RT:		return GetButton(13, buttonAction, player);		// checked
					
				// not defined so far – or don't exist on OUYA
				case OuyaButton.START: return false;
				case OuyaButton.SYSTEM: return false;
				case OuyaButton.SELECT: return false;	
                default: return false;
                }
				break;
#endif
            case OuyaControllerType.XBox360:
#if !UNITY_EDITOR && UNITY_ANDROID
				// tested with the XBOX360 standard controller connected to the OUYA via USB
				// hopefully wireless XBOX controllers connected via Bluetooth have the same values
				// the d-pad has sensitive pressure axis output – however we won't get button events
				// we need to use continious input scanning for managing Buttonup or ButtonDown events
				// the same is true for the pressure sensitive axis triggers
				switch (button)
				{
				// OUYA buttons
            	case OuyaButton.O: 		return GetButton(0, buttonAction, player);		// checked
            	case OuyaButton.U: 		return GetButton(3, buttonAction, player);		// checked
            	case OuyaButton.Y: 		return GetButton(4, buttonAction, player);		// checked
           		case OuyaButton.A: 		return GetButton(1, buttonAction, player);		// checked
				
				// shoulder buttons	
            	case OuyaButton.LB:		return GetButton(6, buttonAction, player);		// checked
            	case OuyaButton.RB:		return GetButton(7, buttonAction, player);		// checked
				
				// center buttons
				case OuyaButton.START: 	return GetButton(11, buttonAction, player);		// checked
				case OuyaButton.SELECT:	return GetButton(27, buttonAction, player);
					
				// stick buttons
            	case OuyaButton.L3: 	return GetButton(13, buttonAction, player);		// checked
            	case OuyaButton.R3: 	return GetButton(14, buttonAction, player);		// checked
						
				// d-pad buttons and trigger buttons
				// these buttons are two axis and do not give out UP or DOWN events natively
				// we use button state management and continious scanning to provide these	
                case OuyaButton.DU: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DD: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DL: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DR: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.LT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);														
                case OuyaButton.RT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);														
							
				// not defined so far
				case OuyaButton.SYSTEM: return false;
				default: return false;
                }
#else
                switch (button)
				// tested with the XBOX360 standard controller connected to a Win64 machine via USB and official driver
				// hopefully wireless XBOX controllers connected via Bluetooth have the same values
				// the d-pad has sensitive pressure axis output – however we won't get button events
				// we need to use continious input scanning for managing Buttonup or ButtonDown events
				// the same is true for the pressure sensitive axis triggers
				// this block won't treat the XBOX360 controller running on MacOSX
				// on MacOSX we use the TattieBogle driver which leads to a different controller type
				{
				// OUYA buttons
				case OuyaButton.O:		return GetButton(0, buttonAction, player);		// checked
                case OuyaButton.U:		return GetButton(2, buttonAction, player);		// checked
                case OuyaButton.Y:		return GetButton(3, buttonAction, player);		// checked
                case OuyaButton.A:		return GetButton(1, buttonAction, player);		// checked
					
				// shoulder buttons
                case OuyaButton.LB:		return GetButton(4, buttonAction, player);		// checked
                case OuyaButton.RB:		return GetButton(5, buttonAction, player);		// checked
					
				// center buttons	
				case OuyaButton.START:	return GetButton(7, buttonAction, player);		// checked
				case OuyaButton.SELECT: return GetButton(6, buttonAction, player);		// checked
                
				// stick buttons
                case OuyaButton.L3: 	return GetButton(8, buttonAction, player);		// checked
                case OuyaButton.R3: 	return GetButton(9, buttonAction, player);		// checked
						
				// d-pad buttons and trigger buttons
				// these buttons are two axis and do not give out UP or DOWN events natively
				// we use button state management and continious scanning to provide these	
                case OuyaButton.DU: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DD: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DL: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DR: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.LT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);														
                case OuyaButton.RT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);														
			
				//  not defined so far
				case OuyaButton.SYSTEM: return false;
                default: return false;
                }
#endif
			case OuyaControllerType.PS3:
#if !UNITY_EDITOR && UNITY_ANDROID 
				// tested with the PS3 standard controller connected to the OUYA via Bluetooth
				// pairing was achieved using a temporary USB cable connection to the OUYA
				// the d-pad uses simple button events – there is no pressure sensitivity here
				// triggers have both: pressure sentive axis output as well as button events
				switch (button)
				{
               	// stick buttons
                case OuyaButton.L3: 	return GetButton(1, buttonAction, player);		// checked
                case OuyaButton.R3: 	return GetButton(2, buttonAction, player);		// checked
					
				// center buttons
				case OuyaButton.START: 	return GetButton(3, buttonAction, player);		// checked
				case OuyaButton.SELECT: return GetButton(27, buttonAction, player);		// checked
					
				// d-pad buttons	
                case OuyaButton.DU:		return GetButton(4, buttonAction, player);		// checked
				case OuyaButton.DR:		return GetButton(5, buttonAction, player);		// checked
                case OuyaButton.DD:		return GetButton(6, buttonAction, player);		// checked
                case OuyaButton.DL:		return GetButton(7, buttonAction, player);		// checked
				
				// trigger buttons
                case OuyaButton.LT: 	return GetButton(8, buttonAction, player);		// checked
                case OuyaButton.RT: 	return GetButton(9, buttonAction, player);		// checked
					
				// shoulder buttons
				case OuyaButton.LB:		return GetButton(10, buttonAction, player);		// checked
                case OuyaButton.RB:		return GetButton(11, buttonAction, player);		// checked
				
				// OUYA buttons
				case OuyaButton.O:		return GetButton(14, buttonAction, player);		// checked
				case OuyaButton.U:		return GetButton(15, buttonAction, player);		// checked
				case OuyaButton.Y:		return GetButton(12, buttonAction, player);		// checked
                case OuyaButton.A:		return GetButton(13, buttonAction, player);		// checked
					
				// not defined do far
				case OuyaButton.SYSTEM: return false;	
                default: return false;
                }
#elif !UNITY_EDITOR && UNITY_STANDALONE_OSX
				// tested with the PS3 standard controller connected to the MacOSX via Bluetooth
				// pairing was achieved using a temporary USB cable connection to the Mac
				// the d-pad and triggers use simple button events – there is no pressure sensitivity here
				// this is because the standard Mac connection doesn't show all the features of the controller
				// we would need a designated 3rd party driver for that
				switch (button)
				{
               	// stick buttons
                case OuyaButton.L3: 	return GetButton(1, buttonAction, player);		// checked
                case OuyaButton.R3: 	return GetButton(2, buttonAction, player);		// checked
					
				// center buttons
				case OuyaButton.START: 	return GetButton(3, buttonAction, player);		// checked
				case OuyaButton.SELECT: return GetButton(0, buttonAction, player);		// checked
					
				// d-pad buttons	
                case OuyaButton.DU:		return GetButton(4, buttonAction, player);		// checked
				case OuyaButton.DR:		return GetButton(5, buttonAction, player);		// checked
                case OuyaButton.DD:		return GetButton(6, buttonAction, player);		// checked
                case OuyaButton.DL:		return GetButton(7, buttonAction, player);		// checked
				
				// trigger buttons
                case OuyaButton.LT: 	return GetButton(8, buttonAction, player);		// checked
                case OuyaButton.RT: 	return GetButton(9, buttonAction, player);		// checked
					
				// shoulder buttons
				case OuyaButton.LB:		return GetButton(10, buttonAction, player);		// checked
                case OuyaButton.RB:		return GetButton(11, buttonAction, player);		// checked
				
				// OUYA buttons
				case OuyaButton.O:		return GetButton(14, buttonAction, player);		// checked
				case OuyaButton.U:		return GetButton(15, buttonAction, player);		// checked
				case OuyaButton.Y:		return GetButton(12, buttonAction, player);		// checked
                case OuyaButton.A:		return GetButton(13, buttonAction, player);		// checked
					
				// not defined do far
				case OuyaButton.SYSTEM: return false;	
                default: return false;
                }
#elif !UNITY_EDITOR && UNITY_STANDALONE_WIN
				// tested with the PS3 standard controller connected to Win7 64 via USB
				// custom setup was done using the most popular but crappy driver: MotionInJoy
				// this needs a CUSTOM button mapping setup in the driver tools to work (see documentation)
				// default sets could not be used as they do not make sense (gyro's and sticks share the same axis)
				// the d-pad use simple button events – there is no pressure sensitivity here
				// the triggers provide both: pressure sensitive axis output and button events
				// READ THE DOCUMENTATION to make this work !!!
				switch (button)
				{
				// OUYA buttons
				case OuyaButton.O:		return GetButton(2, buttonAction, player);		// checked
				case OuyaButton.U:		return GetButton(3, buttonAction, player);		// checked
				case OuyaButton.Y:		return GetButton(0, buttonAction, player);		// checked
                case OuyaButton.A:		return GetButton(1, buttonAction, player);		// checked
					
				// shoulder buttons
				case OuyaButton.LB:		return GetButton(4, buttonAction, player);		// checked
                case OuyaButton.RB:		return GetButton(5, buttonAction, player);		// checked	
					
				// trigger buttons
                case OuyaButton.LT: 	return GetButton(6, buttonAction, player);		// checked
                case OuyaButton.RT: 	return GetButton(7, buttonAction, player);		// checked
					
               	// stick buttons
                case OuyaButton.L3: 	return GetButton(8, buttonAction, player);		// checked
                case OuyaButton.R3: 	return GetButton(9, buttonAction, player);		// checked
					
				// center buttons
				case OuyaButton.SELECT: return GetButton(10, buttonAction, player);		// checked
				case OuyaButton.START: 	return GetButton(11, buttonAction, player);		// checked
				case OuyaButton.SYSTEM: return GetButton(12, buttonAction, player);		// checked
					
				// d-pad buttons	
                case OuyaButton.DU:		return GetButton(13, buttonAction, player);		// checked
				case OuyaButton.DR:		return GetButton(14, buttonAction, player);		// checked
                case OuyaButton.DD:		return GetButton(15, buttonAction, player);		// checked
                case OuyaButton.DL:		return GetButton(16, buttonAction, player);		// checked
					
				// not defined do far
                default: return false;
                }
#elif UNITY_EDITOR
				// the editor receives platform specific inputs which are not covered by the procompile macros
				// therefore the testing developer will have to set the Editor Working Platform
				// otherwise this code copies the data of the Windows and MacOSX platforms
				if (editorWorkPlatform == EditorWorkPlatform.MacOS) {
					// MacOSX via standard Bluetooth connection
					switch (button)
					{
	               	// stick buttons
	                case OuyaButton.L3: 	return GetButton(1, buttonAction, player);		// checked
	                case OuyaButton.R3: 	return GetButton(2, buttonAction, player);		// checked
						
					// center buttons
					case OuyaButton.START: 	return GetButton(3, buttonAction, player);		// checked
					case OuyaButton.SELECT: return GetButton(0, buttonAction, player);		// checked
						
					// d-pad buttons	
	                case OuyaButton.DU:		return GetButton(4, buttonAction, player);		// checked
					case OuyaButton.DR:		return GetButton(5, buttonAction, player);		// checked
	                case OuyaButton.DD:		return GetButton(6, buttonAction, player);		// checked
	                case OuyaButton.DL:		return GetButton(7, buttonAction, player);		// checked
					
					// trigger buttons
	                case OuyaButton.LT: 	return GetButton(8, buttonAction, player);		// checked
	                case OuyaButton.RT: 	return GetButton(9, buttonAction, player);		// checked
						
					// shoulder buttons
					case OuyaButton.LB:		return GetButton(10, buttonAction, player);		// checked
	                case OuyaButton.RB:		return GetButton(11, buttonAction, player);		// checked
					
					// OUYA buttons
					case OuyaButton.O:		return GetButton(14, buttonAction, player);		// checked
					case OuyaButton.U:		return GetButton(15, buttonAction, player);		// checked
					case OuyaButton.Y:		return GetButton(12, buttonAction, player);		// checked
	                case OuyaButton.A:		return GetButton(13, buttonAction, player);		// checked
						
					// not defined do far
					case OuyaButton.SYSTEM: return false;	
	                default: return false;
	                }
				} else {
					// Windows7 via USB and MotionInJoy driver
					switch (button)
					{
					// OUYA buttons
					case OuyaButton.O:		return GetButton(2, buttonAction, player);		// checked
					case OuyaButton.U:		return GetButton(3, buttonAction, player);		// checked
					case OuyaButton.Y:		return GetButton(0, buttonAction, player);		// checked
	                case OuyaButton.A:		return GetButton(1, buttonAction, player);		// checked
						
					// shoulder buttons
					case OuyaButton.LB:		return GetButton(4, buttonAction, player);		// checked
	                case OuyaButton.RB:		return GetButton(5, buttonAction, player);		// checked	
						
					// trigger buttons
	                case OuyaButton.LT: 	return GetButton(6, buttonAction, player);		// checked
	                case OuyaButton.RT: 	return GetButton(7, buttonAction, player);		// checked
						
	               	// stick buttons
	                case OuyaButton.L3: 	return GetButton(8, buttonAction, player);		// checked
	                case OuyaButton.R3: 	return GetButton(9, buttonAction, player);		// checked
						
					// center buttons
					case OuyaButton.SELECT: return GetButton(10, buttonAction, player);		// checked
					case OuyaButton.START: 	return GetButton(11, buttonAction, player);		// checked
					case OuyaButton.SYSTEM: return GetButton(12, buttonAction, player);		// checked
						
					// d-pad buttons	
	                case OuyaButton.DU:		return GetButton(13, buttonAction, player);		// checked
					case OuyaButton.DR:		return GetButton(14, buttonAction, player);		// checked
	                case OuyaButton.DD:		return GetButton(15, buttonAction, player);		// checked
	                case OuyaButton.DL:		return GetButton(16, buttonAction, player);		// checked
						
	                default: return false;
					}
				}
#endif
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
			case OuyaControllerType.TattieBogle:
				// this is for the XBOX360 standard controller running on MacOSX using the TattieBogle driver
				// hopefully wireless XBOX controllers connected via Bluetooth have the same values
				// the d-pad has no pressure sensitivity but gives us button events
				// triggers provide only pressure sensitive axis output therefore
				// we need to use continious input scanning for managing Buttonup or ButtonDown events
                switch (button)
				{
				// shoulder buttons
                case OuyaButton.LB: 		return GetButton(13, buttonAction, player);		// checked
                case OuyaButton.RB: 		return GetButton(14, buttonAction, player);		// checked
					
				// OUYA buttons
                case OuyaButton.O: 			return GetButton(16, buttonAction, player);		// checked
                case OuyaButton.U:			return GetButton(18, buttonAction, player);		// checked
                case OuyaButton.Y: 			return GetButton(19, buttonAction, player);		// checked
                case OuyaButton.A: 			return GetButton(17, buttonAction, player);		// checked
				
				// stick buttons
                case OuyaButton.L3:			return GetButton(11, buttonAction, player);		// checked
                case OuyaButton.R3:			return GetButton(12, buttonAction, player);		// checked
				
				// center buttons
				case OuyaButton.SELECT:		return GetButton(10, buttonAction, player);		// checked
                case OuyaButton.START:		return GetButton(9, buttonAction, player);		// checked
                case OuyaButton.SYSTEM:		return GetButton(15, buttonAction, player);		// checked
               	
				// d-pad buttons
				case OuyaButton.DU:			return GetButton(5, buttonAction, player);		// checked
                case OuyaButton.DD:			return GetButton(6, buttonAction, player);		// checked
                case OuyaButton.DL:			return GetButton(7, buttonAction, player);		// checked
                case OuyaButton.DR:			return GetButton(8, buttonAction, player);		// checked
					
				// trigger buttons
				// the triggers are axis and do not give out UP or DOWN events natively
				// we use button state management and continious scanning to provide these
                case OuyaButton.LT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);	
                case OuyaButton.RT: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                default: return false;
                }
#endif
       		case OuyaControllerType.Unknown:
#if !UNITY_EDITOR && UNITY_ANDROID
				// we hope to catch any unkown bluetooth controllers on Android here (wild card)
				// there can't be any testing for that as it's just a random try
				switch (button)
				{
				// ouya buttons
                case OuyaButton.O:		return GetButton(0, buttonAction, player);
                case OuyaButton.U:		return GetButton(3, buttonAction, player);
                case OuyaButton.Y:		return GetButton(4, buttonAction, player);
                case OuyaButton.A:		return GetButton(1, buttonAction, player);
					
				// shoulder buttons
                case OuyaButton.LB:		return GetButton(6, buttonAction, player);
                case OuyaButton.RB:		return GetButton(7, buttonAction, player);
					
				// stick buttons
                case OuyaButton.L3:		return GetButton(13, buttonAction, player);
                case OuyaButton.R3:		return GetButton(14, buttonAction, player);
					
				// d-pad buttons and trigger buttons
				// tese buttons are axis and do not give out UP or DOWN events natively
				// we use button state management and continious scanning to provide these	
                case OuyaButton.DU: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DD: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DL: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.DR: return GetCachedButtonEvent(button, buttonAction, playerIndex);
                case OuyaButton.LT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);														
                case OuyaButton.RT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);														
				
				// not defined so far
                default: return false;
                }
#endif
				break;
			}
		}
		/* SECURITY FALL THROUGH RETURN */ 
        return false;
    }
	
	private static bool GetCachedButtonEvent(OuyaButton button, ButtonAction buttonAction, int playerIndex) {
		/* this allows to retreive button events discovered by continious scanning
		 * this will sreturn false for ButtonUp and ButtonDown if continioussScanning is not activated
		 */
		// get the correct player from the index
		OuyaPlayer player = (OuyaPlayer)(playerIndex + 1);
		
		switch (button) {
		// d-pad buttons
		// some d-pad buttons are two axis and do not give out UP or DOWN events natively
		// we use button state management and continious scanning to provide these
        case OuyaButton.DU:
			switch (buttonAction) {
			case ButtonAction.DownFrame: return playerControllers[playerIndex].downEventDU;
			case ButtonAction.UpFrame: return playerControllers[playerIndex].upEventDU;
			default: return GetAxis(OuyaAxis.DY, player) > 0f;
			}
        case OuyaButton.DD:
			switch (buttonAction) {
			case ButtonAction.DownFrame: return playerControllers[playerIndex].downEventDD;
			case ButtonAction.UpFrame: return playerControllers[playerIndex].upEventDD;
			default: return GetAxis(OuyaAxis.DY, player) < 0f;
			}
        case OuyaButton.DL:
			switch (buttonAction) {
			case ButtonAction.DownFrame: return playerControllers[playerIndex].downEventDL;
			case ButtonAction.UpFrame: return playerControllers[playerIndex].upEventDL;
			default: return GetAxis(OuyaAxis.DX, player) < 0f;
			}
        case OuyaButton.DR:
			switch (buttonAction) {
			case ButtonAction.DownFrame: return playerControllers[playerIndex].downEventDR;
			case ButtonAction.UpFrame: return playerControllers[playerIndex].upEventDR;
			default: return GetAxis(OuyaAxis.DX, player) > 0f;		
			}
		// trigger buttons
		// some triggers are axis and do not give out UP or DOWN events natively
		// we use button state management and continious scanning to provide these
        case OuyaButton.LT:															
			switch (buttonAction) {
			case ButtonAction.DownFrame: return playerControllers[playerIndex].downEventLT;
			case ButtonAction.UpFrame: return playerControllers[playerIndex].upEventLT;
			default: return GetAxis(OuyaAxis.LT, player) > triggerThreshold;	
			}
        case OuyaButton.RT:															
			switch (buttonAction) {
			case ButtonAction.DownFrame: return playerControllers[playerIndex].downEventRT;
			case ButtonAction.UpFrame: return playerControllers[playerIndex].upEventRT;
			default: return GetAxis(OuyaAxis.RT, player) > triggerThreshold;	
			}
			// not defined so far
        default: return false;
        }
	}
	
	private static bool GetButton(int buttonNum, ButtonAction action, OuyaPlayer player) {
		/* gets Unity Input from Ouya key codes derived from plaer and button number (0-19)
		 */
		// get the correct Ouya key code
		int key = GetOuyaKeyCode(buttonNum, player);
		
		// return the mapped Unity input for the correct button action
		switch (action) {
		case ButtonAction.Pressed: return Input.GetKey((KeyCode)key);
		case ButtonAction.DownFrame: return Input.GetKeyDown((KeyCode)key);
		case ButtonAction.UpFrame: return Input.GetKeyUp((KeyCode)key);
		default: return Input.GetKey((KeyCode)key);
		}    
    }
	
	private static int GetOuyaKeyCode(int buttonNum, OuyaPlayer player) {
		/* calculates the OuyKeyCodefor a given player and button number
		 * depends on the stability of the int chosen in OuyaKeyCodes
		 * the advantage is that is much faster than parsing for joystick names
		 */
		// the buttons numbers 0 to 19 map to player dependent joystick buttons
		if (buttonNum < 20) {
			// the calculations derive from the pattern of the integers assigned in OuyaKeyCodes
			// joystick buttons start at 330 with player = 0 (None)
			// every player has 20 keys
			return (330 + buttonNum + ((int)player * 20));
		}
		// othet buttons map directly to keys
		else return buttonNum;
	}
	#endregion
	
	#region CONVENIENECE JOYSTICK ACCESS
	
	/* -----------------------------------------------------------------------------------
	 * CONVINIENCE JOYSTICK ACCESS
	 */
	
	public static void SetDeadzone(DeadzoneType type, float radius) {
		/* allows to define a deadzone which will be used by convenience acces methods
		 * if not set otherwise the default deadzone uses
		 * type: circular clipped, radius: 0.25
		 */
		deadzoneType = type; deadzoneRadius = radius;
	}
	
	public static void SetTriggerTreshold(float treshold) {
		/* allows to adjust the trigger threshold
		 * this is only needed if the "Dead" values in the Input Manager Settings were set to 0.
		 * default is 0.1f
		 */
		triggerThreshold = treshold;
	}
	
	public static Vector2 GetJoystick(OuyaJoystick joystick, OuyaPlayer player) {
		/* allows to easily get the input of a joystick
		 * the input will already be checked by a preset deadzone
		 */
		switch (joystick) {
		case OuyaJoystick.LeftStick:
			switch (deadzoneType) {
			case DeadzoneType.AxialClip:
				return CheckDeadzoneAxial(GetAxis(OuyaAxis.LX, player), GetAxis(OuyaAxis.LY, player), deadzoneRadius);
			case DeadzoneType.CircularClip:
				return CheckDeadzoneCircular(GetAxis(OuyaAxis.LX, player), GetAxis(OuyaAxis.LY, player), deadzoneRadius);
			case DeadzoneType.CircularMap:
				return CheckDeadzoneRescaled(GetAxis(OuyaAxis.LX, player), GetAxis(OuyaAxis.LY, player), deadzoneRadius);
			default:
				return Vector2.zero;
			} 
		case OuyaJoystick.RightStick:
			switch (deadzoneType) {
			case DeadzoneType.AxialClip:
				return CheckDeadzoneAxial(GetAxis(OuyaAxis.RX, player), GetAxis(OuyaAxis.RY, player), deadzoneRadius);
			case DeadzoneType.CircularClip:
				return CheckDeadzoneCircular(GetAxis(OuyaAxis.RX, player), GetAxis(OuyaAxis.RY, player), deadzoneRadius);
			case DeadzoneType.CircularMap:
				return CheckDeadzoneRescaled(GetAxis(OuyaAxis.RX, player), GetAxis(OuyaAxis.RY, player), deadzoneRadius);
			default:
				return Vector2.zero;
			} 
		case OuyaJoystick.DPad:
			switch (deadzoneType) {
			case DeadzoneType.AxialClip:
				return CheckDeadzoneAxial(GetAxis(OuyaAxis.DX, player), GetAxis(OuyaAxis.DY, player), deadzoneRadius);
			case DeadzoneType.CircularClip:
				return CheckDeadzoneCircular(GetAxis(OuyaAxis.DX, player), GetAxis(OuyaAxis.DY, player), deadzoneRadius);
			case DeadzoneType.CircularMap:
				Vector2 dpadInput = CheckDeadzoneRescaled(GetAxis(OuyaAxis.DX, player), GetAxis(OuyaAxis.DY, player), deadzoneRadius);
				if (dpadInput.x > 1f) dpadInput.x = 1; else if (dpadInput.x < -1) dpadInput.x = -1;
				if (dpadInput.y > 1f) dpadInput.y = 1; else if (dpadInput.y < -1) dpadInput.y = -1;
				return dpadInput;
			default:
				return Vector2.zero;
			} 
		default:
			return Vector2.zero;
		}
	}
	
	public static float GetJoystickAngle(OuyaJoystick joystick, OuyaPlayer player) {
		/* returns the angle of a joystick
		 * This is a convenience method allowing to get the joystick input and
		 * calculate the angle at the same call
		 * angles start at the positive joystick-x-axis and then increase conterclockwise
		 * (x-positive-axis is 0° / y-positive axis is 90° / x-negative axis is 180° / y-negative-axis is 270°)
		 * no joystrick input leads to return of -1 (as 0 is a real value used for the x-positive input)
		 */
		return CalculateJoystickAngle(GetJoystick(joystick, player));
	}
	
	public static float GetTrigger(OuyaTrigger trigger, OuyaPlayer player) {
		/* returns the trigger axis value after clipping by trigger deadzone
		 * this is needed if the "Dead" value in the Input Manager Settings were set to 0
		 * it allows allows to get high precision trigger input with deadzone remapping
		 */
		// create a field to store the results
		float triggerInput;
		
		// get the raw trigger input
		switch (trigger) {
		case OuyaTrigger.Left: triggerInput = GetAxis(OuyaAxis.LT, player); break;
		case OuyaTrigger.Right: triggerInput = GetAxis(OuyaAxis.RT, player); break;
		default: triggerInput = 0f; break;
		}
		// check for deadzone clipping
		if (triggerInput < triggerThreshold) return 0f;
		else {
			switch (deadzoneType) {
			case DeadzoneType.AxialClip: return triggerInput;
			case DeadzoneType.CircularClip: return triggerInput;
			case DeadzoneType.CircularMap:
				// remap the values to allow full range input
				return (triggerInput - triggerThreshold) / (1f - triggerThreshold);
			}
		}
		// we should never arrive here
		return 0f;
	}
	#endregion
	
	#region DEADZONE HELPERS
	
	/* -----------------------------------------------------------------------------------
	 * DEADZONE HELPERS
	 */
	
	public static Vector2 CheckDeadzoneAxial(float xAxis, float yAxis, float deadzone) {
		/* returns an input vector where each axis value inside the deadzone get clipped
		 * this leads to a cross shaped deadzone area
		 */
		// create a vector from the axis values
		Vector2 stickInput = new Vector2(xAxis, yAxis);
		
		// clip each axis value independently if in deadzone
		if(Mathf.Abs(stickInput.x) < deadzone) stickInput.x = 0.0f;
		if(Mathf.Abs(stickInput.y) < deadzone) stickInput.y = 0.0f;
		
		return stickInput;
	}
	
	
	public static Vector2 CheckDeadzoneCircular(float xAxis, float yAxis, float deadRadius) {
		/* returns an input vector where values inside the circular deadzone get clipped
		 * not as smooth as CheckDeadzoneRescaled() but better performance
		 */
		// create a vector from the axis values
		Vector2 stickInput = new Vector2(xAxis, yAxis);
		
		// check against the square deadzone radius (no sqrt >> more performance)
		if(((xAxis * xAxis) + (yAxis * yAxis)) < (deadRadius * deadRadius)) {
			stickInput = Vector2.zero;
		}
		return stickInput;
	}
	
	
	public static Vector2 CheckDeadzoneRescaled(float xAxis, float yAxis, float deadRadius) {
		/* returns an input vector with clipped circular deadzone
		 * values outside the deadzone will be remapped
		 * gives the smoothest input results (no sudden value jump on the deadzone border)
		 * costs a bit more performance
		*/
		// create a vector from the axis values
		Vector2 stickInput = new Vector2(xAxis, yAxis);
		// calculate its length
		float inputMagnitude = stickInput.magnitude;
 		
		// check if the input is inside the deadzone
		if (inputMagnitude < deadRadius) return Vector2.zero;
		else {
		// rescale the clipped input vector into the non-dead zone space
		stickInput *= (inputMagnitude - deadRadius) / ((1f - deadRadius) * inputMagnitude);
		}
		return stickInput;
	}
	#endregion
	
	#region UTILITY HELPERS
	
	/* -----------------------------------------------------------------------------------
	 * UTILITY HELPERS
	 */
	
	public static float CalculateJoystickAngle(Vector2 joystickInput) {
		/* returns the angle of the called joystick
		 * angles start at the positive joystick-x-axis and then increase conterclockwise
		 * (x-positive-axis is 0° / y-positive axis is 90° / x-negative axis is 180° / y-negative-axis is 270°)
		 * no joystrick input leads to return of -1 (as 0 is a real value used for the x-positive input)
		 */
		// check the quadrant our joystick is in
		// we need that to do the correct ploar conversion math
		Quadrant quad = Quadrant.I;
		if (joystickInput.x < 0 && joystickInput.y > 0) quad = Quadrant.II;
		else if (joystickInput.x < 0 && joystickInput.y < 0) quad = Quadrant.III;
		else if (joystickInput.x > 0 && joystickInput.y < 0) quad = Quadrant.IV;
		else if (joystickInput.x == 0 && joystickInput.y > 0) quad = Quadrant.OnYPos;
		else if (joystickInput.x > 0 && joystickInput.y == 0) quad = Quadrant.OnXPos;
		else if (joystickInput.x == 0 && joystickInput.y < 0) quad = Quadrant.OnYNeg;
		else if (joystickInput.x < 0 && joystickInput.y == 0) quad = Quadrant.OnXNeg;
		else if (joystickInput.x == 0 && joystickInput.y == 0) quad = Quadrant.Zero;
		
		// now we can do the poloar conversion according to quadrant cases
		float angle = 0f;
		switch (quad) {
		case Quadrant.I:
			angle = Mathf.Atan2(joystickInput.y, joystickInput.x) * (180f / Mathf.PI); break;
		case Quadrant.II:
			angle = Mathf.Atan2(joystickInput.y, joystickInput.x) * (180f / Mathf.PI); break;
		case Quadrant.III:
			angle = (Mathf.Atan2(joystickInput.y, joystickInput.x) * (180f / Mathf.PI)) + 360; break;
		case Quadrant.IV:
			angle = (Mathf.Atan2(joystickInput.y, joystickInput.x) * (180f / Mathf.PI)) + 360; break;
		case Quadrant.OnXPos:
			angle = 0f; break;
		case Quadrant.OnXNeg:
			angle = 180f; break;
		case Quadrant.OnYPos:
			angle = 90f; break;
		case Quadrant.OnYNeg:
			angle = 270f; break;
		case Quadrant.Zero:
			angle = -1f; break;
		}
		return angle;
	}
	#endregion
}

