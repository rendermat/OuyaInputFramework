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
using System.Collections;

public class OuyaInputTester : MonoBehaviour
{
	// attach this script to any GameObject
	// this tester script draws a GUI overlay to display controller values
	// trigger and d-pad button events are displayed via the debug console
	// as Unity's basic GUI is slow and we do create a lot of strings here
	// performance and garbage collection might by bad – this has nothing to do with OuyaInput
	// please note that any lag in the console messages is due to the slow debug console
	// OuyaInput should have very low latency and nearly no garbage collection
	
	/* INSPECTOR */ 
	// easy switch to stop the GUI overlay
	public bool debug = true;
	
	// do we want to scan for trigger and d-pad button events ?
	public bool continuousScan = true;
	
	// the player we want to observe
	public OuyaPlayer observedPlayer = OuyaPlayer.P01;
	
	// the type of deadzone we want to use for convenience access
	public DeadzoneType deadzoneType = DeadzoneType.CircularMap;
	
	// the size of the deadzone
	public float deadzone = 0.25f;
	public float triggerThreshold = 0.1f;
	
	
	/* -----------------------------------------------------------------------------------
	 * INITIAL SETUP
	 */
	
	public void Start()
	{
		// set button state scanning to receive input state events for trigger and d-pads
		OuyaInput.SetContinuousScanning(continuousScan);
		
		// define the deadzone if you want to use advanced joystick and trigger access
		OuyaInput.SetDeadzone(deadzoneType, deadzone);
		OuyaInput.SetTriggerThreshold(triggerThreshold);
		
		// do one controller update here to get everything started as soon as possible
		OuyaInput.UpdateControllers();
	}
	
	/* -----------------------------------------------------------------------------------
	 * GUI CONTROLLER DISPLAY
	 */
	
	public void OnGUI()
	{
		if (debug) 
		{	
			GUI.Label(new Rect(50, 40, 100, 20), "LX " + OuyaInput.GetAxis(OuyaAxis.LX, observedPlayer));
			GUI.Label(new Rect(50, 60, 100, 20), "LY " + OuyaInput.GetAxis(OuyaAxis.LY, observedPlayer));
			
			GUI.Label(new Rect(250, 40, 100, 20), "RX " + OuyaInput.GetAxis(OuyaAxis.RX, observedPlayer));
			GUI.Label(new Rect(250, 60, 100, 20), "RY " + OuyaInput.GetAxis(OuyaAxis.RY, observedPlayer));
			
			GUI.Label(new Rect(50, 100, 200, 20), "LT " + OuyaInput.GetAxis(OuyaAxis.LT, observedPlayer));
			GUI.Label(new Rect(50, 120, 200, 20), "RT " + OuyaInput.GetAxis(OuyaAxis.RT, observedPlayer));
			
			GUI.Label(new Rect(250, 100, 200, 20), "LT-B " + OuyaInput.GetButton(OuyaButton.LT, observedPlayer));
			GUI.Label(new Rect(250, 120, 200, 20), "RT-B " + OuyaInput.GetButton(OuyaButton.RT, observedPlayer));
			
			GUI.Label(new Rect(50, 160, 100, 20), "DX " + OuyaInput.GetAxis(OuyaAxis.DX, observedPlayer));
			GUI.Label(new Rect(50, 180, 100, 20), "DY " + OuyaInput.GetAxis(OuyaAxis.DY, observedPlayer));
			
			GUI.Label(new Rect(50, 200, 100, 20), "L3 " + OuyaInput.GetButton(OuyaButton.L3, observedPlayer));
			GUI.Label(new Rect(50, 220, 100, 20), "R3 " + OuyaInput.GetButton(OuyaButton.R3, observedPlayer));
			
			GUI.Label(new Rect(250, 160, 100, 20), "DU-B " + OuyaInput.GetButton(OuyaButton.DU, observedPlayer));
			GUI.Label(new Rect(250, 180, 100, 20), "DD-B " + OuyaInput.GetButton(OuyaButton.DD, observedPlayer));
			GUI.Label(new Rect(250, 200, 100, 20), "DL-B " + OuyaInput.GetButton(OuyaButton.DL, observedPlayer));
			GUI.Label(new Rect(250, 220, 100, 20), "DR-B " + OuyaInput.GetButton(OuyaButton.DR, observedPlayer));
			
			GUI.Label(new Rect(250, 260, 200, 20), "LB " + OuyaInput.GetButton(OuyaButton.LB, observedPlayer));
			GUI.Label(new Rect(250, 280, 200, 20), "RB " + OuyaInput.GetButton(OuyaButton.RB, observedPlayer));
			
			GUI.Label(new Rect(50, 260, 100, 20), "O " + OuyaInput.GetButton(OuyaButton.O, observedPlayer));
			GUI.Label(new Rect(50, 280, 100, 20), "U " + OuyaInput.GetButton(OuyaButton.U, observedPlayer));
			GUI.Label(new Rect(50, 300, 100, 20), "Y " + OuyaInput.GetButton(OuyaButton.Y, observedPlayer));
			GUI.Label(new Rect(50, 320, 100, 20), "A " + OuyaInput.GetButton(OuyaButton.A, observedPlayer));
			
			GUI.Label(new Rect(50, 360, 100, 20), "SEL " + OuyaInput.GetButton(OuyaButton.SELECT, observedPlayer));
			GUI.Label(new Rect(50, 380, 100, 20), "SYS " + OuyaInput.GetButton(OuyaButton.SYSTEM, observedPlayer));
			GUI.Label(new Rect(50, 400, 100, 20), "START " + OuyaInput.GetButton(OuyaButton.START, observedPlayer));
			
			GUI.Label(new Rect(50, 440, 600, 20), "JOY_L " + OuyaInput.GetJoystick(OuyaJoystick.LeftStick, observedPlayer));
			GUI.Label(new Rect(50, 460, 600, 20), "JOY_R " + OuyaInput.GetJoystick(OuyaJoystick.RightStick, observedPlayer));
			GUI.Label(new Rect(50, 480, 600, 20), "JOY_D " + OuyaInput.GetJoystick(OuyaJoystick.DPad, observedPlayer));
			
			GUI.Label(new Rect(50, 520, 600, 20), "NAME " + OuyaInput.GetControllerName(observedPlayer));
			GUI.Label(new Rect(50, 540, 600, 20), "TYPE " + OuyaInput.GetControllerType(observedPlayer));
		}
	}
	
	/* -----------------------------------------------------------------------------------
	 * GUI CONTROLLER DISPLAY
	 */
	
	public void Update()
	{
		if (debug)
		{
			/* UPDATE CONTROLERS */
			// IMPORTANT! update the controllers here for best results
			OuyaInput.UpdateControllers();
		
			/* CONSOLE TRIGGER AND D-PAD EVENTS */
			if (OuyaInput.GetButtonDown(OuyaButton.LT, observedPlayer)) Debug.Log("LT down event");
			if (OuyaInput.GetButtonUp(OuyaButton.LT, observedPlayer)) Debug.Log("LT up event");
			if (OuyaInput.GetButtonDown(OuyaButton.RT, observedPlayer)) Debug.Log("RT down event");
			if (OuyaInput.GetButtonUp(OuyaButton.RT, observedPlayer)) Debug.Log("RT up event");
			if (OuyaInput.GetButtonUp(OuyaButton.DU, observedPlayer)) Debug.Log("DU up event");
			if (OuyaInput.GetButtonDown(OuyaButton.DU, observedPlayer)) Debug.Log("DU down event");
			if (OuyaInput.GetButtonUp(OuyaButton.DD, observedPlayer)) Debug.Log("DD up event");
			if (OuyaInput.GetButtonDown(OuyaButton.DD, observedPlayer)) Debug.Log("DD down event");
			if (OuyaInput.GetButtonUp(OuyaButton.DR, observedPlayer)) Debug.Log("DR up event");
			if (OuyaInput.GetButtonDown(OuyaButton.DR, observedPlayer)) Debug.Log("DR down event");
			if (OuyaInput.GetButtonUp(OuyaButton.DL, observedPlayer)) Debug.Log("DL up event");
			if (OuyaInput.GetButtonDown(OuyaButton.DL, observedPlayer)) Debug.Log("DL down event");
		}
	}
}

