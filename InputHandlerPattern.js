// attach this script to any GameObject
// in most cases this sits on the object that should be controlled via input
// this pattern example shows how to get every input from a players controller
// there is no GUI showing the results as I wanted to make this simple, reusable and clean

/* INSPECTOR */ 

// do we want to scan for trigger and d-pad button events ?
var continuousScan : boolean  = true;
	
// the player we want to get input for
var player : OuyaPlayer = OuyaPlayer.P01;

// the platform we are working on (installation editor)
var editorWorkPlatform : EditorWorkPlatform = EditorWorkPlatform.MacOS;

// the type of deadzone we want to use for convenience access
var deadzoneType : DeadzoneType = DeadzoneType.CircularClip;

// the size of the deadzone
var deadzone : float = 0.25f;
var triggerTreshold : float = 0.2f;


/* -----------------------------------------------------------------------------------
 * INITIAL SETUP
 */

function Start()
{
	// set the editor platform to get correct input while testing in the editor
	OuyaInput.SetEditorPlatform(editorWorkPlatform);
	
	// OPTIONAL: set button state scanning to receive input state events for trigger and d-pads
	OuyaInput.SetContinuousScanning(continuousScan);
	
	// OPTIONAL: define the deadzone if you want to use advanced joystick and trigger access
	OuyaInput.SetDeadzone(deadzoneType, deadzone);
	OuyaInput.SetTriggerTreshold(triggerTreshold);
	
	// do one controller update here to get everything started as soon as possible
	OuyaInput.UpdateControllers();
}


/* -----------------------------------------------------------------------------------
 * UPDATE CYCLE
 */

function Update()
{
	/* UPDATE CONTROLERS */
	// IMPORTANT! update the controllers here for best results
	OuyaInput.UpdateControllers();
	
	/* GET VALUES FOR CONTROLLER AXES */
	
	// left joystick
	var x_Axis_LeftStick : float = OuyaInput.GetAxis(OuyaAxis.LX, player);
	var y_Axis_LeftStick : float = OuyaInput.GetAxis(OuyaAxis.LY, player);
	
	// right joystick
	var x_Axis_RightStick : float = OuyaInput.GetAxis(OuyaAxis.RX, player);
	var y_Axis_RightStick : float = OuyaInput.GetAxis(OuyaAxis.RY, player);
	
	// d-pad
	var x_Axis_DPad : float= OuyaInput.GetAxis(OuyaAxis.DX, player);
	var y_Axis_DPad : float= OuyaInput.GetAxis(OuyaAxis.DY, player);
	
	// triggers
	var axis_LeftTrigger : float  = OuyaInput.GetAxis(OuyaAxis.LT, player);
	var axis_RightTrigger : float= OuyaInput.GetAxis(OuyaAxis.RT, player);
	
	// examples for deadzone clipping (we can choose between three types)
	var leftStickInput : Vector2 = OuyaInput.CheckDeadzoneCircular(x_Axis_LeftStick, y_Axis_LeftStick, deadzone);
	var rightStickInput : Vector2= OuyaInput.CheckDeadzoneRescaled(x_Axis_RightStick, y_Axis_RightStick, deadzone);
	var dPadInput : Vector2= OuyaInput.CheckDeadzoneRescaled(x_Axis_DPad, y_Axis_DPad, deadzone);
	
	/* GET ADVANCED JOYSTICK AND TRIGGER INPUT WITH DEADZONE MAPPING */
	
	// examples for easy (or precision) joystick input
	var leftJoystick : Vector2 = OuyaInput.GetJoystick(OuyaJoystick.LeftStick, player);
	var rightKoystick : Vector2 = OuyaInput.GetJoystick(OuyaJoystick.RightStick, player);
	var dPad : Vector2 = OuyaInput.GetJoystick(OuyaJoystick.DPad, player);
	
	// examples for easy (or precision) trigger input
	var leftTrigger : float = OuyaInput.GetTrigger(OuyaTrigger.Left, player);
	var rightTrigger : float = OuyaInput.GetTrigger(OuyaTrigger.Right, player);
	
	/* GET PRESSED STATES FOR CONTROLLER BUTTONS */
	
	// O U Y A buttons
	var pressed_O : boolean = OuyaInput.GetButton(OuyaButton.O, player);
	var pressed_U : boolean = OuyaInput.GetButton(OuyaButton.U, player);
	var pressed_Y : boolean = OuyaInput.GetButton(OuyaButton.Y, player);
	var pressed_A : boolean = OuyaInput.GetButton(OuyaButton.A, player);
	
	// joystick click down buttons
	var pressed_LeftStick : boolean = OuyaInput.GetButton(OuyaButton.L3, player);
	var pressed_RightStick : boolean = OuyaInput.GetButton(OuyaButton.R3, player);
	
	// trigger buttons
	var pressed_LeftTrigger : boolean = OuyaInput.GetButton(OuyaButton.LT, player);
	var pressed_RightTrigger : boolean = OuyaInput.GetButton(OuyaButton.RT, player);
	
	// center buttons
	var pressed_Start : boolean = OuyaInput.GetButton(OuyaButton.START, player);
	var pressed_Select : boolean = OuyaInput.GetButton(OuyaButton.SELECT, player);
	var pressed_System : boolean = OuyaInput.GetButton(OuyaButton.SYSTEM, player);
	
	/* GET DOWN EVENTS FOR CONTROLLER BUTTONS */

	// we need to have OuyaInput.SetContinuousScanning(true) in Start()
	// some controllers might work without this but we want to make sure
	if (continuousScan)
	{
		// O U Y A buttons
		var down_O : boolean = OuyaInput.GetButtonDown(OuyaButton.O, player);
		var down_U : boolean = OuyaInput.GetButtonDown(OuyaButton.U, player);
		var down_Y : boolean= OuyaInput.GetButtonDown(OuyaButton.Y, player);
		var down_A : boolean= OuyaInput.GetButtonDown(OuyaButton.A, player);
	
		// joystick click down buttons
		var down_LeftStick : boolean = OuyaInput.GetButtonDown(OuyaButton.L3, player);
		var down_RightStick : boolean = OuyaInput.GetButtonDown(OuyaButton.R3, player);
	
		// trigger buttons
		var down_LeftTrigger : boolean = OuyaInput.GetButtonDown(OuyaButton.LT, player);
		var down_RightTrigger : boolean = OuyaInput.GetButtonDown(OuyaButton.RT, player);
	
		// center buttons
		var down_Start : boolean = OuyaInput.GetButtonDown(OuyaButton.START, player);
		var down_Select : boolean = OuyaInput.GetButtonDown(OuyaButton.SELECT, player);
		var down_System : boolean = OuyaInput.GetButtonDown(OuyaButton.SYSTEM, player);
	}
	
	/* GET UP (RELEASE) EVENTS FOR CONTROLLER BUTTONS */

	// we need to have OuyaInput.SetContinuousScanning(true) in Start()
	// some controllers might work without this but we want to make sure
	if (continuousScan)
	{
		// O U Y A buttons
		var up_O : boolean = OuyaInput.GetButtonUp(OuyaButton.O, player);
		var up_U : boolean = OuyaInput.GetButtonUp(OuyaButton.U, player);
		var up_Y : boolean = OuyaInput.GetButtonUp(OuyaButton.Y, player);
		var up_A : boolean = OuyaInput.GetButtonUp(OuyaButton.A, player);
	
		// joystick click down buttons
		var up_LeftStick : boolean = OuyaInput.GetButtonUp(OuyaButton.L3, player);
		var up_RightStick : boolean= OuyaInput.GetButtonUp(OuyaButton.R3, player);
	
		// trigger buttons
		var up_LeftTrigger : boolean = OuyaInput.GetButtonUp(OuyaButton.LT, player);
		var up_RightTrigger : boolean = OuyaInput.GetButtonUp(OuyaButton.RT, player);
	
		// center buttons
		var up_Start : boolean = OuyaInput.GetButtonUp(OuyaButton.START, player);
		var up_Select : boolean = OuyaInput.GetButtonUp(OuyaButton.SELECT, player);
		var up_System :  boolean= OuyaInput.GetButtonUp(OuyaButton.SYSTEM, player);
	}
}