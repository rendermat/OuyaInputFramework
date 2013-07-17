/*
 * Copyright (C) 2013 GoldenTricycle, GBR.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/* Version 0.07 */

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
public enum OuyaMapType {
	GameStick_CONSOLE, Ouya_CONSOLE,
	Generic_ANDROID, Broadcom_ANDROID, MogaPro_ANDROID,
	PS3_ANDROID, XBOX360_ANDROID_wireless, XBOX360_ANDROID,
	Ouya_WIN, GameStick_WIN, PS3_WIN, XBox360_WIN, MotionInJoy_WIN,
	GameStick_OSX, PS3_OSX, TattieBogle_OSX, Afterglow_OSX,
	Unknown, None}
// defining deadzone types
public enum DeadzoneType {AxialClip, CircularClip, CircularMap}


public static class OuyaInput
{
	/* PROPERTIES */ 
	// adjust this value to set the check interval (consider performance)
	// this is just for checking if a controller was unplugged or added
	private const int playersMax = 10;

	// setting for platform specific djustments
	private static float plugCheckInterval = 3;
	private static bool scanContinuously = true;
	private static float deadzoneRadius = 0.25f;
	private static float triggerThreshold = 0.10f;

	private static Platform platform = Platform.MacOS;
	private static DeadzoneType deadzoneType = DeadzoneType.CircularClip;

	// this flag disables the d-pad for XBOX360 wireless controllers on Android
	// the crappy driver sends the d-pad input on the same channels as the action buttons
	// therefore a d-pad press would lead to an action button input and vice versa
	// so only clean solution to that is to deactivate the d-pad altogether
	// setting this to true allows d-pad input with the issues mentioned above
	public static bool XBOX360W_ANDROID_DPAD = false;
	private static bool secureSinglePlayerMap = true;
	private static AndroidBluetoothDevice BT_HID_Device = AndroidBluetoothDevice.MogaPro;

	/* TEMPORARY */ 
	// the time of the last joystick lis update
	private static float lastPlugCheckTime = -1;
	// a list of all available controller names
	private static string[] controllerNames = null;
	// a list of all available controller types derived from names
	private static OuyaMapType[] mapTypes = null;
	// a list of active controller mapping containers
	private static PlayerController[] playerControllers = null;
	// counting all connected controllers
	private static int controllerCount = 0;

	/* PRIVATE ENUM DATA TYPES */ 
	// defining test platform types
	private enum Platform {MacOS, Windows, Android, iOS}
	// defining angular quadrants of joystick input
	private enum Quadrant {I, II, III, IV, OnXPos, OnXNeg, OnYPos, OnYNeg, Zero}
	// defining generic Android Bluetoth HID devices
	private enum AndroidBluetoothDevice {Ouya, MogaPro}


	/* -----------------------------------------------------------------------------------
	 * INITIALIZATION
	 */

	static OuyaInput() {
		/* static constructor
		 */
		// set the editor work platform
		switch (Application.platform) {
			case RuntimePlatform.OSXPlayer: platform = Platform.MacOS; break;
			case RuntimePlatform.OSXEditor: platform = Platform.MacOS; break;
			case RuntimePlatform.WindowsPlayer: platform = Platform.Windows; break;
			case RuntimePlatform.WindowsEditor: platform = Platform.Windows; break;
			case RuntimePlatform.Android: platform = Platform.Android; break;
			case RuntimePlatform.IPhonePlayer: platform = Platform.iOS; break;
		}
		// create an array of classes caching input mapping strings
		playerControllers = new PlayerController[playersMax];
		// create an array for storing the controller map types
		mapTypes = new OuyaMapType[playersMax];
		// initialize every field of that array
		for (int i = 0; i < playersMax; i++) {
			mapTypes[i] = OuyaMapType.None;
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
		public OuyaMapType mapType;
		public bool mapConfirmed = false;

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


		public PlayerController(OuyaPlayer player, OuyaMapType controllerType) {
			/* constructor
			 */
			this.mapType = controllerType;
			this.player = player;
		}

		public bool checkMapType(OuyaMapType againstType) {
			/* returns true if the controller type equals that of the given one
			 */
			if (againstType == mapType) return true;
			else return false;
		}

		public void setController(OuyaPlayer player, OuyaMapType mapType) {
			/* allows to reinitialize the ID of an existing controller mapping
			 */
			this.mapType = mapType;
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
			switch (mapType)
			{
#if !UNITY_EDITOR && UNITY_ANDROID
				case OuyaMapType.Generic_ANDROID:
				case OuyaMapType.Broadcom_ANDROID:
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
#endif

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
				case OuyaMapType.Ouya_WIN:
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
#endif

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
				case OuyaMapType.TattieBogle_OSX:
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
#endif

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

	public static void SetContinuousScanning(bool active) {
		/* allows to activate continious controller scanning
		 * this is needed to retreive ButtonUp and BottonDown events for triggers and d-pads
		 */
		scanContinuously = active;
	}

	public static void SetPlugCheckInterval(float seconds) {
		/* allows to adjust the interval for controller plug checks
		 * the smaller the interval is the earlier new controllers get noticed
		 */
		plugCheckInterval = seconds;
	}

	public static void SetSinglePlayerSecure(bool secure) {
		/* this allows to enable a secure check routine which prevents single player input channel shifts
		 * such shifts happen when some controllers are hot-plugged and show LEDs for being not the first player
		 * this issue was observed with the XBOX360 controller on various platforms
		 */
		secureSinglePlayerMap = secure;
	}

	public static void UpdateControllers() {
		/* method to check if joystick where plugged or unplugged
		 * updates the list of joysticks
		 * this should be called at Start() and in Update()
		 */
		// we only do a joystick plug check every 3 seconds
		if (((Time.time - lastPlugCheckTime) > plugCheckInterval)
		    || (lastPlugCheckTime < 0)) {
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
					// set the controller map type for each controller name
					mapTypes[i] = findMapType(i, controllerNames[i]);

					/* (RE)USE EXISTING MAPPING */ 
					// check if we already have a map for this controller
					if (playerControllers[i] != null)
					{
						// if we have a map we make sure that it is still of the same controller type
						if (!playerControllers[i].checkMapType(mapTypes[i])) {
							// reinitialize the type of the existing mapping if needed
							playerControllers[i].setController((OuyaPlayer)(i+1), mapTypes[i]);
							// redo the mapping in the existing container for the new controller
							mapController(playerControllers[i]);
							Input.ResetInputAxes();
						}
					}
					/* CREATE NEW MAPPING */ 
					else {
						// if not we create a new one
						playerControllers[i] = new  PlayerController((OuyaPlayer)(i+1), mapTypes[i]);
						// set the mappings for the new controller
						mapController(playerControllers[i]);
						Input.ResetInputAxes();
					}
				}
				// we reset all the controller map type fields that are not used anymore
				for (int i = controllerCount; i < playersMax; i++) {
					mapTypes[i] = OuyaMapType.None;
				}
			}
			/* CLEAR CONTROLLER MAPS */
			else {
				// if we found no controller names we reset the counter
				controllerCount = 0;

				for (int i = 0; i < playersMax; i++) {
					// we reset all controller map types in the array
					mapTypes[i] = OuyaMapType.None;
					// we also clear all mappings that are not used anymore
					playerControllers[i] = null;
				}
			}
		}
		/* CONTINIOUS JOYSTICK LIST */
		// this block is a state manager that allows to get button events for native axes buttons
		if (scanContinuously && controllerCount > 0)
		{
			// scan controllers to gather button down or up events for triggers
			for (int i = 0; i < controllerCount; i++) {
				playerControllers[i].scanController();
			}
		}
		/* SINGLE PLAYER SECURE MAP */ 
		// scan check if the player has a switched channel
		if (secureSinglePlayerMap
		    && playerControllers[0] != null
		    && !playerControllers[0].mapConfirmed)
		{
			// switch mapping player channals until we got a confirmed map
			playerControllers[0].mapConfirmed = SwitchSinglePlayerMap();
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

	private static OuyaMapType findMapType(int index, string controllerName) {
		/* a helper for the updateJoystickList() method
		 * converts controller string names into enum descriptions
		 */
		switch (platform) {
			case Platform.Android:
			switch (controllerName.ToUpper()) {
				case "BT HID":
				switch (BT_HID_Device) {
					case AndroidBluetoothDevice.Ouya:
					mapTypes[index] = OuyaMapType.Generic_ANDROID;
					return OuyaMapType.Generic_ANDROID;
					case AndroidBluetoothDevice.MogaPro:
					mapTypes[index] = OuyaMapType.MogaPro_ANDROID;
					return OuyaMapType.MogaPro_ANDROID;
					default:
					mapTypes[index] = OuyaMapType.Generic_ANDROID;
					return OuyaMapType.Generic_ANDROID;
				}
				case "BROADCOM BLUETOOTH HID":
				mapTypes[index] = OuyaMapType.Broadcom_ANDROID;
				return OuyaMapType.Broadcom_ANDROID;
				case "MOGA PRO HID":
				mapTypes[index] = OuyaMapType.MogaPro_ANDROID;
				return OuyaMapType.MogaPro_ANDROID;
				case "OUYA GAME CONTROLLER":
				mapTypes[index] = OuyaMapType.Ouya_CONSOLE;
				return OuyaMapType.Ouya_CONSOLE;
				case "GAMESTICK CONTROLLER 1":
				case "GAMESTICK CONTROLLER 2": 
				case "GAMESTICK CONTROLLER 3":
				case "GAMESTICK CONTROLLER 4":
				case "BERWAY TECH LTD GAMESTICK CONTROLLER":
				mapTypes[index] = OuyaMapType.GameStick_CONSOLE;
				return OuyaMapType.GameStick_CONSOLE;
				case "SONY PLAYSTATION(R)3 CONTROLLER":
				case "PLAYSTATION(R)3 CONTROLLER":
				mapTypes[index] = OuyaMapType.PS3_ANDROID;
				return OuyaMapType.PS3_ANDROID;
				case "XBOX 360 WIRELESS RECEIVER":
				case "CONTROLLER (XBOX 360 WIRELESS RECEIVER FOR WINDOWS)":
				case "MICROSOFT WIRELESS 360 CONTROLLER":
				mapTypes[index] = OuyaMapType.XBOX360_ANDROID_wireless;
				return OuyaMapType.XBOX360_ANDROID_wireless;
				case "CONTROLLER (AFTERGLOW GAMEPAD FOR XBOX 360)":
				case "CONTROLLER (ROCK CANDY GAMEPAD FOR XBOX 360)":
				case "MICROSOFT X-BOX 360 PAD":
				case "CONTROLLER (XBOX 360 FOR WINDOWS)":
				case "CONTROLLER (XBOX360 GAMEPAD)":
				case "XBOX 360 FOR WINDOWS (CONTROLLER)":
				mapTypes[index] = OuyaMapType.XBOX360_ANDROID;
				return OuyaMapType.XBOX360_ANDROID;
				case "":
				mapTypes[index] = OuyaMapType.Unknown;
				return OuyaMapType.Unknown;
			}
			break;
			case Platform.Windows:
			switch (controllerName.ToUpper()) {
				case "OUYA GAME CONTROLLER":
				mapTypes[index] = OuyaMapType.Ouya_WIN;
				return OuyaMapType.Ouya_WIN;
				case "MOTIONINJOY VIRTUAL GAME CONTROLLER":
				mapTypes[index] = OuyaMapType.MotionInJoy_WIN;
				return OuyaMapType.MotionInJoy_WIN;	
				case "SONY PLAYSTATION(R)3 CONTROLLER":
				case "PLAYSTATION(R)3 CONTROLLER":
				mapTypes[index] = OuyaMapType.PS3_WIN;
				return OuyaMapType.PS3_WIN;
				case "MICROSOFT WIRELESS 360 CONTROLLER":
				case "XBOX 360 WIRELESS RECEIVER":
				case "CONTROLLER (AFTERGLOW GAMEPAD FOR XBOX 360)":
				case "CONTROLLER (ROCK CANDY GAMEPAD FOR XBOX 360)":
				case "CONTROLLER (XBOX 360 WIRELESS RECEIVER FOR WINDOWS)":
				case "MICROSOFT X-BOX 360 PAD":
				case "CONTROLLER (XBOX 360 FOR WINDOWS)":
				case "CONTROLLER (XBOX360 GAMEPAD)":
				case "XBOX 360 FOR WINDOWS (CONTROLLER)":
				mapTypes[index] = OuyaMapType.XBox360_WIN;
				return OuyaMapType.XBox360_WIN;
				case "":
				mapTypes[index] = OuyaMapType.Unknown;
				return OuyaMapType.Unknown;
				case "BERWAY TECH LTD GAMESTICK CONTROLLER":
				mapTypes[index] = OuyaMapType.GameStick_WIN;
				return OuyaMapType.GameStick_WIN;
			}
			break;
			case Platform.MacOS:
			switch (controllerName.ToUpper()) {
				case "":
				case "PERFORMANCE DESIGNED PRODUCTS AFTERGLOW GAMEPAD FOR XBOX 360":
				case "MICROSOFT WIRELESS 360 CONTROLLER":
				mapTypes[index] = OuyaMapType.TattieBogle_OSX;
				return OuyaMapType.TattieBogle_OSX;
				case "SONY PLAYSTATION(R)3 CONTROLLER":
				case "PLAYSTATION(R)3 CONTROLLER":
				mapTypes[index] = OuyaMapType.PS3_OSX;
				return OuyaMapType.PS3_OSX;
				case "AFTERGLOW WIRED USB XBOX360 CONTROLLER":
				case "BERWAY TECH LTD GAMESTICK CONTROLLER":
				mapTypes[index] = OuyaMapType.GameStick_OSX;
				return OuyaMapType.GameStick_OSX;
			}
			break;
			case Platform.iOS:
			mapTypes[index] = OuyaMapType.Unknown;
			return OuyaMapType.Unknown;
		}
		// security fall through
		mapTypes[index] = OuyaMapType.Unknown;
		return OuyaMapType.Unknown;		
	}

	private static void mapController(PlayerController playerController) {
		/* constructs a specific string for every axis the controller type has
		 * these strings can be used to acces Unity input directly later
		 * the strins are stored in the given mapping container
		 * we also set invert flags for each axis here
		 * ToDo test and adjust all invert flags
		 */
		// we also reset all cached button states
		playerController.resetControllerCache();

		// convert the player enum into an integer
		int player = (int)playerController.player;

		// construct axis name for the controller type and player number
		// the generic axis are different for Android ports and other platforms (Editor)
		// the inversed encoding for axis values is also cared for here
		// this is solved by precompile makros
		switch (playerController.mapType)
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			case OuyaMapType.Generic_ANDROID:
			// tested mapping for OuyaGameController connected to HTCOne
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;
			playerController.map_RX = string.Format("Joy{0} Axis 4", player); playerController.invert_RX = false;
			playerController.map_RY = string.Format("Joy{0} Axis 5", player); playerController.invert_RY = true;
			playerController.map_LT = string.Format("Joy{0} Axis 6", player); playerController.invert_LT = false;
			playerController.map_RT = string.Format("Joy{0} Axis 3", player); playerController.invert_RT = false;
			// the dpad is not analog and therefore not mapped as an axis		
			playerController.map_DX = null; playerController.invert_DX = false;
			playerController.map_DY = null; playerController.invert_DY = false;
			break;

			case OuyaMapType.Broadcom_ANDROID:
			// tested mapping for OuyaGameController connected to Nexus7
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;
			playerController.map_RX = string.Format("Joy{0} Axis 4", player); playerController.invert_RX = false;
			playerController.map_RY = string.Format("Joy{0} Axis 5", player); playerController.invert_RY = true;
			playerController.map_LT = string.Format("Joy{0} Axis 3", player); playerController.invert_LT = false;
			playerController.map_RT = string.Format("Joy{0} Axis 6", player); playerController.invert_RT = false;
			// the dpad is not analog and therefore not mapped as an axis		
			playerController.map_DX = null; playerController.invert_DX = false;
			playerController.map_DY = null; playerController.invert_DY = false;
			break;

			case OuyaMapType.MogaPro_ANDROID:
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;
			playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;
			playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = false;
			playerController.map_LT = string.Format("Joy{0} Axis 8", player); playerController.invert_LT = false;
			playerController.map_RT = string.Format("Joy{0} Axis 7", player); playerController.invert_RT = false;
			playerController.map_DX = string.Format("Joy{0} Axis 5", player); playerController.invert_DX = false;
			playerController.map_DY = string.Format("Joy{0} Axis 6", player); playerController.invert_DY = false;
			break;

			case OuyaMapType.GameStick_CONSOLE:
			// tested mapping for GameStick console with native controller
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

			case OuyaMapType.PS3_ANDROID:
			case OuyaMapType.Ouya_CONSOLE:
			// tested mapping for OUYA console with native controller
			// tested mapping for PS3 controller connected to the OUYA console
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

			case OuyaMapType.XBOX360_ANDROID:
			// tested mapping for XBOX360 USB controller connected to the OUYA console
			// tested mapping for XBOX360 USB controller connected to the OUYA console
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
			playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
			playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
			playerController.map_LT = string.Format("Joy{0} Axis 7", player); playerController.invert_LT = false;		// checked
			playerController.map_RT = string.Format("Joy{0} Axis 8", player); playerController.invert_RT = false;		// checked
			playerController.map_DX = string.Format("Joy{0} Axis 5", player); playerController.invert_DX = false;		// checked
			playerController.map_DY = string.Format("Joy{0} Axis 6", player); playerController.invert_DY = true;		// checked
			break;

			case OuyaMapType.XBOX360_ANDROID_wireless:
			// tested mapping for XBOX360 Wireless controller connected to the OUYA console
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

#if UNITY_EDITOR || UNITY_STANDALONE_WIN	
			case OuyaMapType.Ouya_WIN:   
			// tested mapping for a OuyaGameController connected to Windows 7+8
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
			playerController.map_RX = string.Format("Joy{0} Axis 4", player); playerController.invert_RX = false;		// checked
			playerController.map_RY = string.Format("Joy{0} Axis 5", player); playerController.invert_RY = true;		// checked
			playerController.map_LT = string.Format("Joy{0} Axis 3", player); playerController.invert_LT = false;		// checked
			playerController.map_RT = string.Format("Joy{0} Axis 6", player); playerController.invert_RT = false;		// checked
			// the dpad is not analog and therefore not mapped as an axis		
			playerController.map_DX = null; playerController.invert_DX = false;
			playerController.map_DY = null; playerController.invert_DY = false;	
			break;

			case OuyaMapType.XBox360_WIN:
			// tested maping for a XBOX360 USB or Wireless controller connected to Windows 7+8
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
			playerController.map_RX = string.Format("Joy{0} Axis 4", player); playerController.invert_RX = false;		// checked		
			playerController.map_RY = string.Format("Joy{0} Axis 5", player); playerController.invert_RY = true;		// checked
			playerController.map_LT = string.Format("Joy{0} Axis 9", player); playerController.invert_LT = false;		// checked
			playerController.map_RT = string.Format("Joy{0} Axis 10", player); playerController.invert_RT = false;		// checked
			playerController.map_DX = string.Format("Joy{0} Axis 6", player); playerController.invert_DX = false;		// checked
			playerController.map_DY = string.Format("Joy{0} Axis 7", player); playerController.invert_DY = false;		// checked
			break;

			case OuyaMapType.PS3_WIN:
			// not tested yet
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
			playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
			playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
			// d-pad and triggers are not analog and therefore not mapped as an axis
			playerController.map_LT = null; playerController.invert_LT = false;
			playerController.map_RT = null; playerController.invert_RT = false;	
			playerController.map_DX = null; playerController.invert_DX = false;
			playerController.map_DY = null; playerController.invert_DY = false;
			break;

			case OuyaMapType.MotionInJoy_WIN:
			// tested mapping for PS3 controller connected to Windows 7 with configured MotionInJoy tools
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
			playerController.map_RX = string.Format("Joy{0} Axis 4", player); playerController.invert_RX = false;		// checked
			playerController.map_RY = string.Format("Joy{0} Axis 5", player); playerController.invert_RY = true;		// checked
			playerController.map_LT = string.Format("Joy{0} Axis 3", player); playerController.invert_LT = false;		// checked
			playerController.map_RT = string.Format("Joy{0} Axis 6", player); playerController.invert_RT = false;		// checked	
			// the dpad is not analog and therefore not mapped as an axis	
			playerController.map_DX = null; playerController.invert_DX = false;
			playerController.map_DY = null; playerController.invert_DY = false;	
			break;

			case OuyaMapType.GameStick_WIN:
			// the GameStick has no working driver on Windows
			// so far we can't support the device although we know it's name at least
			// this is just a placeholder whre real mapping should be once we find a driver
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;	
			playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		
			playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		
			playerController.map_DX = string.Format("Joy{0} Axis 5", player); playerController.invert_DX = false;		
			playerController.map_DY = string.Format("Joy{0} Axis 6", player); playerController.invert_DY = true;		
			// the GameStick controller has no triggers at all
			playerController.map_LT = null; playerController.invert_LT = false;
			playerController.map_RT = null; playerController.invert_RT = false;
			break;
#endif

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
			case OuyaMapType.PS3_OSX:
			// tested mapping for PS3 controller connected to MacOSX
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		// checked
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;		// checked
			playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		// checked
			playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		// checked
			// d-pad and triggers are not analog and therefore not mapped as an axis
			playerController.map_LT = null; playerController.invert_LT = false;
			playerController.map_RT = null; playerController.invert_RT = false;	
			playerController.map_DX = null; playerController.invert_DX = false;
			playerController.map_DY = null; playerController.invert_DY = false;
			break;

			case OuyaMapType.TattieBogle_OSX:
			// this is the driver for the XBOX360 controller on MacOSX
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

			case OuyaMapType.GameStick_OSX:
			// the GameStick has no working driver on MacOSX
			// so far we can't support the device although we know it's name at least
			// this is just a placeholder whre real mapping should be once we find a driver
			playerController.map_LX = string.Format("Joy{0} Axis 1", player); playerController.invert_LX = false;		
			playerController.map_LY = string.Format("Joy{0} Axis 2", player); playerController.invert_LY = true;	
			playerController.map_RX = string.Format("Joy{0} Axis 3", player); playerController.invert_RX = false;		
			playerController.map_RY = string.Format("Joy{0} Axis 4", player); playerController.invert_RY = true;		
			playerController.map_DX = string.Format("Joy{0} Axis 5", player); playerController.invert_DX = false;		
			playerController.map_DY = string.Format("Joy{0} Axis 6", player); playerController.invert_DY = true;		
			// the GameStick controller has no triggers at all
			playerController.map_LT = null; playerController.invert_LT = false;
			playerController.map_RT = null; playerController.invert_RT = false;
			break;
#endif
		
			case OuyaMapType.Unknown:
			// we hope to catch any unkown bluetooth controllers
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

	private static bool SwitchSinglePlayerMap() {
		/* this method allows to check if a single joystick connected has a shifted player channel
		 * sometimes a controller does not use the channel his Unity list position would indicate
		 * this means that a single player controller might be sending his inputs in channels 1-4
		 * this problem was observed when dis- and reconnecting XBOX360 USB controllers
		 * our strategy is to run through all the channels and check where we find an input
		 * return: a flag confirming that we have switched maps successfully
		 */
		// security exit in case of missing controller connection
		if (playerControllers[0] == null) return false;

		// check all channels until we find an input
		for (int i = 1; i <= playersMax; i++)
		{
			// convert the index to test player
			OuyaPlayer testPlayer = (OuyaPlayer)i;

			/* REAMAP TO NEXT CHANNEL */ 
			// switch the channles of the existing mapping
			playerControllers[0].setController(testPlayer, mapTypes[0]);
			// redo the mapping in the existing container
			mapController(playerControllers[0]);

			/* SCAN CHECK INPUT */ 
			// we check if we get an input signal on any button
			// or axis of this player channel
			// finally we return that we have found an active player channel
			if (GetAxis(OuyaAxis.LX, OuyaPlayer.P01) != 0) return true;
			if (GetAxis(OuyaAxis.LY, OuyaPlayer.P01) != 0) return true;
			if (GetAxis(OuyaAxis.RX, OuyaPlayer.P01) != 0) return true;
			if (GetAxis(OuyaAxis.RY, OuyaPlayer.P01) != 0) return true;
			if (GetAxis(OuyaAxis.DX, OuyaPlayer.P01) != 0) return true;
			if (GetAxis(OuyaAxis.DY, OuyaPlayer.P01) != 0) return true;
			if (GetAxis(OuyaAxis.LT, OuyaPlayer.P01) != 0) return true;
			if (GetAxis(OuyaAxis.RT, OuyaPlayer.P01) != 0) return true;

			if (GetButton(OuyaButton.O, OuyaPlayer.P01)) return true;
			if (GetButton(OuyaButton.U, OuyaPlayer.P01)) return true;
			if (GetButton(OuyaButton.Y, OuyaPlayer.P01)) return true;
			if (GetButton(OuyaButton.A, OuyaPlayer.P01)) return true;

			if (GetButton(OuyaButton.LB, OuyaPlayer.P01)) return true;
			if (GetButton(OuyaButton.RB, OuyaPlayer.P01)) return true;
			if (GetButton(OuyaButton.L3, OuyaPlayer.P01)) return true;
			if (GetButton(OuyaButton.R3, OuyaPlayer.P01)) return true;

			if (GetButton(OuyaButton.START, OuyaPlayer.P01)) return true;
			if (GetButton(OuyaButton.SELECT, OuyaPlayer.P01)) return true;
			if (GetButton(OuyaButton.SYSTEM, OuyaPlayer.P01)) return true;
		}
		// return false if we didn't receive input yet
		// this may happen when the player does not start pushing puttons
		// but most players will try if their controller works
		return false;
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

	public static OuyaMapType GetControllerType(OuyaPlayer player) {
		/* returns the controller type (enum) for a designated player
		 */
		int index = (int)player -1;
		if (controllerCount == 0) return OuyaMapType.None;
		else return mapTypes[index];
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
		OuyaMapType mapType = mapTypes[playerIndex];

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
					switch (mapType) {
						case OuyaMapType.MotionInJoy_WIN:
						case OuyaMapType.PS3_ANDROID:
						case OuyaMapType.PS3_OSX:
						if (GetButton(OuyaButton.LT, player)) return 1f;
						else return 0f;
					}
				}
				break;
				/* RT-TRIGGER */
				case OuyaAxis.RT: axisName = playerController.map_RT; invert = playerController.invert_RT;
				// if the trigger is treated like a button we convert button press bool flags into axis values
				if (axisName == null)
				{
					switch (mapType) {
						case OuyaMapType.MotionInJoy_WIN:
						case OuyaMapType.PS3_ANDROID:
						case OuyaMapType.PS3_OSX:
						if (GetButton(OuyaButton.RT, player)) return 1f;
						else return 0f;
					}
				}
				break;
				/* DX-AXIS */
				case OuyaAxis.DX: axisName = playerController.map_DX; invert = playerController.invert_DX;
				// if the dpad is treated like a button we convert button press bool flags into axis values
				if (axisName == null)
				{
					switch (mapType)
					{
#if !UNITY_EDITOR && UNITY_ANDROID
						case OuyaMapType.Generic_ANDROID:
						case OuyaMapType.Broadcom_ANDROID:
						case OuyaMapType.Ouya_CONSOLE:
						case OuyaMapType.PS3_ANDROID:
						case OuyaMapType.XBOX360_ANDROID_wireless:
						if (GetButton(OuyaButton.DL, player)) return -1f;
						else if (GetButton(OuyaButton.DR, player)) return 1f;
						break;
#endif
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
						case OuyaMapType.Ouya_WIN:
						case OuyaMapType.MotionInJoy_WIN:
						if (GetButton(OuyaButton.DL, player)) return -1f;
						else if (GetButton(OuyaButton.DR, player)) return 1f;
						break;
#endif
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
						case OuyaMapType.PS3_OSX:
						case OuyaMapType.TattieBogle_OSX:
						if (GetButton(OuyaButton.DL, player)) return -1f;
						else if (GetButton(OuyaButton.DR, player)) return 1f;
						break;
#endif
					}
				} break;
				/* DY-AXIS */
				case OuyaAxis.DY: axisName = playerController.map_DY; invert = playerController.invert_DY;
				// if the dpad is treated like a button we convert button press bool flags into axis values
				if (axisName == null)
				{
					switch (mapType)
					{		
						#if !UNITY_EDITOR && UNITY_ANDROID
						case OuyaMapType.Generic_ANDROID:
						case OuyaMapType.Broadcom_ANDROID:
						case OuyaMapType.Ouya_CONSOLE:
						case OuyaMapType.PS3_ANDROID:
						case OuyaMapType.XBOX360_ANDROID_wireless:
						if (GetButton(OuyaButton.DD, player)) return -1f;
						else if (GetButton(OuyaButton.DU, player)) return 1f;
						break;
						#endif
						#if UNITY_EDITOR || UNITY_STANDALONE_WIN
						case OuyaMapType.Ouya_WIN:
						case OuyaMapType.MotionInJoy_WIN:
						if (GetButton(OuyaButton.DD, player)) return -1f;
						else if (GetButton(OuyaButton.DU, player)) return 1f;
						break;
						#endif
						#if UNITY_EDITOR || UNITY_STANDALONE_OSX
						case OuyaMapType.PS3_OSX:
						case OuyaMapType.TattieBogle_OSX:
						if (GetButton(OuyaButton.DD, player)) return -1f;
						else if (GetButton(OuyaButton.DU, player)) return 1f;
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

	public static bool GetButton(OuyaButton button, OuyaPlayer player) {
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
		// this is not really needed in CLARK â€“ just framework coherence
		int playerIndex = (int) player - 1;
		if (playerIndex >= controllerCount) return false;

		// finally check if we really found a joystick for the player
		OuyaMapType mapType = mapTypes[playerIndex];

		// get the controller mapping for the player
		PlayerController playerController = playerControllers[playerIndex];

		// this twisted recursive move is used for switch-mapped players
		OuyaPlayer mapPlayer = playerController.player;

		// secure that we have found a mapping
		if (playerController != null) {

			/* FIND THE CORRECT MAPPED BUTTON KEY */ 
			switch (mapType)
			{				
				/* ANDROID MAPPINGS */
#if !UNITY_EDITOR && UNITY_ANDROID
				case OuyaMapType.Broadcom_ANDROID:
				// tested mapping for: OuyaGameController connected to Nexus7
				// connected via standard Bluetooth connection
				case OuyaMapType.Generic_ANDROID:
				// tested mapping for: OuyaGameController connected to HTCOne
				// connected via standard Bluetooth connection
				switch (button)
				{
					// ouya buttons
					case OuyaButton.O:		return GetButton(0, buttonAction, mapPlayer);		// checked
					case OuyaButton.U:		return GetButton(1, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y:		return GetButton(2, buttonAction, mapPlayer);		// checked
					case OuyaButton.A:		return GetButton(3, buttonAction, mapPlayer);		// checked

					// shoulder buttons
					case OuyaButton.LB:		return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB:		return GetButton(5, buttonAction, mapPlayer);		// checked

					// stick buttons
					case OuyaButton.L3:		return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3:		return GetButton(7, buttonAction, mapPlayer);		// checked

					// d-pad buttons
					case OuyaButton.DU:		return GetButton(8, buttonAction, mapPlayer);		// checked
					case OuyaButton.DD:		return GetButton(9, buttonAction, mapPlayer);		// checked
					case OuyaButton.DL:		return GetButton(10, buttonAction, mapPlayer);		// checked
					case OuyaButton.DR:		return GetButton(11, buttonAction, mapPlayer);		// checked

					// trigger buttons
					// although button states are natively supported we use axis conversion
					// this is because trigger buttons will natively only react on full pullthrough
					// these buttons then are two axis and do not give out UP or DOWN events natively
					// we use button state management and continious scanning to provide these	
					case OuyaButton.LT: return GetCachedButtonEvent(button, buttonAction, playerIndex);														
					case OuyaButton.RT: return GetCachedButtonEvent(button, buttonAction, playerIndex);

					// not defined so far â€“ or don't exist
					case OuyaButton.START: 	return false;
					case OuyaButton.SYSTEM: return false;
					case OuyaButton.SELECT: return false;	
					default: return false;														
				}

				case OuyaMapType.MogaPro_ANDROID:
				// this device was not tested yet
				// the setting were just extracted from some examples I found
				// please feedback if you find a way to test it
				switch (button)
				{
					// OUYA buttons
					case OuyaButton.O:		return GetButton(0, buttonAction, mapPlayer);
					case OuyaButton.U:		return GetButton(3, buttonAction, mapPlayer);
					case OuyaButton.Y:		return GetButton(4, buttonAction, mapPlayer);
					case OuyaButton.A:		return GetButton(1, buttonAction, mapPlayer);

					// shoulder buttons
					case OuyaButton.LB:		return GetButton(6, buttonAction, mapPlayer);
					case OuyaButton.RB:		return GetButton(7, buttonAction, mapPlayer);

					// stick buttons	
					case OuyaButton.L3:		return GetButton(13, buttonAction, mapPlayer);
					case OuyaButton.R3:		return GetButton(14, buttonAction, mapPlayer);

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

				case OuyaMapType.GameStick_CONSOLE:
				// tested mapping for: GameStick console with native controller
				// connected via standard Bluetooth 
				// triggers do not exist at all on this controller
				switch (button)
				{
					// OUYA buttons
					case OuyaButton.O: 		return GetButton(0, buttonAction, mapPlayer);		// checked
					case OuyaButton.U: 		return GetButton(3, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y: 		return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.A: 		return GetButton(1, buttonAction, mapPlayer);		// checked

					// shoulder buttons	
					case OuyaButton.LB: 	return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB: 	return GetButton(7, buttonAction, mapPlayer);		// checked

					// stick buttons	
					case OuyaButton.L3: 	return GetButton(13, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3: 	return GetButton(14, buttonAction, mapPlayer);		// checked

					// center buttons	
					case OuyaButton.SELECT: return GetButton(27, buttonAction, mapPlayer);		// checked			
					case OuyaButton.START: 	return GetButton(11, buttonAction, mapPlayer);		// checked	

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
					// but this is not valid in Unity (see script reference KeyCode)
					case OuyaButton.SYSTEM: return false;
					default: return false;
				}

				case OuyaMapType.Ouya_CONSOLE:	
				// tested mapping for: OUYA console with native controller
				// connected via standard Bluetooth
				switch (button)
				{
					// OUYA buttons	
					case OuyaButton.O:		return GetButton(0, buttonAction, mapPlayer);		// checked
					case OuyaButton.U:		return GetButton(1, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y:		return GetButton(2, buttonAction, mapPlayer);		// checked
					case OuyaButton.A:		return GetButton(3, buttonAction, mapPlayer);		// checked

					// shoulder buttons
					case OuyaButton.LB: 	return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB: 	return GetButton(5, buttonAction, mapPlayer);		// checked

					// stick buttons
					case OuyaButton.L3:		return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3:		return GetButton(7, buttonAction, mapPlayer);		// checked

					// d-pad buttons
					case OuyaButton.DU:		return GetButton(8, buttonAction, mapPlayer);		// checked
					case OuyaButton.DD:		return GetButton(9, buttonAction, mapPlayer);		// checked
					case OuyaButton.DL:		return GetButton(10, buttonAction, mapPlayer);		// checked
					case OuyaButton.DR:		return GetButton(11, buttonAction, mapPlayer);		// checked

					// trigger buttons
					// although button states are natively supported we use axis conversion
					// the button numbers we do not use are: LT 12 / RR 13
					// this is because trigger buttons will natively only react on full pullthrough
					// this feels very unresponsive and can be solved by evaluating the axis input values
					// these two axis and do not give out UP or DOWN events natively
					// we use button state management and continious scanning to provide these	
					case OuyaButton.LT: return GetCachedButtonEvent(button, buttonAction, playerIndex);														
					case OuyaButton.RT: return GetCachedButtonEvent(button, buttonAction, playerIndex);

					// not defined so far â€“ or don't exist on OUYA
					case OuyaButton.START: 	return false;
					case OuyaButton.SYSTEM: return false;
					case OuyaButton.SELECT: return false;	
					default: return false;
				}

				case OuyaMapType.XBOX360_ANDROID:
				// tested mapping for: XBOX360 USB controller connnected to OUYA, Nexus7
				// connected via USB cable (with USBToGo adapter cable for Nexus7)
				switch (button)
				{
					// OUYA buttons
					case OuyaButton.O: 		return GetButton(0, buttonAction, mapPlayer);		// checked
					case OuyaButton.U: 		return GetButton(3, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y: 		return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.A: 		return GetButton(1, buttonAction, mapPlayer);		// checked

					// shoulder buttons	
					case OuyaButton.LB:		return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB:		return GetButton(7, buttonAction, mapPlayer);		// checked

					// center buttons
					case OuyaButton.START: 	return GetButton(11, buttonAction, mapPlayer);		// checked
					// this mapping for the select button works on OUYA but not on Nexus7
					// Is it worth a complete new mapping section?
					case OuyaButton.SELECT:	return GetButton(27, buttonAction, mapPlayer);

					// stick buttons
					case OuyaButton.L3: 	return GetButton(13, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3: 	return GetButton(14, buttonAction, mapPlayer);		// checked

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

				case OuyaMapType.XBOX360_ANDROID_wireless:
				// tested mapping for: XBOX360 Wireless controller connected to OUYA
				// connected via MicrosoftWirelessReceiver which connects via USB cable
				switch (button)
				{
					// OUYA buttons
					case OuyaButton.O: 		return GetButton(0, buttonAction, mapPlayer);		// checked
					case OuyaButton.U: 		return GetButton(3, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y: 		return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.A: 		return GetButton(1, buttonAction, mapPlayer);		// checked

					// shoulder buttons	
					case OuyaButton.LB:		return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB:		return GetButton(7, buttonAction, mapPlayer);		// checked

					// center buttons
					case OuyaButton.START: 	return GetButton(11, buttonAction, mapPlayer);		// checked
					case OuyaButton.SELECT:	return GetButton(27, buttonAction, mapPlayer);

					// stick buttons
					case OuyaButton.L3: 	return GetButton(13, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3: 	return GetButton(14, buttonAction, mapPlayer);		// checked

					// d-pad buttons
					// these buttons are very strange as they run on the same KeyCodes as O U (Y) A buttons
					// so if we eant to work fine on Android we can't really use the d-pad
					// that's why we partly deactivate these buttons via a flag
					// pressing a d-pad button will still lead to [O U Y A] action button events
					// this can't be helped â€“ at leas we won't fire d-pad input when pressing action buttons
					case OuyaButton.DU:
					if (XBOX360W_ANDROID_DPAD) return GetButton(2, buttonAction, mapPlayer); else return false;	
					case OuyaButton.DD:
					if (XBOX360W_ANDROID_DPAD) return GetButton(3, buttonAction, mapPlayer); else return false;	
					case OuyaButton.DL:
					if (XBOX360W_ANDROID_DPAD) return GetButton(0, buttonAction, mapPlayer); else return false;		
					case OuyaButton.DR:
					if (XBOX360W_ANDROID_DPAD) return GetButton(1, buttonAction, mapPlayer); else return false;	

					// trigger buttons
					// these buttons are two axis and do not give out UP or DOWN events natively
					// we use button state management and continious scanning to provide these	
					case OuyaButton.LT: return GetCachedButtonEvent(button, buttonAction, playerIndex);														
					case OuyaButton.RT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);														

					// not defined so far
					case OuyaButton.SYSTEM: return false;
					default: return false;
				}

				case OuyaMapType.PS3_ANDROID:
				// tested mapping for: PS3 DS controller connected to OUYA
				// connection via standard Bluetooth
				// pairing achieved using a temporary USB cable connection
				switch (button)
				{
					// stick buttons
					case OuyaButton.L3: 	return GetButton(1, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3: 	return GetButton(2, buttonAction, mapPlayer);		// checked

					// center buttons
					case OuyaButton.START: 	return GetButton(3, buttonAction, mapPlayer);		// checked
					case OuyaButton.SELECT: return GetButton(27, buttonAction, mapPlayer);		// checked

					// d-pad buttons	
					case OuyaButton.DU:		return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.DR:		return GetButton(5, buttonAction, mapPlayer);		// checked
					case OuyaButton.DD:		return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.DL:		return GetButton(7, buttonAction, mapPlayer);		// checked

					// trigger buttons
					case OuyaButton.LT: 	return GetButton(8, buttonAction, mapPlayer);		// checked
					case OuyaButton.RT: 	return GetButton(9, buttonAction, mapPlayer);		// checked

					// shoulder buttons
					case OuyaButton.LB:		return GetButton(10, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB:		return GetButton(11, buttonAction, mapPlayer);		// checked

					// OUYA buttons
					case OuyaButton.O:		return GetButton(14, buttonAction, mapPlayer);		// checked
					case OuyaButton.U:		return GetButton(15, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y:		return GetButton(12, buttonAction, mapPlayer);		// checked
					case OuyaButton.A:		return GetButton(13, buttonAction, mapPlayer);		// checked

					// not defined do far
					case OuyaButton.SYSTEM: return false;	
					default: return false;
				}
#endif

				/* WINDOWS MAPPINGS */
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
				case OuyaMapType.Ouya_WIN:
				// tested mapping for: OUYA Game Controller connected to Windows 7+8 64bit
				// connection via standard Bluetooth (preinstalled OS drivers)
				switch (button)
				{
					// shoulder buttons
					case OuyaButton.LB: 	return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB: 	return GetButton(5, buttonAction, mapPlayer);		// checked

					// OUYA buttons	
					case OuyaButton.O:		return GetButton(0, buttonAction, mapPlayer);		// checked
					case OuyaButton.U:		return GetButton(1, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y:		return GetButton(2, buttonAction, mapPlayer);		// checked
					case OuyaButton.A:		return GetButton(3, buttonAction, mapPlayer);		// checked

					// stick buttons
					case OuyaButton.L3:		return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3:		return GetButton(7, buttonAction, mapPlayer);		// checked

					// d-pad buttons
					case OuyaButton.DU:		return GetButton(8, buttonAction, mapPlayer);		// checked
					case OuyaButton.DD:		return GetButton(9, buttonAction, mapPlayer);		// checked
					case OuyaButton.DL:		return GetButton(10, buttonAction, mapPlayer);		// checked
					case OuyaButton.DR:		return GetButton(11, buttonAction, mapPlayer);		// checked

					// trigger buttons
					// although button states are natively supported we use axis conversion
					// the button numbers we do not use are: LT 12 / RR 13
					// this is because trigger buttons will natively only react on full pullthrough
					// this feels very unresponsive and can be solved by evaluating the axis input values
					// these two axis and do not give out UP or DOWN events natively
					// we use button state management and continious scanning to provide these	
					case OuyaButton.LT: return GetCachedButtonEvent(button, buttonAction, playerIndex);														
					case OuyaButton.RT: return GetCachedButtonEvent(button, buttonAction, playerIndex);

					// not defined so far â€“ or don't exist on OUYA
					case OuyaButton.START: return false;
					case OuyaButton.SYSTEM: return false;
					case OuyaButton.SELECT: return false;	
					default: return false;
				}			

				case OuyaMapType.XBox360_WIN:
				switch (button)
					// tested mapping for XBOX360 USB controller connected to Windows 7+8 64bit
					// connection via USB cable (official Microsoft XBOX360 Drivers on Windows 7)
					// connection via USB cable (preinstalled OS drivers on Windows 8)
				{
					// OUYA buttons
					case OuyaButton.O:		return GetButton(0, buttonAction, mapPlayer);		// checked
					case OuyaButton.U:		return GetButton(2, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y:		return GetButton(3, buttonAction, mapPlayer);		// checked
					case OuyaButton.A:		return GetButton(1, buttonAction, mapPlayer);		// checked

					// shoulder buttons
					case OuyaButton.LB:		return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB:		return GetButton(5, buttonAction, mapPlayer);		// checked

					// center buttons	
					case OuyaButton.START:	return GetButton(7, buttonAction, mapPlayer);		// checked
					case OuyaButton.SELECT: return GetButton(6, buttonAction, mapPlayer);		// checked

					// stick buttons
					case OuyaButton.L3: 	return GetButton(8, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3: 	return GetButton(9, buttonAction, mapPlayer);		// checked

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

				case OuyaMapType.PS3_WIN:
				// tested mapping for: PS3 DS controller connected to Windows 8 64bit
				// connection via standard Bluetooth (preinstalled OS drivers)
				// pairing achieved using a temporary USB cable connection
				switch (button)
				{
					// stick buttons
					case OuyaButton.L3: 	return GetButton(1, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3: 	return GetButton(2, buttonAction, mapPlayer);		// checked

					// center buttons
					case OuyaButton.START: 	return GetButton(3, buttonAction, mapPlayer);		// checked
					case OuyaButton.SELECT: return GetButton(0, buttonAction, mapPlayer);		// checked

					// d-pad buttons	
					case OuyaButton.DU:		return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.DR:		return GetButton(5, buttonAction, mapPlayer);		// checked
					case OuyaButton.DD:		return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.DL:		return GetButton(7, buttonAction, mapPlayer);		// checked

					// trigger buttons
					case OuyaButton.LT: 	return GetButton(8, buttonAction, mapPlayer);		// checked
					case OuyaButton.RT: 	return GetButton(9, buttonAction, mapPlayer);		// checked

					// shoulder buttons
					case OuyaButton.LB:		return GetButton(10, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB:		return GetButton(11, buttonAction, mapPlayer);		// checked

					// OUYA buttons
					case OuyaButton.O:		return GetButton(14, buttonAction, mapPlayer);		// checked
					case OuyaButton.U:		return GetButton(15, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y:		return GetButton(12, buttonAction, mapPlayer);		// checked
					case OuyaButton.A:		return GetButton(13, buttonAction, mapPlayer);		// checked

					// not defined do far
					case OuyaButton.SYSTEM: return false;	
					default: return false;
				}

				case OuyaMapType.MotionInJoy_WIN:
				// tested mapping for: PS3 DS controller connected to Windows 7 64bit
				// connection via USB cable (custom setup using the most popular but crappy driver: MotionInJoy)
				// this needs a CUSTOM button mapping setup in the driver tools to work (see documentation)
				// default sets could not be used as they do not make sense (gyro's and sticks share the same axis)
				// READ THE DOCUMENTATION to make this work !!!
				switch (button)
				{
					// OUYA buttons
					case OuyaButton.O:		return GetButton(2, buttonAction, mapPlayer);		// checked
					case OuyaButton.U:		return GetButton(3, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y:		return GetButton(0, buttonAction, mapPlayer);		// checked
					case OuyaButton.A:		return GetButton(1, buttonAction, mapPlayer);		// checked

					// shoulder buttons
					case OuyaButton.LB:		return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB:		return GetButton(5, buttonAction, mapPlayer);		// checked	

					// trigger buttons
					case OuyaButton.LT: 	return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.RT: 	return GetButton(7, buttonAction, mapPlayer);		// checked

					// stick buttons
					case OuyaButton.L3: 	return GetButton(8, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3: 	return GetButton(9, buttonAction, mapPlayer);		// checked

					// center buttons
					case OuyaButton.SELECT: return GetButton(10, buttonAction, mapPlayer);		// checked
					case OuyaButton.START: 	return GetButton(11, buttonAction, mapPlayer);		// checked
					case OuyaButton.SYSTEM: return GetButton(12, buttonAction, mapPlayer);		// checked

					// d-pad buttons	
					case OuyaButton.DU:		return GetButton(13, buttonAction, mapPlayer);		// checked
					case OuyaButton.DR:		return GetButton(14, buttonAction, mapPlayer);		// checked
					case OuyaButton.DD:		return GetButton(15, buttonAction, mapPlayer);		// checked
					case OuyaButton.DL:		return GetButton(16, buttonAction, mapPlayer);		// checked

					// not defined do far
					default: return false;
				}

				case OuyaMapType.GameStick_WIN:
				// the GameStick has no working driver on Windows
				// so far we can't support the device although we know it's name at least
				// this is just a placeholder where real mapping should be once we find a driver
				switch (button)
				{
					// OUYA buttons
					case OuyaButton.O: 		return GetButton(0, buttonAction, mapPlayer);	
					case OuyaButton.U: 		return GetButton(3, buttonAction, mapPlayer);		
					case OuyaButton.Y: 		return GetButton(4, buttonAction, mapPlayer);		
					case OuyaButton.A: 		return GetButton(1, buttonAction, mapPlayer);		

					// shoulder buttons	
					case OuyaButton.LB: 	return GetButton(6, buttonAction, mapPlayer);		
					case OuyaButton.RB: 	return GetButton(7, buttonAction, mapPlayer);		

					// stick buttons	
					case OuyaButton.L3: 	return GetButton(13, buttonAction, mapPlayer);		
					case OuyaButton.R3: 	return GetButton(14, buttonAction, mapPlayer);		

					// center buttons	
					case OuyaButton.SELECT: return GetButton(27, buttonAction, mapPlayer);					
					case OuyaButton.START: 	return GetButton(11, buttonAction, mapPlayer);			

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
#endif

				/* MACOSX MAPPINGS */
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
				case OuyaMapType.PS3_OSX:
				// tested mapping for PS3 DS controller connected to the MacOSX
				// connection via standard Bluetooth (prinstalled drivers)
				// pairing achieved using a temporary USB cable connection
				switch (button)
				{
					// stick buttons
					case OuyaButton.L3: 	return GetButton(1, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3: 	return GetButton(2, buttonAction, mapPlayer);		// checked

					// center buttons
					case OuyaButton.START: 	return GetButton(3, buttonAction, mapPlayer);		// checked
					case OuyaButton.SELECT: return GetButton(0, buttonAction, mapPlayer);		// checked

					// d-pad buttons	
					case OuyaButton.DU:		return GetButton(4, buttonAction, mapPlayer);		// checked
					case OuyaButton.DR:		return GetButton(5, buttonAction, mapPlayer);		// checked
					case OuyaButton.DD:		return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.DL:		return GetButton(7, buttonAction, mapPlayer);		// checked

					// trigger buttons
					case OuyaButton.LT: 	return GetButton(8, buttonAction, mapPlayer);		// checked
					case OuyaButton.RT: 	return GetButton(9, buttonAction, mapPlayer);		// checked

					// shoulder buttons
					case OuyaButton.LB:		return GetButton(10, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB:		return GetButton(11, buttonAction, mapPlayer);		// checked

					// OUYA buttons
					case OuyaButton.O:		return GetButton(14, buttonAction, mapPlayer);		// checked
					case OuyaButton.U:		return GetButton(15, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y:		return GetButton(12, buttonAction, mapPlayer);		// checked
					case OuyaButton.A:		return GetButton(13, buttonAction, mapPlayer);		// checked

					// not defined do far
					case OuyaButton.SYSTEM: return false;	
					default: return false;
				}

				case OuyaMapType.TattieBogle_OSX:
				// tested mapping for: XBOX360 USB controller connected to MacOSX
				// tested mapping for: XBOX360 Wireless controller connected to MacOSX
				// connection via USB cable (with the free to dowload TattieBogle driver)
				// NOTE: disconnecting a XBOX360 USB controller using Tattie can lead to full system reboot
				switch (button)
				{
					// shoulder buttons
					case OuyaButton.LB: 		return GetButton(13, buttonAction, mapPlayer);		// checked
					case OuyaButton.RB: 		return GetButton(14, buttonAction, mapPlayer);		// checked

					// OUYA buttons
					case OuyaButton.O: 			return GetButton(16, buttonAction, mapPlayer);		// checked
					case OuyaButton.U:			return GetButton(18, buttonAction, mapPlayer);		// checked
					case OuyaButton.Y: 			return GetButton(19, buttonAction, mapPlayer);		// checked
					case OuyaButton.A: 			return GetButton(17, buttonAction, mapPlayer);		// checked

					// stick buttons
					case OuyaButton.L3:			return GetButton(11, buttonAction, mapPlayer);		// checked
					case OuyaButton.R3:			return GetButton(12, buttonAction, mapPlayer);		// checked

					// center buttons
					case OuyaButton.SELECT:		return GetButton(10, buttonAction, mapPlayer);		// checked
					case OuyaButton.START:		return GetButton(9, buttonAction, mapPlayer);		// checked
					case OuyaButton.SYSTEM:		return GetButton(15, buttonAction, mapPlayer);		// checked

					// d-pad buttons
					case OuyaButton.DU:			return GetButton(5, buttonAction, mapPlayer);		// checked
					case OuyaButton.DD:			return GetButton(6, buttonAction, mapPlayer);		// checked
					case OuyaButton.DL:			return GetButton(7, buttonAction, mapPlayer);		// checked
					case OuyaButton.DR:			return GetButton(8, buttonAction, mapPlayer);		// checked

					// trigger buttons
					// the triggers are axis and do not give out UP or DOWN events natively
					// we use button state management and continious scanning to provide these
					case OuyaButton.LT:	return GetCachedButtonEvent(button, buttonAction, playerIndex);	
					case OuyaButton.RT: return GetCachedButtonEvent(button, buttonAction, playerIndex);
					default: return false;
				}

				case OuyaMapType.GameStick_OSX:
				// the GameStick has no working driver on MacOSX
				// so far we can't support the device although we know it's name at least
				// this is just a placeholder where real mapping should be once we find a driver
				switch (button)
				{
					// OUYA buttons
					case OuyaButton.O: 		return GetButton(0, buttonAction, mapPlayer);	
					case OuyaButton.U: 		return GetButton(3, buttonAction, mapPlayer);		
					case OuyaButton.Y: 		return GetButton(4, buttonAction, mapPlayer);		
					case OuyaButton.A: 		return GetButton(1, buttonAction, mapPlayer);		

					// shoulder buttons	
					case OuyaButton.LB: 	return GetButton(6, buttonAction, mapPlayer);		
					case OuyaButton.RB: 	return GetButton(7, buttonAction, mapPlayer);		

					// stick buttons	
					case OuyaButton.L3: 	return GetButton(13, buttonAction, mapPlayer);		
					case OuyaButton.R3: 	return GetButton(14, buttonAction, mapPlayer);		

					// center buttons	
					case OuyaButton.SELECT: return GetButton(27, buttonAction, mapPlayer);					
					case OuyaButton.START: 	return GetButton(11, buttonAction, mapPlayer);			

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
#endif

				/* UNKNOWN MAPPINGS */
				case OuyaMapType.Unknown:
				// we hope to catch any unkown bluetooth controllers here (wild card)
				// there can't be any testing for that as it's just a random try
				switch (button)
				{
					// ouya buttons
					case OuyaButton.O:		return GetButton(0, buttonAction, mapPlayer);
					case OuyaButton.U:		return GetButton(3, buttonAction, mapPlayer);
					case OuyaButton.Y:		return GetButton(4, buttonAction, mapPlayer);
					case OuyaButton.A:		return GetButton(1, buttonAction, mapPlayer);

					// shoulder buttons
					case OuyaButton.LB:		return GetButton(6, buttonAction, mapPlayer);
					case OuyaButton.RB:		return GetButton(7, buttonAction, mapPlayer);

					// stick buttons
					case OuyaButton.L3:		return GetButton(13, buttonAction, mapPlayer);
					case OuyaButton.R3:		return GetButton(14, buttonAction, mapPlayer);

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

	public static void SetTriggerThreshold(float threshold) {
		/* allows to adjust the trigger threshold
		 * this is only needed if the "Dead" values in the Input Manager Settings were set to 0.
		 * default is 0.1f
		 */
		triggerThreshold = threshold;
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
		 * (x-positive-axis is 0Â° / y-positive axis is 90Â° / x-negative axis is 180Â° / y-negative-axis is 270Â°)
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
		 * (x-positive-axis is 0Â° / y-positive axis is 90Â° / x-negative axis is 180Â° / y-negative-axis is 270Â°)
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