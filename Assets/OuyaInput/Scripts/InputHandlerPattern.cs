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

#pragma warning disable 219

public class InputHandlerPattern : MonoBehaviour
{
	// attach this script to any GameObject
	// in most cases this sits on the object that should be controlled via input
	// this pattern example shows how to get every input from a players controller
	// there is no GUI showing the results as I wanted to make this simple, reusable and clean
	
	/* INSPECTOR */ 
	
	// do we want to scan for trigger and d-pad button events ?
	public bool continuousScan = true;
	
	// the player we want to get input for
	public OuyaPlayer player = OuyaPlayer.P01;
	
	// the type of deadzone we want to use for convenience access
	public DeadzoneType deadzoneType = DeadzoneType.CircularClip;
	
	// the size of the deadzone
	public float deadzone = 0.25f;
	public float triggerTreshold = 0.2f;
	
	
	/* -----------------------------------------------------------------------------------
	 * INITIAL SETUP
	 */
	
	public void Start()
	{
		// OPTIONAL: set button state scanning to receive input state events for trigger and d-pads
		OuyaInput.SetContinuousScanning(continuousScan);
		
		// OPTIONAL: define the deadzone if you want to use advanced joystick and trigger access
		OuyaInput.SetDeadzone(deadzoneType, deadzone);
		OuyaInput.SetTriggerThreshold(triggerTreshold);
		
		// do one controller update here to get everything started as soon as possible
		OuyaInput.UpdateControllers();
	}

	
	/* -----------------------------------------------------------------------------------
	 * UPDATE CYCLE
	 */
	
	public void Update()
	{
		/* UPDATE CONTROLERS */
		// IMPORTANT! update the controllers here for best results
		OuyaInput.UpdateControllers();
		
		/* GET VALUES FOR CONTROLLER AXES */
		
		// left joystick
		float x_Axis_LeftStick = OuyaInput.GetAxis(OuyaAxis.LX, player);
		float y_Axis_LeftStick = OuyaInput.GetAxis(OuyaAxis.LY, player);
		
		// right joystick
		float x_Axis_RightStick = OuyaInput.GetAxis(OuyaAxis.RX, player);
		float y_Axis_RightStick = OuyaInput.GetAxis(OuyaAxis.RY, player);
		
		// d-pad
		float x_Axis_DPad = OuyaInput.GetAxis(OuyaAxis.DX, player);
		float y_Axis_DPad = OuyaInput.GetAxis(OuyaAxis.DY, player);
		
		// triggers
		float axis_LeftTrigger = OuyaInput.GetAxis(OuyaAxis.LT, player);
		float axis_RightTrigger = OuyaInput.GetAxis(OuyaAxis.RT, player);
		
		// examples for deadzone clipping (we can choose between three types)
		Vector2 leftStickInput = OuyaInput.CheckDeadzoneCircular(x_Axis_LeftStick, y_Axis_LeftStick, deadzone);
		Vector2 rightStickInput = OuyaInput.CheckDeadzoneRescaled(x_Axis_RightStick, y_Axis_RightStick, deadzone);
		Vector2 dPadInput = OuyaInput.CheckDeadzoneRescaled(x_Axis_DPad, y_Axis_DPad, deadzone);
		
		/* GET ADVANCED JOYSTICK AND TRIGGER INPUT WITH DEADZONE MAPPING */
		
		// examples for easy (or precision) joystick input
		Vector2 leftJoystick = OuyaInput.GetJoystick(OuyaJoystick.LeftStick, player);
		Vector2 rightJoystick = OuyaInput.GetJoystick(OuyaJoystick.RightStick, player);
		Vector2 dPad = OuyaInput.GetJoystick(OuyaJoystick.DPad, player);
		
		// examples for easy (or precision) trigger input
		float leftTrigger = OuyaInput.GetTrigger(OuyaTrigger.Left, player);
		float rightTrigger = OuyaInput.GetTrigger(OuyaTrigger.Right, player);
		
		/* GET PRESSED STATES FOR CONTROLLER BUTTONS */
		
		// O U Y A buttons
		bool pressed_O = OuyaInput.GetButton(OuyaButton.O, player);
		bool pressed_U = OuyaInput.GetButton(OuyaButton.U, player);
		bool pressed_Y = OuyaInput.GetButton(OuyaButton.Y, player);
		bool pressed_A = OuyaInput.GetButton(OuyaButton.A, player);
		
		// joystick click down buttons
		bool pressed_LeftStick	= OuyaInput.GetButton(OuyaButton.L3, player);
		bool pressed_RightStick	= OuyaInput.GetButton(OuyaButton.R3, player);
		
		// trigger buttons
		bool pressed_LeftTrigger = OuyaInput.GetButton(OuyaButton.LT, player);
		bool pressed_RightTrigger = OuyaInput.GetButton(OuyaButton.RT, player);
		
		//shoulder buttons
		bool pressed_LeftShoulder = OuyaInput.GetButton(OuyaButton.LB, player);
		bool pressed_RightShoulder = OuyaInput.GetButton(OuyaButton.RB, player);
		
		// center buttons
		bool pressed_Start = OuyaInput.GetButton(OuyaButton.START, player);
		bool pressed_Select = OuyaInput.GetButton(OuyaButton.SELECT, player);
		bool pressed_System = OuyaInput.GetButton(OuyaButton.SYSTEM, player);
		
		/* GET DOWN EVENTS FOR CONTROLLER BUTTONS */

		// we need to have OuyaInput.SetContinuousScanning(true) in Start()
		// some controllers might work without this but we want to make sure
		if (continuousScan)
		{
			// O U Y A buttons
			bool down_O = OuyaInput.GetButtonDown(OuyaButton.O, player);
			bool down_U = OuyaInput.GetButtonDown(OuyaButton.U, player);
			bool down_Y = OuyaInput.GetButtonDown(OuyaButton.Y, player);
			bool down_A = OuyaInput.GetButtonDown(OuyaButton.A, player);
		
			// joystick click down buttons
			bool down_LeftStick	= OuyaInput.GetButtonDown(OuyaButton.L3, player);
			bool down_RightStick = OuyaInput.GetButtonDown(OuyaButton.R3, player);
		
			// trigger buttons
			bool down_LeftTrigger = OuyaInput.GetButtonDown(OuyaButton.LT, player);
			bool down_RightTrigger = OuyaInput.GetButtonDown(OuyaButton.RT, player);
			
			//shoulder buttons
			bool down_LeftShoulder = OuyaInput.GetButtonDown(OuyaButton.LB, player);
			bool down_RightShoulder = OuyaInput.GetButtonDown(OuyaButton.RB, player);
		
			// center buttons
			bool down_Start = OuyaInput.GetButtonDown(OuyaButton.START, player);
			bool down_Select = OuyaInput.GetButtonDown(OuyaButton.SELECT, player);
			bool down_System = OuyaInput.GetButtonDown(OuyaButton.SYSTEM, player);
		}
		
		/* GET UP (RELEASE) EVENTS FOR CONTROLLER BUTTONS */

		// we need to have OuyaInput.SetContinuousScanning(true) in Start()
		// some controllers might work without this but we want to make sure
		if (continuousScan)
		{
			// O U Y A buttons
			bool up_O = OuyaInput.GetButtonUp(OuyaButton.O, player);
			bool up_U = OuyaInput.GetButtonUp(OuyaButton.U, player);
			bool up_Y = OuyaInput.GetButtonUp(OuyaButton.Y, player);
			bool up_A = OuyaInput.GetButtonUp(OuyaButton.A, player);
		
			// joystick click down buttons
			bool up_LeftStick = OuyaInput.GetButtonUp(OuyaButton.L3, player);
			bool up_RightStick = OuyaInput.GetButtonUp(OuyaButton.R3, player);
		
			// trigger buttons
			bool up_LeftTrigger = OuyaInput.GetButtonUp(OuyaButton.LT, player);
			bool up_RightTrigger = OuyaInput.GetButtonUp(OuyaButton.RT, player);
			
			//shoulder buttons
			bool up_LeftShoulder = OuyaInput.GetButtonUp(OuyaButton.LB, player);
			bool up_RightShoulder = OuyaInput.GetButtonUp(OuyaButton.RB, player);
		
			// center buttons
			bool up_Start = OuyaInput.GetButtonUp(OuyaButton.START, player);
			bool up_Select = OuyaInput.GetButtonUp(OuyaButton.SELECT, player);
			bool up_System = OuyaInput.GetButtonUp(OuyaButton.SYSTEM, player);
		}
	}
}

