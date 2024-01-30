namespace HIDReportParserHowogairlerejaceBemnayferedewall;

static class ReportUsage
{
    /* Usage Page: Generic Desktop (=0x01U)
    ** Sys: System
    */
    public const uint GD_Pointer = 0x01U;
    public const uint GD_Mouse = 0x02U;
    /* Reserved */
    public const uint GD_Joystick = 0x04U;
    public const uint GD_Game_Pad = 0x05U;
    public const uint GD_Keyboard = 0x06U;
    public const uint GD_Keypad = 0x07U;
    public const uint GD_Multiaxis_Controller = 0x08U;
    public const uint GD_Tablet_PC_Sys_Controls = 0x09U;
    /* Reserved */
    public const uint GD_X = 0x30U;
    public const uint GD_Y = 0x31U;
    public const uint GD_Z = 0x32U;
    public const uint GD_Rx = 0x33U;
    public const uint GD_Ry = 0x34U;
    public const uint GD_Rz = 0x35U;
    public const uint GD_Slider = 0x36U;
    public const uint GD_Dial = 0x37U;
    public const uint GD_Wheel = 0x38U;
    public const uint GD_Hat_Switch = 0x39U;
    public const uint GD_Counted_Buffer = 0x3AU;
    public const uint GD_Byte_Count = 0x3BU;
    public const uint GD_Motion_Wakeup = 0x3CU;
    public const uint GD_Start = 0x3DU;
    public const uint GD_Select = 0x3EU;
    /* Reserved */
    public const uint GD_Vx = 0x40U;
    public const uint GD_Vy = 0x41U;
    public const uint GD_Vz = 0x42U;
    public const uint GD_Vbrx = 0x43U;
    public const uint GD_Vbry = 0x44U;
    public const uint GD_Vbrz = 0x45U;
    public const uint GD_Vno = 0x46U;
    public const uint GD_Feature_Notification = 0x47U;
    public const uint GD_Resolution_Multiplier = 0x48U;
    /* Reserved */
    public const uint GD_Sys_Control = 0x80U;
    public const uint GD_Sys_Power_Down = 0x81U;
    public const uint GD_Sys_Sleep = 0x82U;
    public const uint GD_Sys_Wake_Up = 0x83U;
    public const uint GD_Sys_Context_Menu = 0x84U;
    public const uint GD_Sys_Main_Menu = 0x85U;
    public const uint GD_Sys_App_Menu = 0x86U;
    public const uint GD_Sys_Menu_Help = 0x87U;
    public const uint GD_Sys_Menu_Exit = 0x88U;
    public const uint GD_Sys_Menu_Select = 0x89U;
    public const uint GD_Sys_Menu_Right = 0x8AU;
    public const uint GD_Sys_Menu_Left = 0x8BU;
    public const uint GD_Sys_Menu_Up = 0x8CU;
    public const uint GD_Sys_Menu_Down = 0x8DU;
    public const uint GD_Sys_Cold_Restart = 0x8EU;
    public const uint GD_Sys_Warm_Restart = 0x8FU;
    public const uint GD_D_pad_Up = 0x90U;
    public const uint GD_D_pad_Down = 0x91U;
    public const uint GD_D_pad_Right = 0x92U;
    public const uint GD_D_pad_Left = 0x93U;
    /* Reserved */
    public const uint GD_Sys_Dock = 0xA0U;
    public const uint GD_Sys_Undock = 0xA1U;
    public const uint GD_Sys_Setup = 0xA2U;
    public const uint GD_Sys_Break = 0xA3U;
    public const uint GD_Sys_Debugger_Break = 0xA4U;
    public const uint GD_Application_Break = 0xA5U;
    public const uint GD_Application_Debugger_Break = 0xA6U;
    public const uint GD_Sys_Speaker_Mute = 0xA7U;
    public const uint GD_Sys_Hibernate = 0xA8U;
    /* Reserved */
    public const uint GD_Sys_Display_Invert = 0xB0U;
    public const uint GD_Sys_Display_Internal = 0xB1U;
    public const uint GD_Sys_Display_External = 0xB2U;
    public const uint GD_Sys_Display_Both = 0xB3U;
    public const uint GD_Sys_Display_Dual = 0xB4U;
    public const uint GD_Sys_Display_Toggle = 0xB5U;
    public const uint GD_Sys_Display_Swap = 0xB6U;
    public const uint GD_Sys_Display_LCD_Autoscale = 0xB7U;
    /* Reserved */


    /* Usage Page: Simulation Controls Page (=0x02U)
    ** SimuDev: Simulation Device
    */
    public const uint SC_SimuDev_Flight = 0x01U;
    public const uint SC_SimuDev_Automobile = 0x02U;
    public const uint SC_SimuDev_Tank = 0x03U;
    public const uint SC_SimuDev_Spaceship = 0x04U;
    public const uint SC_SimuDev_Submarine = 0x05U;
    public const uint SC_SimuDev_Sailing = 0x06U;
    public const uint SC_SimuDev_Motorcycle = 0x07U;
    public const uint SC_SimuDev_Sports = 0x08U;
    public const uint SC_SimuDev_Airplane = 0x09U;
    public const uint SC_SimuDev_Helicopter = 0x0AU;
    public const uint SC_SimuDev_MagicCarpet = 0x0BU;
    public const uint SC_SimuDev_Bicycle = 0x0CU;
    /* Reserved */
    public const uint SC_Flight_Control_Stick = 0x20U;
    public const uint SC_Flilght_Stick = 0x21U;
    public const uint SC_Cyclic_Control = 0x22U;
    public const uint SC_Cyclic_Trim = 0x23U;
    public const uint SC_Flight_Yoke = 0x24U;
    public const uint SC_Track_Control = 0x25U;
    /* Reserved */
    public const uint SC_Aileron = 0xB0U;
    public const uint SC_Aileron_Trim = 0xB1U;
    public const uint SC_Anti_Torque_Control = 0xB2U;
    public const uint SC_Autopilot_Enable = 0xB3U;
    public const uint SC_Chaff_Release = 0xB4U;
    public const uint SC_Collective_Control = 0xB5U;
    public const uint SC_Dive_Brake = 0xB6U;
    public const uint SC_Electronic_Countermeasures = 0xB7U;
    public const uint SC_Elevator = 0xB8U;
    public const uint SC_Elevator_Trim = 0xB9U;
    public const uint SC_Rudder = 0xBAU;
    public const uint SC_Throttle = 0xBBU;
    public const uint SC_Flight_Communications = 0xBCU;
    public const uint SC_Flare_Release = 0xBDU;
    public const uint SC_Landing_Gear = 0xBEU;
    public const uint SC_Toe_Brake = 0xBFU;
    public const uint SC_Trigger = 0xC0U;
    public const uint SC_Weapons_Arm = 0xC1U;
    public const uint SC_Weapons_Select = 0xC2U;
    public const uint SC_Wing_Flaps = 0xC3U;
    public const uint SC_Accelerator = 0xC4U;
    public const uint SC_Brake = 0xC5U;
    public const uint SC_Clutch = 0xC6U;
    public const uint SC_Shifter = 0xC7U;
    public const uint SC_Steering = 0xC8U;
    public const uint SC_Turret_Direction = 0xC9U;
    public const uint SC_Barrel_Elevation = 0xCAU;
    public const uint SC_Dive_Plane = 0xCBU;
    public const uint SC_Ballast = 0xCCU;
    public const uint SC_Bicycle_Crank = 0xCDU;
    public const uint SC_Handle_Bars = 0xCEU;
    public const uint SC_Front_Brake = 0xCFU;
    public const uint SC_Rear_Brake = 0xD0U;
    /* Reserved */


    /* Usage Page: VR Controls Page (=0x03)
    ** 
    */
    public const uint VR_Belt = 0x01U;
    public const uint VR_Body_Suit = 0x02U;
    public const uint VR_Flexor = 0x03U;
    public const uint VR_Glove = 0x04U;
    public const uint VR_Head_Tracker = 0x05U;
    public const uint VR_Head_Mounted_Display = 0x06U;
    public const uint VR_Hand_Tracker = 0x07U;
    public const uint VR_Oculometer = 0x08U;
    public const uint VR_Vest = 0x09U;
    public const uint VR_Animatronic_Device = 0x0AU;
    /* Reserved */
    public const uint VR_Stereo_Enable = 0x20U;
    public const uint VR_Display_Enable = 0x21U;
    /* Reserved */


    /* Usage Page: Sport Controls Page (=0x04)
    ** 
    */
    public const uint SpC_Baseball_Bat = 0x01U;
    public const uint SpC_Golf_Club = 0x02U;
    public const uint SpC_Rowing_Machine = 0x03U;
    public const uint SpC_Treadmill = 0x04U;
    /* Reserved */
    public const uint SpC_Oar = 0x30U;
    public const uint SpC_Slope = 0x31U;
    public const uint SpC_Rate = 0x32U;
    public const uint SpC_Stick_Speed = 0x33U;
    public const uint SpC_Stick_Face_Angle = 0x34U;
    public const uint SpC_Stick_HeelorToe = 0x35U;
    public const uint SpC_Stick_Follow_Through = 0x36U;
    public const uint SpC_Stick_Tempo = 0x37U;
    public const uint SpC_Stick_Type = 0x38U;
    public const uint SpC_Stick_Height = 0x39U;
    /* Reserved */
    public const uint SpC_Putter = 0x50U;
    public const uint SpC_Iron_1 = 0x51U;
    public const uint SpC_Iron_2 = 0x52U;
    public const uint SpC_Iron_3 = 0x53U;
    public const uint SpC_Iron_4 = 0x54U;
    public const uint SpC_Iron_5 = 0x55U;
    public const uint SpC_Iron_6 = 0x56U;
    public const uint SpC_Iron_7 = 0x57U;
    public const uint SpC_Iron_8 = 0x58U;
    public const uint SpC_Iron_9 = 0x59U;
    public const uint SpC_Iron_10 = 0x5AU;
    public const uint SpC_Iron_11 = 0x5BU;
    public const uint SpC_Sand_Wedge = 0x5CU;
    public const uint SpC_Loft_Wedge = 0x5DU;
    public const uint SpC_Power_Wedge = 0x5EU;
    public const uint SpC_Wood_1 = 0x5FU;
    public const uint SpC_Wood_3 = 0x60U;
    public const uint SpC_Wood_5 = 0x61U;
    public const uint SpC_Wood_7 = 0x62U;
    public const uint SpC_Wood_9 = 0x63U;
    /* Reserved */


    /* Usage Page: Game Controls Page (=0x05)
    */
    public const uint GC_3D_Game_Controller = 0x01U;
    public const uint GC_Pinball_Device = 0x02U;
    public const uint GC_Gun_Device = 0x03U;
    /* Reserved */
    public const uint GC_Point_of_View = 0x20U;
    public const uint GC_Turn_Right_Left = 0x21U;
    public const uint GC_Pitch_Forward_Backward = 0x22U;
    public const uint GC_Roll_Right_Left = 0x23U;
    public const uint GC_Move_Right_Left = 0x24U;
    public const uint GC_Move_Forward_Backward = 0x25U;
    public const uint GC_Move_Up_Down = 0x26U;
    public const uint GC_Lean_Right_Left = 0x27U;
    public const uint GC_Lean_Forward_Backward = 0x28U;
    public const uint GC_Height_of_POV = 0x29U;
    public const uint GC_Flipper = 0x2AU;
    public const uint GC_Secondary_Flipper = 0x2BU;
    public const uint GC_Bump = 0x2CU;
    public const uint GC_New_Game = 0x2DU;
    public const uint GC_Shoot_Ball = 0x2EU;
    public const uint GC_Player = 0x2FU;
    public const uint GC_Gun_Bolt = 0x30U;
    public const uint GC_Gun_Clip = 0x31U;
    public const uint GC_Gun_Selector = 0x32U;
    public const uint GC_Gun_Single_Shot = 0x33U;
    public const uint GC_Gun_Burst = 0x34U;
    public const uint GC_Gun_Automatic = 0x35U;
    public const uint GC_Gun_Safety = 0x36U;
    public const uint GC_Gamepad_Fire_Jump = 0x37U;
    /* Reserved */
    public const uint GC_Gamepad_Trigger = 0x39U;
    /* Reserved */


    /* Usage Page: Generic Device Controls (=0x06)
    ** SC: Security Code
    */
    /* =0x00 Undefined */
    /* Reserved */
    public const uint GDC_Battery_Strength = 0x20U;
    public const uint GDC_Wireless_Channel = 0x21U;
    public const uint GDC_Wireless_ID = 0x22U;
    public const uint GDC_Discover_Wireless_Ctrl = 0x23U;
    public const uint GDC_SC_Character_Entered = 0x24U;
    public const uint GDC_SC_Character_Cleared = 0x25U;
    public const uint GDC_SC_Cleared = 0x26U;
    /* Reserved */

    /* Usage Page: Keyboard/Keypad (=0x07)
    */
    //TODO:

    /* Usage Page: LED (=0x08)
    */
    public const uint LED_Num_Lock = 0x01U;
    public const uint LED_Caps_Lock = 0x02U;
    public const uint LED_Scroll_Lock = 0x03U;
    public const uint LED_Compose = 0x04U;
    public const uint LED_Kana = 0x05U;
    public const uint LED_Power = 0x06U;
    public const uint LED_Shift = 0x07U;
    public const uint LED_Donot_Disturb = 0x08U;
    public const uint LED_Mute = 0x09U;
    public const uint LED_Tone_Enable = 0x0AU;
    public const uint LED_High_Cut_Filter = 0x0BU;
    public const uint LED_Low_Cut_Filter = 0x0CU;
    public const uint LED_Equalizer_Enable = 0x0DU;
    public const uint LED_Sound_Field_On = 0x0EU;
    public const uint LED_Surround_On = 0x0FU;
    public const uint LED_Repeat = 0x10U;
    public const uint LED_Stereo = 0x11U;
    public const uint LED_Sampling_Rate_Detect = 0x12U;
    public const uint LED_Spinning = 0x13U;
    public const uint LED_CAV = 0x14U;
    public const uint LED_CLV = 0x15U;
    public const uint LED_Recording_Format_Detect = 0x16U;
    public const uint LED_Off_Hook = 0x17U;
    public const uint LED_Ring = 0x18U;
    public const uint LED_Message_Waiting = 0x19U;
    public const uint LED_Data_Mode = 0x1AU;
    public const uint LED_Battery_Operation = 0x1BU;
    public const uint LED_Battery_OK = 0x1CU;
    public const uint LED_Battery_Low = 0x1DU;
    public const uint LED_Speaker = 0x1EU;
    public const uint LED_Head_Set = 0x1FU;
    public const uint LED_Hold = 0x20U;
    public const uint LED_Microphone = 0x21U;
    public const uint LED_Coverage = 0x22U;
    public const uint LED_Night_Mode = 0x23U;
    public const uint LED_Send_Calls = 0x24U;
    public const uint LED_Call_Pickup = 0x25U;
    public const uint LED_Conference = 0x26U;
    public const uint LED_Standby = 0x27U;
    public const uint LED_Camera_On = 0x28U;
    public const uint LED_Camera_Off = 0x29U;
    public const uint LED_On_Line = 0x2AU;
    public const uint LED_Off_Line = 0x2BU;
    public const uint LED_Busy = 0x2CU;
    public const uint LED_Ready = 0x2DU;
    public const uint LED_Paper_Out = 0x2EU;
    public const uint LED_Paper_Jam = 0x2FU;
    public const uint LED_Remote = 0x30U;
    public const uint LED_Forward = 0x31U;
    public const uint LED_Reverse = 0x32U;
    public const uint LED_Stop = 0x33U;
    public const uint LED_Rewind = 0x34U;
    public const uint LED_Fast_Forward = 0x35U;
    public const uint LED_Play = 0x36U;
    public const uint LED_Pause = 0x37U;
    public const uint LED_Record = 0x38U;
    public const uint LED_Error = 0x39U;
    public const uint LED_Selected_Indicator = 0x3AU;
    public const uint LED_In_Use_Indicator = 0x3BU;
    public const uint LED_Multi_Mode_Indicator = 0x3CU;
    public const uint LED_Indicator_On = 0x3DU;
    public const uint LED_Indicator_Flash = 0x3EU;
    public const uint LED_Indicator_Slow_Blink = 0x3FU;
    public const uint LED_Indicator_Fast_Blink = 0x40U;
    public const uint LED_Indicator_Off = 0x41U;
    public const uint LED_Flash_On_Time = 0x42U;
    public const uint LED_Slow_Blink_On_Time = 0x43U;
    public const uint LED_Slow_Blink_Off_Time = 0x44U;
    public const uint LED_Fast_Blink_On_Time = 0x45U;
    public const uint LED_Fast_Blink_Off_Time = 0x46U;
    public const uint LED_Usage_Indicator_Color = 0x47U;
    public const uint LED_Indicator_Red = 0x48U;
    public const uint LED_Indicator_Green = 0x49U;
    public const uint LED_Indicator_Amber = 0x4AU;
    public const uint LED_Generic_Indicator = 0x4BU;
    public const uint LED_Sys_Suspend = 0x4CU;
    public const uint LED_External_Power_Connected = 0x4DU;
    /* Reserved */


    /* Usage Page: Button (=0x09)
    ** ID N is Button N 
    */

    /* Usage Page: Ordinal (=0x0A)
    ** ID N is Instance N
    */

    /* Usage Page: Telephony Device (=0x0B)
    */
    public const uint TD_Phone = 0x01U;
    public const uint TD_Answering_Machine = 0x02U;
    public const uint TD_Message_Controls = 0x03U;
    public const uint TD_Handset = 0x04U;
    public const uint TD_Headset = 0x05U;
    public const uint TD_Telephony_Key_Pad = 0x06U;
    public const uint TD_Programmable_Button = 0x07U;
    /* Reserved */
    public const uint TD_Hook_Switch = 0x20U;
    public const uint TD_Flash = 0x21U;
    public const uint TD_Feature = 0x22U;
    public const uint TD_Hold = 0x23U;
    public const uint TD_Redial = 0x24U;
    public const uint TD_Transfer = 0x25U;
    public const uint TD_Drop = 0x26U;
    public const uint TD_Park = 0x27U;
    public const uint TD_Forward_Calls = 0x28U;
    public const uint TD_Alternate_Function = 0x29U;
    public const uint TD_Line = 0x2AU;
    public const uint TD_Speaker_Phone = 0x2BU;
    public const uint TD_Conference = 0x2CU;
    public const uint TD_Ring_Enable = 0x2DU;
    public const uint TD_Ring_Select = 0x2EU;
    public const uint TD_Phone_Mute = 0x2FU;
    public const uint TD_Caller_ID = 0x30U;
    public const uint TD_Send = 0x31U;
    /* Reserved */
    public const uint TD_Speed_Dial = 0x50U;
    public const uint TD_Store_Number = 0x51U;
    public const uint TD_Recall_Number = 0x52U;
    public const uint TD_Phone_Directory = 0x53U;
    /* Reserved */
    public const uint TD_Voice_Mail = 0x70U;
    public const uint TD_Screen_Calls = 0x71U;
    public const uint TD_Do_Not_Disturb = 0x72U;
    public const uint TD_Message = 0x73U;
    public const uint TD_Answer_On_Off = 0x74U;
    /* Reserved */
    public const uint TD_Inside_Dial_Tone = 0x90U;
    public const uint TD_Outside_Dial_Tone = 0x91U;
    public const uint TD_Inside_Ring_Tone = 0x92U;
    public const uint TD_Outside_Ring_Tone = 0x93U;
    public const uint TD_Priority_Ring_Tone = 0x94U;
    public const uint TD_Inside_Ringback = 0x95U;
    public const uint TD_Priority_Ringback = 0x96U;
    public const uint TD_Line_Busy_Tone = 0x97U;
    public const uint TD_Reorder_Tone = 0x98U;
    public const uint TD_Call_Waiting_Tone = 0x99U;
    public const uint TD_Confirmation_Tone_1 = 0x9AU;
    public const uint TD_Confirmation_Tone_2 = 0x9BU;
    public const uint TD_Tones_Off = 0x9CU;
    public const uint TD_Outside_Ringback = 0x9DU;
    public const uint TD_Ringer = 0x9EU;
    /* Reserved */
    public const uint TD_Phone_Key_0 = 0xB0U;
    public const uint TD_Phone_Key_1 = 0xB1U;
    public const uint TD_Phone_Key_2 = 0xB2U;
    public const uint TD_Phone_Key_3 = 0xB3U;
    public const uint TD_Phone_Key_4 = 0xB4U;
    public const uint TD_Phone_Key_5 = 0xB5U;
    public const uint TD_Phone_Key_6 = 0xB6U;
    public const uint TD_Phone_Key_7 = 0xB7U;
    public const uint TD_Phone_Key_8 = 0xB8U;
    public const uint TD_Phone_Key_9 = 0xB9U;
    public const uint TD_Phone_Key_Star = 0xBAU;
    public const uint TD_Phone_Key_Pound = 0xBBU;
    public const uint TD_Phone_Key_A = 0xBCU;
    public const uint TD_Phone_Key_B = 0xBDU;
    public const uint TD_Phone_Key_C = 0xBEU;
    public const uint TD_Phone_Key_D = 0xBFU;
    /* Reserved */

    /* Usage Page: Consumer (=0x0C)
    ** App      - Application
    ** Btn      - Button
    ** Ctrl     - Control
    ** Incr/Decr - Increase/Decrease
    ** Prog     - Programmable
    ** Sel      - Select
    ** Sys      - System
    ** ILL      - Illumination
    */

    public const uint UC_Consumer_Ctrl = 0x1U;
    public const uint UC_Numeric_Keypad = 0x2U;
    public const uint UC_Prog_Btns = 0x3U;
    public const uint UC_Mic = 0x4U;
    public const uint UC_Headphone = 0x5U;
    public const uint UC_Graphic_Equalizer = 0x6U;
    /* Reserved */
    public const uint UC_Add_10 = 0x20U;
    public const uint UC_Add_100 = 0x21U;
    public const uint UC_AM_PM = 0x22U;
    /* Reserved */
    public const uint UC_Power = 0x30U;
    public const uint UC_Reset = 0x31U;
    public const uint UC_Sleep = 0x32U;
    public const uint UC_Sleep_After = 0x33U;
    public const uint UC_Sleep_Mode = 0x34U;
    public const uint UC_ILL = 0x35U;
    public const uint UC_Function_Buttons = 0x36U;
    /* Reserved */
    public const uint UC_Menu = 0x40U;
    public const uint UC_Menu_Pick = 0x41U;
    public const uint UC_Menu_Up = 0x42U;
    public const uint UC_Menu_Down = 0x43U;
    public const uint UC_Menu_Left = 0x44U;
    public const uint UC_Menu_Right = 0x45U;
    public const uint UC_Menu_Escape = 0x46U;
    public const uint UC_Menu_Value_Incr = 0x47U;
    public const uint UC_Menu_Value_Decr = 0x48U;
    /* Reserved */
    public const uint UC_Data_On_Screen = 0x60U;
    public const uint UC_Closed_Caption = 0x61U;
    public const uint UC_Closed_Caption_Sel = 0x62U;
    public const uint UC_VCR_TV = 0x63U;
    public const uint UC_Broadcast_Mode = 0x64U;
    public const uint UC_Snapshot = 0x65U;
    public const uint UC_Still = 0x66U;
    /* Reserved */
    public const uint UC_Selion = 0x80U;
    public const uint UC_Assign_Selion = 0x81U;
    public const uint UC_Mode_Step = 0x82U;
    public const uint UC_Recall_Last = 0x83U;
    public const uint UC_Enter_Channel = 0x84U;
    public const uint UC_Order_Movie = 0x85U;
    public const uint UC_Channel = 0x86U;
    public const uint UC_Media_Selion = 0x87U;
    public const uint UC_Media_Sel_Computer = 0x88U;
    public const uint UC_Media_Sel_TV = 0x89U;
    public const uint UC_Media_Sel_WWW = 0x8AU;
    public const uint UC_Media_Sel_DVD = 0x8BU;
    public const uint UC_Media_Sel_Telephone = 0x8CU;
    public const uint UC_Media_Sel_Program_Guide = 0x8DU;
    public const uint UC_Media_Sel_Video_Phone = 0x8EU;
    public const uint UC_Media_Sel_Games = 0x8FU;
    public const uint UC_Media_Sel_Messages = 0x90U;
    public const uint UC_Media_Sel_CD = 0x91U;
    public const uint UC_Media_Sel_VCR = 0x92U;
    public const uint UC_Media_Sel_Tuner = 0x93U;
    public const uint UC_Quit = 0x94U;
    public const uint UC_Help = 0x95U;
    public const uint UC_Media_Sel_Tape = 0x96U;
    public const uint UC_Media_Sel_Cable = 0x97U;
    public const uint UC_Media_Sel_Satellite = 0x98U;
    public const uint UC_Media_Sel_Security = 0x99U;
    public const uint UC_Media_Sel_Home = 0x9AU;
    public const uint UC_Media_Sel_Call = 0x9BU;
    public const uint UC_Channel_Incr = 0x9CU;
    public const uint UC_Channel_Decr = 0x9DU;
    public const uint UC_Media_Sel_SAP = 0x9EU;
    /* Reserved */
    public const uint UC_VCR_Plus = 0xA0U;
    public const uint UC_Once = 0xA1U;
    public const uint UC_Daily = 0xA2U;
    public const uint UC_Weekly = 0xA3U;
    public const uint UC_Monthly = 0xA4U;
    /* Reserved */
    public const uint UC_Play = 0xB0U;
    public const uint UC_Pause = 0xB1U;
    public const uint UC_Record = 0xB2U;
    public const uint UC_Fast_Forward = 0xB3U;
    public const uint UC_Rewind = 0xB4U;
    public const uint UC_Scan_Next_Track = 0xB5U;
    public const uint UC_Scan_Previous_Track = 0xB6U;
    public const uint UC_Stop = 0xB7U;
    public const uint UC_Eject = 0xB8U;
    public const uint UC_Random_Play = 0xB9U;
    public const uint UC_Sel_Disc = 0xBAU;
    public const uint UC_Enter_Disc = 0xBBU;
    public const uint UC_Repeat = 0xBCU;
    public const uint UC_Tracking = 0xBDU;
    public const uint UC_Track_Normal = 0xBEU;
    public const uint UC_Slow_Tracking = 0xBFU;
    public const uint UC_Frame_Forward = 0xC0U;
    public const uint UC_Frame_Back = 0xC1U;
    public const uint UC_Mark = 0xC2U;
    public const uint UC_Clear_Mark = 0xC3U;
    public const uint UC_Repeat_From_Mark = 0xC4U;
    public const uint UC_Return_To_Mark = 0xC5U;
    public const uint UC_Search_Mark_Forward = 0xC6U;
    public const uint UC_Search_Mark_Backward = 0xC7U;
    public const uint UC_Counter_Reset = 0xC8U;
    public const uint UC_Show_Counter = 0xC9U;
    public const uint UC_Tracking_Incr = 0xCAU;
    public const uint UC_Tracking_Decr = 0xCBU;
    public const uint UC_Stop_Eject = 0xCCU;
    public const uint UC_Play_Pause = 0xCDU;
    public const uint UC_Play_Skip = 0xCEU;
    /* Reserved */
    public const uint UC_Volume = 0xE0U;
    public const uint UC_Balance = 0xE1U;
    public const uint UC_Mute = 0xE2U;
    public const uint UC_Bass = 0xE3U;
    public const uint UC_Treble = 0xE4U;
    public const uint UC_Bass_Boost = 0xE5U;
    public const uint UC_Surround_Mode = 0xE6U;
    public const uint UC_Loudness = 0xE7U;
    public const uint UC_MPX = 0xE8U;
    public const uint UC_Volume_Incr = 0xE9U;
    public const uint UC_Volume_Decr = 0xEAU;
    /* Reserved */
    public const uint UC_Speed_Sel = 0xF0U;
    public const uint UC_Playback_Speed = 0xF1U;
    public const uint UC_Standard_Play = 0xF2U;
    public const uint UC_Long_Play = 0xF3U;
    public const uint UC_Extended_Play = 0xF4U;
    public const uint UC_Slow = 0xF5U;
    /* Reserved */
    public const uint UC_Fan_Enable = 0x100U;
    public const uint UC_Fan_Speed = 0x101U;
    public const uint UC_Light_Enable = 0x102U;
    public const uint UC_Light_ILL_Level = 0x103U;
    public const uint UC_Climate_Ctrl_Enable = 0x104U;
    public const uint UC_Room_Temperature = 0x105U;
    public const uint UC_Security_Enable = 0x106U;
    public const uint UC_Fire_Alarm = 0x107U;
    public const uint UC_Police_Alarm = 0x108U;
    public const uint UC_Proximity = 0x109U;
    public const uint UC_Motion = 0x10AU;
    public const uint UC_Duress_Alarm = 0x10BU;
    public const uint UC_Holdup_Alarm = 0x10CU;
    public const uint UC_Medical_Alarm = 0x10DU;
    /* Reserved */
    public const uint UC_Balance_Right = 0x150U;
    public const uint UC_Balance_Left = 0x151U;
    public const uint UC_Bass_Incr = 0x152U;
    public const uint UC_Bass_Decr = 0x153U;
    public const uint UC_Treble_Incr = 0x154U;
    public const uint UC_Treble_Decr = 0x155U;
    /* Reserved */
    public const uint UC_Speaker_Sys = 0x160U;
    public const uint UC_Channel_Left = 0x161U;
    public const uint UC_Channel_Right = 0x162U;
    public const uint UC_Channel_Center = 0x163U;
    public const uint UC_Channel_Front = 0x164U;
    public const uint UC_Channel_Center_Front = 0x165U;
    public const uint UC_Channel_Side = 0x166U;
    public const uint UC_Channel_Surround = 0x167U;
    public const uint UC_Channel_Low_Frequency_Enhancement = 0x168U;
    public const uint UC_Channel_Top = 0x169U;
    public const uint UC_Channel_Unknown = 0x16AU;
    /* Reserved */
    public const uint UC_Subchannel = 0x170U;
    public const uint UC_Subchannel_Incr = 0x171U;
    public const uint UC_Subchannel_Decr = 0x172U;
    public const uint UC_Alternate_Audio_Incr = 0x173U;
    public const uint UC_Alternate_Audio_Decr = 0x174U;
    /* Reserved */
    public const uint UC_App_Launch_Btns = 0x180U;
    public const uint UC_AL_Launch_Btn_Config_Tool = 0x181U;
    public const uint UC_AL_Prog_Btn_Config = 0x182U;
    public const uint UC_AL_Consumer_Ctrl_Config = 0x183U;
    public const uint UC_AL_Word_Processor = 0x184U;
    public const uint UC_AL_Text_Editor = 0x185U;
    public const uint UC_AL_Spreadsheet = 0x186U;
    public const uint UC_AL_Graphics_Editor = 0x187U;
    public const uint UC_AL_Presentation_App = 0x188U;
    public const uint UC_AL_Database_App = 0x189U;
    public const uint UC_AL_Email_Reader = 0x18AU;
    public const uint UC_AL_Newsreader = 0x18BU;
    public const uint UC_AL_Voicemail = 0x18CU;
    public const uint UC_AL_Contacts_Address_Book = 0x18DU;
    public const uint UC_AL_Calendar_Schedule = 0x18EU;
    public const uint UC_AL_Task_Project_Manager = 0x18FU;
    public const uint UC_AL_Log_Journal_Timecard = 0x190U;
    public const uint UC_AL_Checkbook_Finance = 0x191U;
    public const uint UC_AL_Calculator = 0x192U;
    public const uint UC_AL_AV_Capture_Playback = 0x193U;
    public const uint UC_AL_Local_Machine_Browser = 0x194U;
    public const uint UC_AL_LAN_WAN_Browser = 0x195U;
    public const uint UC_AL_Internet_Browser = 0x196U;
    public const uint UC_AL_RemoteNet_ISP_Connect = 0x197U;
    public const uint UC_AL_Net_Conference = 0x198U;
    public const uint UC_AL_Net_Chat = 0x199U;
    public const uint UC_AL_Telephony_Dialer = 0x19AU;
    public const uint UC_AL_Logon = 0x19BU;
    public const uint UC_AL_Logoff = 0x19CU;
    public const uint UC_AL_Logon_Logoff = 0x19DU;
    public const uint UC_AL_Terminal_Lock_Screensaver = 0x19EU;
    public const uint UC_AL_Ctrl_Panel = 0x19FU;
    public const uint UC_AL_Command_Line_Processor_Run = 0x1A0U;
    public const uint UC_AL_Process_Task_Manager = 0x1A1U;
    public const uint UC_AL_Sel_Task_App = 0x1A2U;
    public const uint UC_AL_Next_Task_App = 0x1A3U;
    public const uint UC_AL_Previous_Task_App = 0x1A4U;
    public const uint UC_AL_Preemptive_Halt_Task_App = 0x1A5U;
    public const uint UC_AL_Integrated_Help_Center = 0x1A6U;
    public const uint UC_AL_Documents = 0x1A7U;
    public const uint UC_AL_Thesaurus = 0x1A8U;
    public const uint UC_AL_Dictionary = 0x1A9U;
    public const uint UC_AL_Desktop = 0x1AAU;
    public const uint UC_AL_Spell_Check = 0x1ABU;
    public const uint UC_AL_Grammar_Check = 0x1ACU;
    public const uint UC_AL_Wireless_Status = 0x1ADU;
    public const uint UC_AL_Keyboard_Layout = 0x1AEU;
    public const uint UC_AL_Virus_Protection = 0x1AFU;
    public const uint UC_AL_Encryption = 0x1B0U;
    public const uint UC_AL_Screen_Saver = 0x1B1U;
    public const uint UC_AL_Alarms = 0x1B2U;
    public const uint UC_AL_Clock = 0x1B3U;
    public const uint UC_AL_File_Browser = 0x1B4U;
    public const uint UC_AL_Power_Status = 0x1B5U;
    public const uint UC_AL_Image_Browser = 0x1B6U;
    public const uint UC_AL_Audio_Browser = 0x1B7U;
    public const uint UC_AL_Movie_Browser = 0x1B8U;
    public const uint UC_AL_Digital_Rights_Manager = 0x1B9U;
    public const uint UC_AL_Digital_Wallet = 0x1BAU;
    public const uint UC_AL_Instant_Messaging = 0x1BCU;
    public const uint UC_AL_OEM_Features_Tips_Tutorial_Browser = 0x1BDU;
    public const uint UC_AL_OEM_Help = 0x1BEU;
    public const uint UC_AL_Online_Community = 0x1BFU;
    public const uint UC_AL_Entertainment_Content_Browser = 0x1C0U;
    public const uint UC_AL_Online_Shopping_Browser = 0x1C1U;
    public const uint UC_AL_SmartCard_Information_Help = 0x1C2U;
    public const uint UC_AL_Market_Monitoror_Finance_Browser = 0x1C3U;
    public const uint UC_AL_Customized_Corporate_News_Browser = 0x1C4U;
    public const uint UC_AL_Online_Activity_Browser = 0x1C5U;
    public const uint UC_AL_Research_Search_Browser = 0x1C6U;
    public const uint UC_AL_Audio_Player = 0x1C7U;
    /* Reserved */
    public const uint UC_Generic_GUI_App_Ctrls = 0x200U;
    public const uint UC_AC_New = 0x201U;
    public const uint UC_AC_Open = 0x202U;
    public const uint UC_AC_Close = 0x203U;
    public const uint UC_AC_Exit = 0x204U;
    public const uint UC_AC_Maximize = 0x205U;
    public const uint UC_AC_Minimize = 0x206U;
    public const uint UC_AC_Save = 0x207U;
    public const uint UC_AC_Print = 0x208U;
    public const uint UC_AC_Properties = 0x209U;
    /* Reserved */
    public const uint UC_AC_Undo = 0x21AU;
    public const uint UC_AC_Copy = 0x21BU;
    public const uint UC_AC_Cut = 0x21CU;
    public const uint UC_AC_Paste = 0x21DU;
    public const uint UC_AC_Sel_All = 0x21EU;
    public const uint UC_AC_Find = 0x21FU;
    /* Reserved */
    public const uint UC_AC_Find_and_Replace = 0x220U;
    public const uint UC_AC_Search = 0x221U;
    public const uint UC_AC_Go_To = 0x222U;
    public const uint UC_AC_Home = 0x223U;
    public const uint UC_AC_Back = 0x224U;
    public const uint UC_AC_Forward = 0x225U;
    public const uint UC_AC_Stop = 0x226U;
    public const uint UC_AC_Refresh = 0x227U;
    public const uint UC_AC_Previous_Link = 0x228U;
    public const uint UC_AC_Next_Link = 0x229U;
    public const uint UC_AC_Bookmarks = 0x22AU;
    public const uint UC_AC_History = 0x22BU;
    public const uint UC_AC_Subscriptions = 0x22CU;
    public const uint UC_AC_Zoom_In = 0x22DU;
    public const uint UC_AC_Zoom_Out = 0x22EU;
    public const uint UC_AC_Zoom = 0x22FU;
    public const uint UC_AC_Full_Screen_View = 0x230U;
    public const uint UC_AC_Normal_View = 0x231U;
    public const uint UC_AC_View_Toggle = 0x232U;
    public const uint UC_AC_Scroll_Up = 0x233U;
    public const uint UC_AC_Scroll_Down = 0x234U;
    public const uint UC_AC_Scroll = 0x235U;
    public const uint UC_AC_Pan_Left = 0x236U;
    public const uint UC_AC_Pan_Right = 0x237U;
    public const uint UC_AC_Pan = 0x238U;
    public const uint UC_AC_New_Window = 0x239U;
    public const uint UC_AC_Tile_Horizontally = 0x23AU;
    public const uint UC_AC_Tile_Vertically = 0x23BU;
    public const uint UC_AC_Format = 0x23CU;
    public const uint UC_AC_Edit = 0x23DU;
    public const uint UC_AC_Bold = 0x23EU;
    public const uint UC_AC_Italics = 0x23FU;
    public const uint UC_AC_Underline = 0x240U;
    public const uint UC_AC_Strikethrough = 0x241U;
    public const uint UC_AC_Subscript = 0x242U;
    public const uint UC_AC_Superscript = 0x243U;
    public const uint UC_AC_All_Caps = 0x244U;
    public const uint UC_AC_Rotate = 0x245U;
    public const uint UC_AC_Resize = 0x246U;
    public const uint UC_AC_Flip_Horiz = 0x247U;
    public const uint UC_AC_Flip_Verti = 0x248U;
    public const uint UC_AC_Mirror_Horizontal = 0x249U;
    public const uint UC_AC_Mirror_Vertical = 0x24AU;
    public const uint UC_AC_Font_Sel = 0x24BU;
    public const uint UC_AC_Font_Color = 0x24CU;
    public const uint UC_AC_Font_Size = 0x24DU;
    public const uint UC_AC_Justify_Left = 0x24EU;
    public const uint UC_AC_Justify_Center_H = 0x24FU;
    public const uint UC_AC_Justify_Right = 0x250U;
    public const uint UC_AC_Justify_Block_H = 0x251U;
    public const uint UC_AC_Justify_Top = 0x252U;
    public const uint UC_AC_Justify_Center_V = 0x253U;
    public const uint UC_AC_Justify_Bottom = 0x254U;
    public const uint UC_AC_Justify_Block_V = 0x255U;
    public const uint UC_AC_Indent_Decr = 0x256U;
    public const uint UC_AC_Indent_Incr = 0x257U;
    public const uint UC_AC_Numbered_List = 0x258U;
    public const uint UC_AC_Restart_Numbering = 0x259U;
    public const uint UC_AC_Bulleted_List = 0x25AU;
    public const uint UC_AC_Promote = 0x25BU;
    public const uint UC_AC_Demote = 0x25CU;
    public const uint UC_AC_Yes = 0x25DU;
    public const uint UC_AC_No = 0x25EU;
    public const uint UC_AC_Cancel = 0x25FU;
    public const uint UC_AC_Catalog = 0x260U;
    public const uint UC_AC_BuyorCheckout = 0x261U;
    public const uint UC_AC_Add_to_Cart = 0x262U;
    public const uint UC_AC_Expand = 0x263U;
    public const uint UC_AC_Expand_All = 0x264U;
    public const uint UC_AC_Collapse = 0x265U;
    public const uint UC_AC_Collapse_All = 0x266U;
    public const uint UC_AC_Print_Preview = 0x267U;
    public const uint UC_AC_Paste_Special = 0x268U;
    public const uint UC_AC_Insert_Mode = 0x269U;
    public const uint UC_AC_Delete = 0x26AU;
    public const uint UC_AC_Lock = 0x26BU;
    public const uint UC_AC_Unlock = 0x26CU;
    public const uint UC_AC_Protect = 0x26DU;
    public const uint UC_AC_Unprotect = 0x26EU;
    public const uint UC_AC_Attach_Comment = 0x26FU;
    public const uint UC_AC_Delete_Comment = 0x270U;
    public const uint UC_AC_View_Comment = 0x271U;
    public const uint UC_AC_Sel_Word = 0x272U;
    public const uint UC_AC_Sel_Sentence = 0x273U;
    public const uint UC_AC_Sel_Paragraph = 0x274U;
    public const uint UC_AC_Sel_Column = 0x275U;
    public const uint UC_AC_Sel_Row = 0x276U;
    public const uint UC_AC_Sel_Table = 0x277U;
    public const uint UC_AC_Sel_Object = 0x278U;
    public const uint UC_AC_Redo_Repeat = 0x279U;
    public const uint UC_AC_Sort = 0x27AU;
    public const uint UC_AC_Sort_Ascending = 0x27BU;
    public const uint UC_AC_Sort_Descending = 0x27CU;
    public const uint UC_AC_Filter = 0x27DU;
    public const uint UC_AC_Set_Clock = 0x27EU;
    public const uint UC_AC_View_Clock = 0x27FU;
    public const uint UC_AC_Sel_Time_Zone = 0x280U;
    public const uint UC_AC_Edit_Time_Zones = 0x281U;
    public const uint UC_AC_Set_Alarm = 0x282U;
    public const uint UC_AC_Clear_Alarm = 0x283U;
    public const uint UC_AC_Snooze_Alarm = 0x284U;
    public const uint UC_AC_Reset_Alarm = 0x285U;
    public const uint UC_AC_Synchronize = 0x286U;
    public const uint UC_AC_Send_or_Recv = 0x287U;
    public const uint UC_AC_Send_To = 0x288U;
    public const uint UC_AC_Reply = 0x289U;
    public const uint UC_AC_Reply_All = 0x28AU;
    public const uint UC_AC_Forward_Msg = 0x28BU;
    public const uint UC_AC_Send = 0x28CU;
    public const uint UC_AC_Attach_File = 0x28DU;
    public const uint UC_AC_Upload = 0x28EU;
    public const uint UC_AC_Download_Save_As = 0x28FU;
    public const uint UC_AC_Set_Borders = 0x290U;
    public const uint UC_AC_Insert_Row = 0x291U;
    public const uint UC_AC_Insert_Column = 0x292U;
    public const uint UC_AC_Insert_File = 0x293U;
    public const uint UC_AC_Insert_Picture = 0x294U;
    public const uint UC_AC_Insert_Object = 0x295U;
    public const uint UC_AC_Insert_Symbol = 0x296U;
    public const uint UC_AC_Save_and_Close = 0x297U;
    public const uint UC_AC_Rename = 0x298U;
    public const uint UC_AC_Merge = 0x299U;
    public const uint UC_AC_Split = 0x29AU;
    public const uint UC_AC_Distribute_Horiz = 0x29BU;
    public const uint UC_AC_Distribute_Verti = 0x29CU;
    /* Reserved */


    /* Usage Page: Digitizer (=0x0D)
    */
    public const uint D_Digitizer = 0x1U;
    public const uint D_Pen = 0x2U;
    public const uint D_Light_Pen = 0x3U;
    public const uint D_Touch_Screen = 0x4U;
    public const uint D_Touch_Pad = 0x5U;
    public const uint D_White_Board = 0x6U;
    public const uint D_Coordinate_Measuring_Machine = 0x7U;
    public const uint D_4D_Digitizer = 0x8U;
    public const uint D_Stereo_Plotter = 0x9U;
    public const uint D_Articulated_Arm = 0xAU;
    public const uint D_Armature = 0xBU;
    public const uint D_Multiple_Point_Digitizer = 0xCU;
    public const uint D_Free_Space_Wand = 0xDU;
    /* Reserved */
    public const uint D_Stylus = 0x20U;
    public const uint D_Puck = 0x21U;
    public const uint D_Finger = 0x22U;
    /* Reserved */
    public const uint D_Tip_Pressure = 0x30U;
    public const uint D_Barrel_Pressure = 0x31U;
    public const uint D_In_Range = 0x32U;
    public const uint D_Touch = 0x33U;
    public const uint D_Untouch = 0x34U;
    public const uint D_Tap = 0x35U;
    public const uint D_Quality = 0x36U;
    public const uint D_Data_Valid = 0x37U;
    public const uint D_Transducer_Index = 0x38U;
    public const uint D_Tablet_Function_Keys = 0x39U;
    public const uint D_Program_Change_Keys = 0x3AU;
    public const uint D_Battery_Strength = 0x3BU;
    public const uint D_Invert = 0x3CU;
    public const uint D_X_Tilt = 0x3DU;
    public const uint D_Y_Tilt = 0x3EU;
    public const uint D_Azimuth = 0x3FU;
    public const uint D_Altitude = 0x40U;
    public const uint D_Twist = 0x41U;
    public const uint D_Tip_Switch = 0x42U;
    public const uint D_Secondary_Tip_Switch = 0x43U;
    public const uint D_Barrel_Switch = 0x44U;
    public const uint D_Eraser = 0x45U;
    public const uint D_Tablet_Pick = 0x46U;
    /* Reserved */


    /* Usage Page: Alphanumeric Display (=0x14)
    */
    public const uint AD_Alphanumeric_Display = 0x1U;
    public const uint AD_Bitmapped_Display = 0x2U;
    /* Reserved */
    public const uint AD_Display_Attributes_Report = 0x20U;
    public const uint AD_ASCII_Character_Set = 0x21U;
    public const uint AD_Data_Read_Back = 0x22U;
    public const uint AD_Font_Read_Back = 0x23U;
    public const uint AD_Display_Control_Report = 0x24U;
    public const uint AD_Clear_Display = 0x25U;
    public const uint AD_Display_Enable = 0x26U;
    public const uint AD_Screen_Saver_Delay = 0x27U;
    public const uint AD_Screen_Saver_Enable = 0x28U;
    public const uint AD_Vertical_Scroll = 0x29U;
    public const uint AD_Horizontal_Scroll = 0x2AU;
    public const uint AD_Character_Report = 0x2BU;
    public const uint AD_Display_Data = 0x2CU;
    public const uint AD_Display_Status = 0x2DU;
    public const uint AD_Stat_Not_Ready = 0x2EU;
    public const uint AD_Stat_Ready = 0x2FU;
    public const uint AD_Err_Not_a_loadable_character = 0x30U;
    public const uint AD_Err_Font_data_cannot_be_read = 0x31U;
    public const uint AD_Cursor_Position_Report = 0x32U;
    public const uint AD_Row = 0x33U;
    public const uint AD_Column = 0x34U;
    public const uint AD_Rows = 0x35U;
    public const uint AD_Columns = 0x36U;
    public const uint AD_Cursor_Pixel_Positioning = 0x37U;
    public const uint AD_Cursor_Mode = 0x38U;
    public const uint AD_Cursor_Enable = 0x39U;
    public const uint AD_Cursor_Blink = 0x3AU;
    public const uint AD_Font_Report = 0x3BU;
    public const uint AD_Font_Data = 0x3CU;
    public const uint AD_Character_Width = 0x3DU;
    public const uint AD_Character_Height = 0x3EU;
    public const uint AD_Character_Spacing_Horizontal = 0x3FU;
    public const uint AD_Character_Spacing_Vertical = 0x40U;
    public const uint AD_Unicode_Character_Set = 0x41U;
    public const uint AD_Font_7_Segment = 0x42U;
    public const uint AD_7_Segment_Direct_Map = 0x43U;
    public const uint AD_Font_14_Segment = 0x44U;
    public const uint AD_14_Segment_Direct_Map = 0x45U;
    public const uint AD_Display_Brightness = 0x46U;
    public const uint AD_Display_Contrast = 0x47U;
    public const uint AD_Character_Attribute = 0x48U;
    public const uint AD_Attribute_Readback = 0x49U;
    public const uint AD_Attribute_Data = 0x4AU;
    public const uint AD_Char_Attr_Enhance = 0x4BU;
    public const uint AD_Char_Attr_Underline = 0x4CU;
    public const uint AD_Char_Attr_Blink = 0x4DU;
    /* Reserved */
    public const uint AD_Bitmap_Size_X = 0x80U;
    public const uint AD_Bitmap_Size_Y = 0x81U;
    public const uint AD_Bit_Depth_Format = 0x83U;
    public const uint AD_Display_Orientation = 0x84U;
    public const uint AD_Palette_Report = 0x85U;
    public const uint AD_Palette_Data_Size = 0x86U;
    public const uint AD_Palette_Data_Offset = 0x87U;
    public const uint AD_Palette_Data = 0x88U;
    public const uint AD_Blit_Report = 0x8AU;
    public const uint AD_Blit_Rectangle_X1 = 0x8BU;
    public const uint AD_Blit_Rectangle_Y1 = 0x8CU;
    public const uint AD_Blit_Rectangle_X2 = 0x8DU;
    public const uint AD_Blit_Rectangle_Y2 = 0x8EU;
    public const uint AD_Blit_Data = 0x8FU;
    public const uint AD_Soft_Button = 0x90U;
    public const uint AD_Soft_Button_ID = 0x91U;
    public const uint AD_Soft_Button_Side = 0x92U;
    public const uint AD_Soft_Button_Offset_1 = 0x93U;
    public const uint AD_Soft_Button_Offset_2 = 0x94U;
    public const uint AD_Soft_Button_Report = 0x95U;
    /* Reserved */


    /* Usage Page: Medical Instrument (=0x40)
    */
    public const uint MI_Medical_Ultrasound = 0x1U;
    /* Reserved */
    public const uint MI_VCR_Acquisition = 0x20U;
    public const uint MI_Freeze_Thaw = 0x21U;
    public const uint MI_Clip_Store = 0x22U;
    public const uint MI_Update = 0x23U;
    public const uint MI_Next = 0x24U;
    public const uint MI_Save = 0x25U;
    public const uint MI_Print = 0x26U;
    public const uint MI_Microphone_Enable = 0x27U;
    /* Reserved */
    public const uint MI_Cine = 0x40U;
    public const uint MI_Transmit_Power = 0x41U;
    public const uint MI_Volume = 0x42U;
    public const uint MI_Focus = 0x43U;
    public const uint MI_Depth = 0x44U;
    /* Reserved */
    public const uint MI_Soft_Step_Primary = 0x60U;
    public const uint MI_Soft_Step_Secondary = 0x61U;
    /* Reserved */
    public const uint MI_Depth_Gain_Compensation = 0x70U;
    /* Reserved */
    public const uint MI_Zoom_Select = 0x80U;
    public const uint MI_Zoom_Adjust = 0x81U;
    public const uint MI_Spectral_Doppler_Mode_Select = 0x82U;
    public const uint MI_Spectral_Doppler_Adjust = 0x83U;
    public const uint MI_Color_Doppler_Mode_Select = 0x84U;
    public const uint MI_Color_Doppler_Adjust = 0x85U;
    public const uint MI_Motion_Mode_Select = 0x86U;
    public const uint MI_Motion_Mode_Adjust = 0x87U;
    public const uint MI_2D_Mode_Select = 0x88U;
    public const uint MI_2D_Mode_Adjust = 0x89U;
    /* Reserved */
    public const uint MI_Soft_Control_Select = 0xA0U;
    public const uint MI_Soft_Control_Adjust = 0xA1U;
    /* Reserved */



    public static string ToGenericDesktop(uint usage)
    {
        switch (usage)
        {
            case GD_Pointer:
                return "Pointer";
            case GD_Mouse:
                return "Mouse";
            case GD_Joystick:
                return "Joystick";
            case GD_Game_Pad:
                return "Game Pad";
            case GD_Keyboard:
                return "Keyboard";
            case GD_Keypad:
                return "Keypad";
            case GD_Multiaxis_Controller:
                return "Multi-axis Controller";
            case GD_Tablet_PC_Sys_Controls:
                return "Tablet PC System Controls";
            case GD_X:
                return "X";
            case GD_Y:
                return "Y";
            case GD_Z:
                return "Z";
            case GD_Rx:
                return "Rx";
            case GD_Ry:
                return "Ry";
            case GD_Rz:
                return "Rz";
            case GD_Slider:
                return "Slider";
            case GD_Dial:
                return "Dial";
            case GD_Wheel:
                return "Wheel";
            case GD_Hat_Switch:
                return "Switch";
            case GD_Counted_Buffer:
                return "Counted Buffer";
            case GD_Byte_Count:
                return "Byte Count";
            case GD_Motion_Wakeup:
                return "Motion Wakeup";
            case GD_Start:
                return "Start";
            case GD_Select:
                return "Select";
            case GD_Vx:
                return "Vx";
            case GD_Vy:
                return "Vy";
            case GD_Vz:
                return "Vz";
            case GD_Vbrx:
                return "Vbrx";
            case GD_Vbry:
                return "Vbry";
            case GD_Vbrz:
                return "Vbrz";
            case GD_Vno:
                return "Vno";
            case GD_Feature_Notification:
                return "Feature Notification";
            case GD_Resolution_Multiplier:
                return "Resolution Multiplier";
            case GD_Sys_Control:
                return "System Control";
            case GD_Sys_Power_Down:
                return "System Power Down";
            case GD_Sys_Sleep:
                return "System Sleep";
            case GD_Sys_Wake_Up:
                return "System Wake Up";
            case GD_Sys_Context_Menu:
                return "System Context Menu";
            case GD_Sys_Main_Menu:
                return "System Main Menu";
            case GD_Sys_App_Menu:
                return "System App Menu";
            case GD_Sys_Menu_Help:
                return "System Menu Help";
            case GD_Sys_Menu_Exit:
                return "System Menu Exit";
            case GD_Sys_Menu_Select:
                return "System Menu Select";
            case GD_Sys_Menu_Right:
                return "System Menu Right";
            case GD_Sys_Menu_Left:
                return "System Menu Left";
            case GD_Sys_Menu_Up:
                return "System Menu Up";
            case GD_Sys_Menu_Down:
                return "System Menu Down";
            case GD_Sys_Cold_Restart:
                return "System Cold Restart";
            case GD_Sys_Warm_Restart:
                return "System Warm Restart";
            case GD_D_pad_Up:
                return "D-pad Up";
            case GD_D_pad_Down:
                return "D-pad Down";
            case GD_D_pad_Right:
                return "D-pad Right";
            case GD_D_pad_Left:
                return "D-pad Left";
            case GD_Sys_Dock:
                return "System Dock";
            case GD_Sys_Undock:
                return "SYstem Undock";
            case GD_Sys_Setup:
                return "System Setup";
            case GD_Sys_Break:
                return "System Break";
            case GD_Sys_Debugger_Break:
                return "System Debugger Break";
            case GD_Application_Break:
                return "Application Break";
            case GD_Application_Debugger_Break:
                return "Application Debugger Break";
            case GD_Sys_Speaker_Mute:
                return "System Speaker Mute";
            case GD_Sys_Hibernate:
                return "System Hibernate";
            case GD_Sys_Display_Invert:
                return "System Display Invert";
            case GD_Sys_Display_Internal:
                return "System Display Internal";
            case GD_Sys_Display_External:
                return "System Display External";
            case GD_Sys_Display_Both:
                return "System Display Both";
            case GD_Sys_Display_Dual:
                return "System Display Dual";
            case GD_Sys_Display_Toggle:
                return "System Display Toggle";
            case GD_Sys_Display_Swap:
                return "System Display Swap";
            case GD_Sys_Display_LCD_Autoscale:
                return "System Display LCD Autoscale";
            default:
                return "Unknown";
        }
    }

    public static string ToSimulationControls(uint usage)
    {
        switch (usage)
        {
            case SC_SimuDev_Flight:
                return "Flight Simulation Device";
            case SC_SimuDev_Automobile:
                return "Automobile Simulation Device";
            case SC_SimuDev_Tank:
                return "Tank Simulation Device";
            case SC_SimuDev_Spaceship:
                return "Spaceship Simulation Device";
            case SC_SimuDev_Submarine:
                return "Submarine Simulation Device";
            case SC_SimuDev_Sailing:
                return "Sailing Simulation Device";
            case SC_SimuDev_Motorcycle:
                return "Motorcycle Simulation Device";
            case SC_SimuDev_Sports:
                return "Sports Simulation Device";
            case SC_SimuDev_Airplane:
                return "Airplane Simulation Device";
            case SC_SimuDev_Helicopter:
                return "Helicopter Simulation Device";
            case SC_SimuDev_MagicCarpet:
                return "MagicCarpet Simulation Device";
            case SC_SimuDev_Bicycle:
                return "Bicycle Simulation Device";
            case SC_Flight_Control_Stick:
                return "Flight Control Stick";
            case SC_Flilght_Stick:
                return "Flilght Stick";
            case SC_Cyclic_Control:
                return "Cyclic Control";
            case SC_Cyclic_Trim:
                return "Cyclic Trim";
            case SC_Flight_Yoke:
                return "Flight Yoke";
            case SC_Track_Control:
                return "Track Control";
            case SC_Aileron:
                return "Aileron";
            case SC_Aileron_Trim:
                return "Aileron Trim";
            case SC_Anti_Torque_Control:
                return "Anti Torque Control";
            case SC_Autopilot_Enable:
                return "Autopilot Enable";
            case SC_Chaff_Release:
                return "Chaff Release";
            case SC_Collective_Control:
                return "Collective Control";
            case SC_Dive_Brake:
                return "Dive Brake";
            case SC_Electronic_Countermeasures:
                return "Electronic Countermeasures";
            case SC_Elevator:
                return "Elevator";
            case SC_Elevator_Trim:
                return "Elevator Trim";
            case SC_Rudder:
                return "Rudder";
            case SC_Throttle:
                return "Throttle";
            case SC_Flight_Communications:
                return "Flight Communications";
            case SC_Flare_Release:
                return "Flare Release";
            case SC_Landing_Gear:
                return "Landing Gear";
            case SC_Toe_Brake:
                return "Toe Brake";
            case SC_Trigger:
                return "Trigger";
            case SC_Weapons_Arm:
                return "Weapons Arm";
            case SC_Weapons_Select:
                return "Weapons Select";
            case SC_Wing_Flaps:
                return "Wing Flaps";
            case SC_Accelerator:
                return "Accelerator";
            case SC_Brake:
                return "Brake";
            case SC_Clutch:
                return "Clutch";
            case SC_Shifter:
                return "Shifter";
            case SC_Steering:
                return "Steering";
            case SC_Turret_Direction:
                return "Turret Direction";
            case SC_Barrel_Elevation:
                return "Barrel Elevation";
            case SC_Dive_Plane:
                return "Dive Plane";
            case SC_Ballast:
                return "Ballast";
            case SC_Bicycle_Crank:
                return "Bicycle Crank";
            case SC_Handle_Bars:
                return "Handle Bars";
            case SC_Front_Brake:
                return "Front Brake";
            case SC_Rear_Brake:
                return "Rear Brake";
            default:
                return "Unkonwn";
        }
    }

    public static string ToVRControls(uint usage)
    {
        switch (usage)
        {
            case VR_Belt:
                return "Belt";
            case VR_Body_Suit:
                return "Body Suit";
            case VR_Flexor:
                return "Flexor";
            case VR_Glove:
                return "Glove";
            case VR_Head_Tracker:
                return "Head Tracker";
            case VR_Head_Mounted_Display:
                return "Head Mounted Display";
            case VR_Hand_Tracker:
                return "Hand Tracker";
            case VR_Oculometer:
                return "Oculometer";
            case VR_Vest:
                return "Vest";
            case VR_Animatronic_Device:
                return "Animatronic Device";
            case VR_Stereo_Enable:
                return "Stereo Enable";
            case VR_Display_Enable:
                return "Display Enable";
            default:
                return "Unknown";
        }
    }

    public static string ToSportControls(uint usage)
    {
        switch (usage)
        {
            case SpC_Baseball_Bat:
                return "Baseball Bat";
            case SpC_Golf_Club:
                return "Golf Club";
            case SpC_Rowing_Machine:
                return "Rowing Machine";
            case SpC_Treadmill:
                return "Treadmill";
            case SpC_Oar:
                return "Oar";
            case SpC_Slope:
                return "Slope";
            case SpC_Rate:
                return "Rate";
            case SpC_Stick_Speed:
                return "Stick Speed";
            case SpC_Stick_Face_Angle:
                return "Stick Face Angle";
            case SpC_Stick_HeelorToe:
                return "Stick HeelorToe";
            case SpC_Stick_Follow_Through:
                return "Stick Follow Through";
            case SpC_Stick_Tempo:
                return "Stick Tempo";
            case SpC_Stick_Type:
                return "Stick Type";
            case SpC_Stick_Height:
                return "Stick Height";
            case SpC_Putter:
                return "Putter";
            case SpC_Iron_1:
            case SpC_Iron_2:
            case SpC_Iron_3:
            case SpC_Iron_4:
            case SpC_Iron_5:
            case SpC_Iron_6:
            case SpC_Iron_7:
            case SpC_Iron_8:
            case SpC_Iron_9:
            case SpC_Iron_10:
            case SpC_Iron_11:
                return "Iron";
            case SpC_Sand_Wedge:
                return "Sand Wedge";
            case SpC_Loft_Wedge:
                return "Loft Wedge";
            case SpC_Power_Wedge:
                return "Power Wedge";
            case SpC_Wood_1:
            case SpC_Wood_3:
            case SpC_Wood_5:
            case SpC_Wood_7:
            case SpC_Wood_9:
                return "Wood";
            default:
                return "Unknown";
        }
    }

    public static string ToGameControls(uint usage)
    {
        switch (usage)
        {
            case GC_3D_Game_Controller:
                return "3D Game Controller ";
            case GC_Pinball_Device:
                return "Pinball Device ";
            case GC_Gun_Device:
                return "Gun Device";
            case GC_Point_of_View:
                return "Point of View";
            case GC_Turn_Right_Left:
                return "Turn Right/Left";
            case GC_Pitch_Forward_Backward:
                return "Pitch Forward/Backward";
            case GC_Roll_Right_Left:
                return "Roll Right/Left";
            case GC_Move_Right_Left:
                return "Move Right/Left";
            case GC_Move_Forward_Backward:
                return "Move Forward/Backward";
            case GC_Move_Up_Down:
                return "Move Up/Down";
            case GC_Lean_Right_Left:
                return "Lean Right/Left";
            case GC_Lean_Forward_Backward:
                return "Lean Forward/Backward";
            case GC_Height_of_POV:
                return "Height of POV";
            case GC_Flipper:
                return "Flipper";
            case GC_Secondary_Flipper:
                return "Secondary Flipper";
            case GC_Bump:
                return "Bump";
            case GC_New_Game:
                return "New Game";
            case GC_Shoot_Ball:
                return "Shoot Ball";
            case GC_Player:
                return "Player";
            case GC_Gun_Bolt:
                return "Gun Bolt";
            case GC_Gun_Clip:
                return "Gun Clip";
            case GC_Gun_Selector:
                return "Gun Selector";
            case GC_Gun_Single_Shot:
                return "Gun Single Shot";
            case GC_Gun_Burst:
                return "Gun Burst";
            case GC_Gun_Automatic:
                return "Gun Automatic";
            case GC_Gun_Safety:
                return "Gun Safety";
            case GC_Gamepad_Fire_Jump:
                return "Gamepad Fire/Jump";
            case GC_Gamepad_Trigger:
                return "Gamepad Trigger";
            default:
                return "Unknown";
        }
    }

    public static string ToGenericDeviceControls(uint usage)
    {
        switch (usage)
        {
            case GDC_Battery_Strength:
                return "Battery Strength";
            case GDC_Wireless_Channel:
                return "Wireless Channel";
            case GDC_Wireless_ID:
                return "Wireless ID";
            case GDC_Discover_Wireless_Ctrl:
                return "Discover Wireless Ctrl";
            case GDC_SC_Character_Entered:
                return "Security Code Character Entered";
            case GDC_SC_Character_Cleared:
                return "Security Code Character Cleared";
            case GDC_SC_Cleared:
                return "Security Code Cleared";
            default:
                return "Unknown";
        }
    }

    public static string ToKeyboardOrKeypad(uint usage)
    {
        switch (usage)
        {
            //TODO:
            default:
                return "Unknown";
        }
    }

    public static string ToLEDs(uint usage)
    {
        switch (usage)
        {
            case LED_Num_Lock:
                return "Num Lock";
            case LED_Caps_Lock:
                return "Caps Lock";
            case LED_Scroll_Lock:
                return "Scroll Lock";
            case LED_Compose:
                return "Compose";
            case LED_Kana:
                return "Kana";
            case LED_Power:
                return "Power";
            case LED_Shift:
                return "Shift";
            case LED_Donot_Disturb:
                return "Donot Disturb";
            case LED_Mute:
                return "Mute";
            case LED_Tone_Enable:
                return "Tone Enable";
            case LED_High_Cut_Filter:
                return "High Cut Filter";
            case LED_Low_Cut_Filter:
                return "Low Cut Filter";
            case LED_Equalizer_Enable:
                return "Equalizer Enable";
            case LED_Sound_Field_On:
                return "Sound Field On";
            case LED_Surround_On:
                return "Surround On";
            case LED_Repeat:
                return "Repeat";
            case LED_Stereo:
                return "Stereo";
            case LED_Sampling_Rate_Detect:
                return "Sampling Rate Detect";
            case LED_Spinning:
                return "Spinning";
            case LED_CAV:
                return "CAV";
            case LED_CLV:
                return "CLV";
            case LED_Recording_Format_Detect:
                return "Recording Format Detect";
            case LED_Off_Hook:
                return "Off-Hook";
            case LED_Ring:
                return "Ring";
            case LED_Message_Waiting:
                return "Message Waiting";
            case LED_Data_Mode:
                return "Data Mode";
            case LED_Battery_Operation:
                return "Battery Operation";
            case LED_Battery_OK:
                return "Battery OK";
            case LED_Battery_Low:
                return "Battery LOW";
            case LED_Speaker:
                return "Speaker";
            case LED_Head_Set:
                return "Head Set";
            case LED_Hold:
                return "Hold";
            case LED_Microphone:
                return "Microphone";
            case LED_Coverage:
                return "Coverage";
            case LED_Night_Mode:
                return "Night Mode";
            case LED_Send_Calls:
                return "Send Calls";
            case LED_Call_Pickup:
                return "Call Pickup";
            case LED_Conference:
                return "Conference";
            case LED_Standby:
                return "Standby";
            case LED_Camera_On:
                return "Camera On";
            case LED_Camera_Off:
                return "Camera Off";
            case LED_On_Line:
                return "On Line";
            case LED_Off_Line:
                return "Off Line";
            case LED_Busy:
                return "Busy";
            case LED_Ready:
                return "Ready";
            case LED_Paper_Out:
                return "Paper Out";
            case LED_Paper_Jam:
                return "Paper Jam";
            case LED_Remote:
                return "Remote";
            case LED_Forward:
                return "Forward";
            case LED_Reverse:
                return "Reverse";
            case LED_Stop:
                return "Stop";
            case LED_Rewind:
                return "Rewind";
            case LED_Fast_Forward:
                return "Fast Forward";
            case LED_Play:
                return "Play";
            case LED_Pause:
                return "Pause";
            case LED_Record:
                return "Record";
            case LED_Error:
                return "Error";
            case LED_Selected_Indicator:
                return "Selected Indicator";
            case LED_In_Use_Indicator:
                return "In Use Indicator";
            case LED_Multi_Mode_Indicator:
                return "Multi Mode Indicator";
            case LED_Indicator_On:
                return "Indicator On";
            case LED_Indicator_Flash:
                return "Indicator Flash";
            case LED_Indicator_Slow_Blink:
                return "Indicator Slow Blink";
            case LED_Indicator_Fast_Blink:
                return "Indicator Fast Blink";
            case LED_Indicator_Off:
                return "Indicator Off";
            case LED_Flash_On_Time:
                return "Flash On Time";
            case LED_Slow_Blink_On_Time:
                return "Slow Blink On Time";
            case LED_Slow_Blink_Off_Time:
                return "Slow Blink Off Time";
            case LED_Fast_Blink_On_Time:
                return "Fast Blink On Time";
            case LED_Fast_Blink_Off_Time:
                return "Fast Blink Off Time";
            case LED_Usage_Indicator_Color:
                return "Usage Indicator Color";
            case LED_Indicator_Red:
                return "Indicator Red";
            case LED_Indicator_Green:
                return "Indicator Green";
            case LED_Indicator_Amber:
                return "Indicator Amber";
            case LED_Generic_Indicator:
                return "Generic Indicator";
            case LED_Sys_Suspend:
                return "System Suspend";
            case LED_External_Power_Connected:
                return "External Power Connected";
            default:
                return "Unknown";
        }
    }

    public static string ToButton(uint usage)
    {
        return $"Button {usage}";
    }

    public static string ToOrdinal(uint usage)
    {
        return $"Instance {usage}";
    }

    public static string ToTelephony(uint usage)
    {
        switch (usage)
        {
            case TD_Phone:
                return "Phone";
            case TD_Answering_Machine:
                return "Answering Machine";
            case TD_Message_Controls:
                return "Message Controls";
            case TD_Handset:
                return "Handset";
            case TD_Headset:
                return "Headset";
            case TD_Telephony_Key_Pad:
                return "Telephony Key Pad";
            case TD_Programmable_Button:
                return "Programmable Button";
            case TD_Hook_Switch:
                return "Hook Switch";
            case TD_Flash:
                return "Flash";
            case TD_Feature:
                return "Feature";
            case TD_Hold:
                return "Hold";
            case TD_Redial:
                return "Redial";
            case TD_Transfer:
                return "Transfer";
            case TD_Drop:
                return "Drop";
            case TD_Park:
                return "Park";
            case TD_Forward_Calls:
                return "Forward Calls";
            case TD_Alternate_Function:
                return "Alternate Function";
            case TD_Line:
                return "Line";
            case TD_Speaker_Phone:
                return "Speaker Phone";
            case TD_Conference:
                return "Conference";
            case TD_Ring_Enable:
                return "Ring Enable";
            case TD_Ring_Select:
                return "Ring Select";
            case TD_Phone_Mute:
                return "Phone Mute";
            case TD_Caller_ID:
                return "Caller ID";
            case TD_Send:
                return "Send";
            case TD_Speed_Dial:
                return "Speed Dial";
            case TD_Store_Number:
                return "Store Number";
            case TD_Recall_Number:
                return "Recall Number";
            case TD_Phone_Directory:
                return "Phone Directory";
            case TD_Voice_Mail:
                return "Voice Mail";
            case TD_Screen_Calls:
                return "Screen Calls";
            case TD_Do_Not_Disturb:
                return "Do Not Disturb";
            case TD_Message:
                return "Message";
            case TD_Answer_On_Off:
                return "Answer On/Off";
            case TD_Inside_Dial_Tone:
                return "Inside Dial Tone";
            case TD_Outside_Dial_Tone:
                return "Outside Dial Tone";
            case TD_Inside_Ring_Tone:
                return "Inside Ring Tone";
            case TD_Outside_Ring_Tone:
                return "Outside Ring Tone";
            case TD_Priority_Ring_Tone:
                return "Priority Ring Tone";
            case TD_Inside_Ringback:
                return "Inside Ringback";
            case TD_Priority_Ringback:
                return "Priority Ringback";
            case TD_Line_Busy_Tone:
                return "Line Busy Tone";
            case TD_Reorder_Tone:
                return "Reorder Tone";
            case TD_Call_Waiting_Tone:
                return "Call Waiting Tone";
            case TD_Confirmation_Tone_1:
                return "Confirmation Tone 1";
            case TD_Confirmation_Tone_2:
                return "Confirmation Tone 2";
            case TD_Tones_Off:
                return "Tones Off";
            case TD_Outside_Ringback:
                return "Outside Ringback";
            case TD_Ringer:
                return "Ringer";
            default:
                return "Unknown";
        }
    }

    public static string ToConsumer(uint usage)
    {
        switch (usage)
        {
            case UC_Consumer_Ctrl:
                return "Consumer Control";
            case UC_Numeric_Keypad:
                return "Numeric Key Pad";
            case UC_Prog_Btns:
                return "Programmable Buttons";
            case UC_Mic:
                return "Microphone";
            case UC_Headphone:
                return "Headphone";
            case UC_Graphic_Equalizer:
                return "Graphic Equalizer";
            case UC_Add_10:
                return "+10";
            case UC_Add_100:
                return "+100";
            case UC_AM_PM:
                return "AM/PM";
            case UC_Power:
                return "Power";
            case UC_Reset:
                return "Reset";
            case UC_Sleep:
                return "Sleep";
            case UC_Sleep_After:
                return "Sleep After";
            case UC_Sleep_Mode:
                return "Sleep Mode";
            case UC_ILL:
                return "Illumination";
            case UC_Function_Buttons:
                return "Function Buttons";
            case UC_Menu:
                return "Menu";
            case UC_Menu_Pick:
                return "Menu Pick";
            case UC_Menu_Up:
                return "Menu Up";
            case UC_Menu_Down:
                return "Menu Down";
            case UC_Menu_Left:
                return "Menu Left";
            case UC_Menu_Right:
                return "Menu Right";
            case UC_Menu_Escape:
                return "Menu Escape";
            case UC_Menu_Value_Incr:
                return "Menu Value Increase";
            case UC_Menu_Value_Decr:
                return "Menu Value Decrease";
            case UC_Data_On_Screen:
                return "Data On Screen";
            case UC_Closed_Caption:
                return "Closed Caption";
            case UC_Closed_Caption_Sel:
                return "Closed Caption Select";
            case UC_VCR_TV:
                return "VCR/TV";
            case UC_Broadcast_Mode:
                return "Broadcast Mode";
            case UC_Snapshot:
                return "Snapshot";
            case UC_Still:
                return "Still";
            case UC_Selion:
                return "Selection";
            case UC_Assign_Selion:
                return "Assign Selection";
            case UC_Mode_Step:
                return "Mode Step";
            case UC_Recall_Last:
                return "Recall Last";
            case UC_Enter_Channel:
                return "Enter Channel";
            case UC_Order_Movie:
                return "Order Movie";
            case UC_Channel:
                return "Channel";
            case UC_Media_Selion:
                return "Media Selection";
            case UC_Media_Sel_Computer:
                return "Media Select Computer";
            case UC_Media_Sel_TV:
                return "Media Select TV";
            case UC_Media_Sel_WWW:
                return "Media Select WWW";
            case UC_Media_Sel_DVD:
                return "Media Select DVD";
            case UC_Media_Sel_Telephone:
                return "Media Select Telephone";
            case UC_Media_Sel_Program_Guide:
                return "Media Select Program Guide";
            case UC_Media_Sel_Video_Phone:
                return "Media Select Video Phone";
            case UC_Media_Sel_Games:
                return "Media Select Games";
            case UC_Media_Sel_Messages:
                return "Media Select Messages";
            case UC_Media_Sel_CD:
                return "Media Select CD";
            case UC_Media_Sel_VCR:
                return "Media Select VCR";
            case UC_Media_Sel_Tuner:
                return "Media Select Tuner";
            case UC_Quit:
                return "Quit";
            case UC_Help:
                return "Help";
            case UC_Media_Sel_Tape:
                return "Media Select Tape";
            case UC_Media_Sel_Cable:
                return "Media Select Cable";
            case UC_Media_Sel_Satellite:
                return "Media Select Satellite";
            case UC_Media_Sel_Security:
                return "Media Select Security";
            case UC_Media_Sel_Home:
                return "Media Select Home";
            case UC_Media_Sel_Call:
                return "Media Select Call";
            case UC_Channel_Incr:
                return "Channel Increment";
            case UC_Channel_Decr:
                return "Channel Decrement";
            case UC_Media_Sel_SAP:
                return "Media Select SAP";
            case UC_VCR_Plus:
                return "VCR Plus";
            case UC_Once:
                return "Once";
            case UC_Daily:
                return "Daily";
            case UC_Weekly:
                return "Weekly";
            case UC_Monthly:
                return "Monthly";
            case UC_Play:
                return "Play";
            case UC_Pause:
                return "Pause";
            case UC_Record:
                return "Record";
            case UC_Fast_Forward:
                return "Fast Forward";
            case UC_Rewind:
                return "Rewind";
            case UC_Scan_Next_Track:
                return "Scan Next Track";
            case UC_Scan_Previous_Track:
                return "Scan Previous Track";
            case UC_Stop:
                return "Stop";
            case UC_Eject:
                return "Eject";
            case UC_Random_Play:
                return "Random Play";
            case UC_Sel_Disc:
                return "Select Disc";
            case UC_Enter_Disc:
                return "Enter Disc";
            case UC_Repeat:
                return "Repeat";
            case UC_Tracking:
                return "Tracking";
            case UC_Track_Normal:
                return "Track Normal";
            case UC_Slow_Tracking:
                return "Slow Tracking";
            case UC_Frame_Forward:
                return "Frame Forward";
            case UC_Frame_Back:
                return "Frame Back";
            case UC_Mark:
                return "Mark";
            case UC_Clear_Mark:
                return "Clear Mark";
            case UC_Repeat_From_Mark:
                return "Repeat From Mark";
            case UC_Return_To_Mark:
                return "Return To Mark";
            case UC_Search_Mark_Forward:
                return "Search Mark Forward";
            case UC_Search_Mark_Backward:
                return "Search Mark Backwards";
            case UC_Counter_Reset:
                return "Counter Reset";
            case UC_Show_Counter:
                return "Show Counter";
            case UC_Tracking_Incr:
                return "Tracking Increment";
            case UC_Tracking_Decr:
                return "Tracking Decrement";
            case UC_Stop_Eject:
                return "Stop/Eject";
            case UC_Play_Pause:
                return "Play/Pause";
            case UC_Play_Skip:
                return "Play/Skip";
            case UC_Volume:
                return "Volume";
            case UC_Balance:
                return "Balance";
            case UC_Mute:
                return "Mute";
            case UC_Bass:
                return "Bass";
            case UC_Treble:
                return "Treble";
            case UC_Bass_Boost:
                return "Bass Boost";
            case UC_Surround_Mode:
                return "Surround Mode";
            case UC_Loudness:
                return "Loudness";
            case UC_MPX:
                return "MPX";
            case UC_Volume_Incr:
                return "Volume Increment";
            case UC_Volume_Decr:
                return "Volume Decrement";
            case UC_Speed_Sel:
                return "Speed Select";
            case UC_Playback_Speed:
                return "Playback Speed";
            case UC_Standard_Play:
                return "Standard Play";
            case UC_Long_Play:
                return "Long Play";
            case UC_Extended_Play:
                return "Extended Play";
            case UC_Slow:
                return "Slow";
            case UC_Fan_Enable:
                return "Fan Enable";
            case UC_Fan_Speed:
                return "Fan Speed";
            case UC_Light_Enable:
                return "Light Enable";
            case UC_Light_ILL_Level:
                return "Light Illumination Level";
            case UC_Climate_Ctrl_Enable:
                return "Climate Control Enable";
            case UC_Room_Temperature:
                return "Room Temperature";
            case UC_Security_Enable:
                return "Security Enable";
            case UC_Fire_Alarm:
                return "Fire Alarm";
            case UC_Police_Alarm:
                return "Police Alarm";
            case UC_Proximity:
                return "Proximity";
            case UC_Motion:
                return "Motion";
            case UC_Duress_Alarm:
                return "Duress Alarm";
            case UC_Holdup_Alarm:
                return "Holdup Alarm";
            case UC_Medical_Alarm:
                return "Medical Alarm";
            case UC_Balance_Right:
                return "Balance Right";
            case UC_Balance_Left:
                return "Balance Left";
            case UC_Bass_Incr:
                return "Bass Increment";
            case UC_Bass_Decr:
                return "Bass Decrement";
            case UC_Treble_Incr:
                return "Treble Increment";
            case UC_Treble_Decr:
                return "Treble Decrement";
            case UC_Speaker_Sys:
                return "Speaker System";
            case UC_Channel_Left:
                return "Channel Left";
            case UC_Channel_Right:
                return "Channel Right";
            case UC_Channel_Center:
                return "Channel Center";
            case UC_Channel_Front:
                return "Channel Front";
            case UC_Channel_Center_Front:
                return "Channel Center Front";
            case UC_Channel_Side:
                return "Channel Side";
            case UC_Channel_Surround:
                return "Channel Surround";
            case UC_Channel_Low_Frequency_Enhancement:
                return "Channel Low Frequency Enhancement";
            case UC_Channel_Top:
                return "Channel Top";
            case UC_Channel_Unknown:
                return "Channel Unknown";
            case UC_Subchannel:
                return "Sub-channel";
            case UC_Subchannel_Incr:
                return "Sub-channel Increment";
            case UC_Subchannel_Decr:
                return "Sub-channel Decrement";
            case UC_Alternate_Audio_Incr:
                return "Alternate Audio Increment";
            case UC_Alternate_Audio_Decr:
                return "Alternate Audio Decrement";
            case UC_App_Launch_Btns:
                return "Application Launch Buttons";
            case UC_AL_Launch_Btn_Config_Tool:
                return "AL Launch Button Configuration Tool";
            case UC_AL_Prog_Btn_Config:
                return "AL Programmable Button Configuration";
            case UC_AL_Consumer_Ctrl_Config:
                return "AL Consumer Control Configuration";
            case UC_AL_Word_Processor:
                return "AL Word Processor";
            case UC_AL_Text_Editor:
                return "AL Text Editor";
            case UC_AL_Spreadsheet:
                return "AL Spreadsheet";
            case UC_AL_Graphics_Editor:
                return "AL Graphics Editor";
            case UC_AL_Presentation_App:
                return "AL Presentation App";
            case UC_AL_Database_App:
                return "AL Database App";
            case UC_AL_Email_Reader:
                return "AL Email Reader";
            case UC_AL_Newsreader:
                return "AL Newsreader";
            case UC_AL_Voicemail:
                return "AL Voicemail";
            case UC_AL_Contacts_Address_Book:
                return "AL Contacts/Address Book";
            case UC_AL_Calendar_Schedule:
                return "AL Calendar/Schedule";
            case UC_AL_Task_Project_Manager:
                return "AL Task/Project Manager";
            case UC_AL_Log_Journal_Timecard:
                return "AL Log/Journal/Timecard";
            case UC_AL_Checkbook_Finance:
                return "AL Checkbook/Finance";
            case UC_AL_Calculator:
                return "AL Calculator";
            case UC_AL_AV_Capture_Playback:
                return "AL A/V Capture/Playback";
            case UC_AL_Local_Machine_Browser:
                return "AL Local Machine Browser";
            case UC_AL_LAN_WAN_Browser:
                return "AL LAN/WAN Browser";
            case UC_AL_Internet_Browser:
                return "AL Internet Browser";
            case UC_AL_RemoteNet_ISP_Connect:
                return "AL Remote Networking/ISP Connect";
            case UC_AL_Net_Conference:
                return "AL Network Conference";
            case UC_AL_Net_Chat:
                return "AL Network Chat";
            case UC_AL_Telephony_Dialer:
                return "AL Telephony/Dialer";
            case UC_AL_Logon:
                return "AL Logon";
            case UC_AL_Logoff:
                return "AL Logoff";
            case UC_AL_Logon_Logoff:
                return "AL Logon/Logoff";
            case UC_AL_Terminal_Lock_Screensaver:
                return "AL Terminal Lock/Screensaver";
            case UC_AL_Ctrl_Panel:
                return "AL Control Panel";
            case UC_AL_Command_Line_Processor_Run:
                return "AL Command Line Processor/Run";
            case UC_AL_Process_Task_Manager:
                return "AL Process/Task Manager";
            case UC_AL_Sel_Task_App:
                return "AL Select Task/Application";
            case UC_AL_Next_Task_App:
                return "AL Next Task/Application";
            case UC_AL_Previous_Task_App:
                return "AL Previous Task/Application";
            case UC_AL_Preemptive_Halt_Task_App:
                return "AL Preemptive Halt Task/Application";
            case UC_AL_Integrated_Help_Center:
                return "AL Integrated Help Center";
            case UC_AL_Documents:
                return "AL Documents";
            case UC_AL_Thesaurus:
                return "AL Thesaurus";
            case UC_AL_Dictionary:
                return "AL Dictionary";
            case UC_AL_Desktop:
                return "AL Desktop";
            case UC_AL_Spell_Check:
                return "AL Spell Check";
            case UC_AL_Grammar_Check:
                return "AL Grammar Check";
            case UC_AL_Wireless_Status:
                return "AL Wireless Status";
            case UC_AL_Keyboard_Layout:
                return "AL Keyboard Layout";
            case UC_AL_Virus_Protection:
                return "AL Virus Protection";
            case UC_AL_Encryption:
                return "AL Encryption";
            case UC_AL_Screen_Saver:
                return "AL Screen Saver";
            case UC_AL_Alarms:
                return "AL Alarms";
            case UC_AL_Clock:
                return "AL Clock";
            case UC_AL_File_Browser:
                return "AL File Browser";
            case UC_AL_Power_Status:
                return "AL Power Status";
            case UC_AL_Image_Browser:
                return "AL Image Browser";
            case UC_AL_Audio_Browser:
                return "AL Audio Browser";
            case UC_AL_Movie_Browser:
                return "AL Movie Browser";
            case UC_AL_Digital_Rights_Manager:
                return "AL Digital Rights Manager";
            case UC_AL_Digital_Wallet:
                return "AL Digital Wallet";
            case UC_AL_Instant_Messaging:
                return "AL Instant Messaging";
            case UC_AL_OEM_Features_Tips_Tutorial_Browser:
                return "AL OEM Features/Tips/Tutorial Browser";
            case UC_AL_OEM_Help:
                return "AL OEM Help";
            case UC_AL_Online_Community:
                return "AL Online Community";
            case UC_AL_Entertainment_Content_Browser:
                return "AL Entertainment Content Browser";
            case UC_AL_Online_Shopping_Browser:
                return "AL Online Shopping Browser";
            case UC_AL_SmartCard_Information_Help:
                return "AL SmartCard Information/Help";
            case UC_AL_Market_Monitoror_Finance_Browser:
                return "AL Market Monitor/Finance Browser";
            case UC_AL_Customized_Corporate_News_Browser:
                return "AL Customized Corporate News Browser";
            case UC_AL_Online_Activity_Browser:
                return "AL Online Activity Browser";
            case UC_AL_Research_Search_Browser:
                return "AL Research/Search Browser";
            case UC_AL_Audio_Player:
                return "AL Audio Player";
            case UC_Generic_GUI_App_Ctrls:
                return "Generic GUI Application Controls";
            case UC_AC_New:
                return "AC New";
            case UC_AC_Open:
                return "AC Open";
            case UC_AC_Close:
                return "AC Close";
            case UC_AC_Exit:
                return "AC Exit";
            case UC_AC_Maximize:
                return "AC Maximize";
            case UC_AC_Minimize:
                return "AC Minimize";
            case UC_AC_Save:
                return "AC Save";
            case UC_AC_Print:
                return "AC Print";
            case UC_AC_Properties:
                return "AC Properties";
            case UC_AC_Undo:
                return "AC Undo";
            case UC_AC_Copy:
                return "AC Copy";
            case UC_AC_Cut:
                return "AC Cut";
            case UC_AC_Paste:
                return "AC Paste";
            case UC_AC_Sel_All:
                return "AC Select All";
            case UC_AC_Find:
                return "AC Find";
            case UC_AC_Find_and_Replace:
                return "AC Find and Replace";
            case UC_AC_Search:
                return "AC Search";
            case UC_AC_Go_To:
                return "AC Go To";
            case UC_AC_Home:
                return "AC Home";
            case UC_AC_Back:
                return "AC Back";
            case UC_AC_Forward:
                return "AC Forward";
            case UC_AC_Stop:
                return "AC Stop";
            case UC_AC_Refresh:
                return "AC Refresh";
            case UC_AC_Previous_Link:
                return "AC Previous Link";
            case UC_AC_Next_Link:
                return "AC Next Link";
            case UC_AC_Bookmarks:
                return "AC Bookmarks";
            case UC_AC_History:
                return "AC History";
            case UC_AC_Subscriptions:
                return "AC Subscriptions";
            case UC_AC_Zoom_In:
                return "AC Zoom In";
            case UC_AC_Zoom_Out:
                return "AC Zoom Out";
            case UC_AC_Zoom:
                return "AC Zoom";
            case UC_AC_Full_Screen_View:
                return "AC Full Screen View";
            case UC_AC_Normal_View:
                return "AC Normal View";
            case UC_AC_View_Toggle:
                return "AC View Toggle";
            case UC_AC_Scroll_Up:
                return "AC Scroll Up";
            case UC_AC_Scroll_Down:
                return "AC Scroll Down";
            case UC_AC_Scroll:
                return "AC Scroll";
            case UC_AC_Pan_Left:
                return "AC Pan Left";
            case UC_AC_Pan_Right:
                return "AC Pan Right";
            case UC_AC_Pan:
                return "AC Pan";
            case UC_AC_New_Window:
                return "AC New Window";
            case UC_AC_Tile_Horizontally:
                return "AC Tile Horizontally";
            case UC_AC_Tile_Vertically:
                return "AC Tile Vertically";
            case UC_AC_Format:
                return "AC Format";
            case UC_AC_Edit:
                return "AC Edit";
            case UC_AC_Bold:
                return "AC Bold";
            case UC_AC_Italics:
                return "AC Italics";
            case UC_AC_Underline:
                return "AC Underline";
            case UC_AC_Strikethrough:
                return "AC Strikethrough";
            case UC_AC_Subscript:
                return "AC Subscript";
            case UC_AC_Superscript:
                return "AC Superscript";
            case UC_AC_All_Caps:
                return "AC All Caps";
            case UC_AC_Rotate:
                return "AC Rotate";
            case UC_AC_Resize:
                return "AC Resize";
            case UC_AC_Flip_Horiz:
                return "AC Flip horizontal";
            case UC_AC_Flip_Verti:
                return "AC Flip Vertical";
            case UC_AC_Mirror_Horizontal:
                return "AC Mirror Horizontal";
            case UC_AC_Mirror_Vertical:
                return "AC Mirror Vertical";
            case UC_AC_Font_Sel:
                return "AC Font Select";
            case UC_AC_Font_Color:
                return "AC Font Color";
            case UC_AC_Font_Size:
                return "AC Font Size";
            case UC_AC_Justify_Left:
                return "AC Justify Left";
            case UC_AC_Justify_Center_H:
                return "AC Justify Center H";
            case UC_AC_Justify_Right:
                return "AC Justify Right";
            case UC_AC_Justify_Block_H:
                return "AC Justify Block H";
            case UC_AC_Justify_Top:
                return "AC Justify Top";
            case UC_AC_Justify_Center_V:
                return "AC Justify Center V";
            case UC_AC_Justify_Bottom:
                return "AC Justify Bottom";
            case UC_AC_Justify_Block_V:
                return "AC Justify Block V";
            case UC_AC_Indent_Decr:
                return "AC Indent Decrease";
            case UC_AC_Indent_Incr:
                return "AC Indent Increase";
            case UC_AC_Numbered_List:
                return "AC Numbered List";
            case UC_AC_Restart_Numbering:
                return "AC Restart Numbering";
            case UC_AC_Bulleted_List:
                return "AC Bulleted List";
            case UC_AC_Promote:
                return "AC Promote";
            case UC_AC_Demote:
                return "AC Demote";
            case UC_AC_Yes:
                return "AC Yes";
            case UC_AC_No:
                return "AC No";
            case UC_AC_Cancel:
                return "AC Cancel";
            case UC_AC_Catalog:
                return "AC Catalog";
            case UC_AC_BuyorCheckout:
                return "AC Buy/Checkout";
            case UC_AC_Add_to_Cart:
                return "AC Add to Cart";
            case UC_AC_Expand:
                return "AC Expand";
            case UC_AC_Expand_All:
                return "AC Expand All";
            case UC_AC_Collapse:
                return "AC Collapse";
            case UC_AC_Collapse_All:
                return "AC Collapse All";
            case UC_AC_Print_Preview:
                return "AC Print Preview";
            case UC_AC_Paste_Special:
                return "AC Paste Special";
            case UC_AC_Insert_Mode:
                return "AC Insert Mode";
            case UC_AC_Delete:
                return "AC Delete";
            case UC_AC_Lock:
                return "AC Lock";
            case UC_AC_Unlock:
                return "AC Unlock";
            case UC_AC_Protect:
                return "AC Protect";
            case UC_AC_Unprotect:
                return "AC Unprotect";
            case UC_AC_Attach_Comment:
                return "AC Attach Comment";
            case UC_AC_Delete_Comment:
                return "AC Delete Comment";
            case UC_AC_View_Comment:
                return "AC View Comment";
            case UC_AC_Sel_Word:
                return "AC Select Word";
            case UC_AC_Sel_Sentence:
                return "AC Select Sentence";
            case UC_AC_Sel_Paragraph:
                return "AC Select Paragraph";
            case UC_AC_Sel_Column:
                return "AC Select Column";
            case UC_AC_Sel_Row:
                return "AC Select Row";
            case UC_AC_Sel_Table:
                return "AC Select Table";
            case UC_AC_Sel_Object:
                return "AC Select Object";
            case UC_AC_Redo_Repeat:
                return "AC Redo/Repeat";
            case UC_AC_Sort:
                return "AC Sort";
            case UC_AC_Sort_Ascending:
                return "AC Sort Ascending";
            case UC_AC_Sort_Descending:
                return "AC Sort Descending";
            case UC_AC_Filter:
                return "AC Filter";
            case UC_AC_Set_Clock:
                return "AC Set Clock";
            case UC_AC_View_Clock:
                return "AC View Clock";
            case UC_AC_Sel_Time_Zone:
                return "AC Select Time Zone";
            case UC_AC_Edit_Time_Zones:
                return "AC Edit Time Zones";
            case UC_AC_Set_Alarm:
                return "AC Set Alarm";
            case UC_AC_Clear_Alarm:
                return "AC Clear Alarm";
            case UC_AC_Snooze_Alarm:
                return "AC Snooze Alarm";
            case UC_AC_Reset_Alarm:
                return "AC Reset Alarm";
            case UC_AC_Synchronize:
                return "AC Synchronize";
            case UC_AC_Send_or_Recv:
                return "AC Send/Receive";
            case UC_AC_Send_To:
                return "AC Send To";
            case UC_AC_Reply:
                return "AC Reply";
            case UC_AC_Reply_All:
                return "AC Reply All";
            case UC_AC_Forward_Msg:
                return "AC Forward Msg";
            case UC_AC_Send:
                return "AC Send";
            case UC_AC_Attach_File:
                return "AC Attach File";
            case UC_AC_Upload:
                return "AC Upload";
            case UC_AC_Download_Save_As:
                return "AC Download (Save Target As)";
            case UC_AC_Set_Borders:
                return "AC Set Borders";
            case UC_AC_Insert_Row:
                return "AC Insert Row";
            case UC_AC_Insert_Column:
                return "AC Insert Column";
            case UC_AC_Insert_File:
                return "AC Insert File";
            case UC_AC_Insert_Picture:
                return "AC Insert Picture";
            case UC_AC_Insert_Object:
                return "AC Insert Object";
            case UC_AC_Insert_Symbol:
                return "AC Insert Symbol";
            case UC_AC_Save_and_Close:
                return "AC Save and Close";
            case UC_AC_Rename:
                return "AC Rename";
            case UC_AC_Merge:
                return "AC Merge";
            case UC_AC_Split:
                return "AC Split";
            case UC_AC_Distribute_Horiz:
                return "AC Distribute Horizontally";
            case UC_AC_Distribute_Verti:
                return "AC Distribute Vertically";
            default:
                return "Unknown";
        }
    }

    public static string ToDigitizer(uint usage)
    {
        switch (usage)
        {
            case D_Digitizer:
                return "Digitizer";
            case D_Pen:
                return "Pen";
            case D_Light_Pen:
                return "Light Pen";
            case D_Touch_Screen:
                return "Touch Screen";
            case D_Touch_Pad:
                return "Touch Pad";
            case D_White_Board:
                return "White Board";
            case D_Coordinate_Measuring_Machine:
                return "Coordinate Measuring Machine";
            case D_4D_Digitizer:
                return "3D Digitizer";
            case D_Stereo_Plotter:
                return "Stereo Plotter";
            case D_Articulated_Arm:
                return "Articulated Arm";
            case D_Armature:
                return "Armature";
            case D_Multiple_Point_Digitizer:
                return "Multiple Point Digitizer ";
            case D_Free_Space_Wand:
                return "Free Space Wand";
            case D_Stylus:
                return "Stylus";
            case D_Puck:
                return "Puck";
            case D_Finger:
                return "Finger";
            case D_Tip_Pressure:
                return "Tip Pressure";
            case D_Barrel_Pressure:
                return "Barrel Pressure";
            case D_In_Range:
                return "In Range";
            case D_Touch:
                return "Touch";
            case D_Untouch:
                return "Untouch";
            case D_Tap:
                return "Tap";
            case D_Quality:
                return "Quality";
            case D_Data_Valid:
                return "Data Valid";
            case D_Transducer_Index:
                return "Transducer Index";
            case D_Tablet_Function_Keys:
                return "Tablet Function Keys";
            case D_Program_Change_Keys:
                return "Program Change Keys";
            case D_Battery_Strength:
                return "Battery Strength";
            case D_Invert:
                return "Invert";
            case D_X_Tilt:
                return "X Tilt";
            case D_Y_Tilt:
                return "Y Tilt";
            case D_Azimuth:
                return "Azimuth";
            case D_Altitude:
                return "Altitude ";
            case D_Twist:
                return "Twist ";
            case D_Tip_Switch:
                return "Tip Switch";
            case D_Secondary_Tip_Switch:
                return "Secondary Tip Switch ";
            case D_Barrel_Switch:
                return "Barrel Switch ";
            case D_Eraser:
                return "Eraser";
            case D_Tablet_Pick:
                return "Tablet Pick";
            default:
                return "Unknown";
        }
    }

    public static string ToPIDPage(uint usage)
    {
        switch (usage)
        {
            /* HID 1.11 Chapter 3 Table 1 says that
            ** USB Physical Interface Device definitions
            ** for force feedback and related decvices.
            ** This document didn't describe details.
            */
            default:
                return "Unknown";
        }
    }

    public static string ToUnicode(uint usage)
    {
        switch (usage)
        {
            /* see HID 1.11 Chapter 17
            */
            default:
                return "Unknown";
        }
    }

    public static string ToAlphanumericDisplay(uint usage)
    {
        switch (usage)
        {
            case AD_Alphanumeric_Display:
                return "Alphanumeric Display";
            case AD_Bitmapped_Display:
                return "Bitmapped Display";
            case AD_Display_Attributes_Report:
                return "Display Attributes Report";
            case AD_ASCII_Character_Set:
                return "ASCII Character Set";
            case AD_Data_Read_Back:
                return "Data Read Back";
            case AD_Font_Read_Back:
                return "Font Read Back";
            case AD_Display_Control_Report:
                return "Display Control Report";
            case AD_Clear_Display:
                return "Clear Display";
            case AD_Display_Enable:
                return "Display Enable";
            case AD_Screen_Saver_Delay:
                return "Screen Saver Delay";
            case AD_Screen_Saver_Enable:
                return "Screen Saver Enable";
            case AD_Vertical_Scroll:
                return "Vertical Scroll";
            case AD_Horizontal_Scroll:
                return "Horizontal Scroll";
            case AD_Character_Report:
                return "Character Report";
            case AD_Display_Data:
                return "Display Data";
            case AD_Display_Status:
                return "Display Status";
            case AD_Stat_Not_Ready:
                return "Stat Not Ready";
            case AD_Stat_Ready:
                return "Stat Ready";
            case AD_Err_Not_a_loadable_character:
                return "Err Not a loadable character";
            case AD_Err_Font_data_cannot_be_read:
                return "Err Font data cannot be read";
            case AD_Cursor_Position_Report:
                return "Cursor Position Report";
            case AD_Row:
                return "Row";
            case AD_Column:
                return "Column";
            case AD_Rows:
                return "Rows";
            case AD_Columns:
                return "Columns";
            case AD_Cursor_Pixel_Positioning:
                return "Cursor Pixel Positioning";
            case AD_Cursor_Mode:
                return "Cursor Mode";
            case AD_Cursor_Enable:
                return "Cursor Enable";
            case AD_Cursor_Blink:
                return "Cursor Blink";
            case AD_Font_Report:
                return "Font Report";
            case AD_Font_Data:
                return "Font Data";
            case AD_Character_Width:
                return "Character Width";
            case AD_Character_Height:
                return "Character Height";
            case AD_Character_Spacing_Horizontal:
                return "Character Spacing Horizontal";
            case AD_Character_Spacing_Vertical:
                return "Character Spacing Vertical";
            case AD_Unicode_Character_Set:
                return "Unicode Character Set";
            case AD_Font_7_Segment:
                return "Font 7-Segment";
            case AD_7_Segment_Direct_Map:
                return "7-Segment Direct Map";
            case AD_Font_14_Segment:
                return "Font 14-Segment";
            case AD_14_Segment_Direct_Map:
                return "14-Segment Direct Map";
            case AD_Display_Brightness:
                return "Display Brightness";
            case AD_Display_Contrast:
                return "Display Contrast";
            case AD_Character_Attribute:
                return "Character Attribute";
            case AD_Attribute_Readback:
                return "Attribute Readback";
            case AD_Attribute_Data:
                return "Attribute Data";
            case AD_Char_Attr_Enhance:
                return "Char Attr Enhance";
            case AD_Char_Attr_Underline:
                return "Char Attr Underline";
            case AD_Char_Attr_Blink:
                return "Char Attr Blink";
            case AD_Bitmap_Size_X:
                return "Bitmap Size X";
            case AD_Bitmap_Size_Y:
                return "Bitmap Size Y";
            case AD_Bit_Depth_Format:
                return "Bit Depth Format";
            case AD_Display_Orientation:
                return "Display Orientation";
            case AD_Palette_Report:
                return "Palette Report";
            case AD_Palette_Data_Size:
                return "Palette Data Size";
            case AD_Palette_Data_Offset:
                return "Palette Data Offset";
            case AD_Palette_Data:
                return "Palette Data";
            case AD_Blit_Report:
                return "Blit Report";
            case AD_Blit_Rectangle_X1:
                return "Blit Rectangle X1";
            case AD_Blit_Rectangle_Y1:
                return "Blit Rectangle Y1";
            case AD_Blit_Rectangle_X2:
                return "Blit Rectangle X2";
            case AD_Blit_Rectangle_Y2:
                return "Blit Rectangle Y2";
            case AD_Blit_Data:
                return "Blit Data";
            case AD_Soft_Button:
                return "Soft Button";
            case AD_Soft_Button_ID:
                return "Soft Button ID";
            case AD_Soft_Button_Side:
                return "Soft Button Side";
            case AD_Soft_Button_Offset_1:
                return "Soft Button Offset 1";
            case AD_Soft_Button_Offset_2:
                return "Soft Button Offset 2";
            case AD_Soft_Button_Report:
                return "Soft Button Report";
            default:
                return "Unknown";
        }
    }

    public static string ToMedicalInstruments(uint usage)
    {
        switch (usage)
        {
            case MI_Medical_Ultrasound:
                return "Medical Ultrasound";
            case MI_VCR_Acquisition:
                return "VCR/Acquisition";
            case MI_Freeze_Thaw:
                return "Freeze/Thaw";
            case MI_Clip_Store:
                return "Clip Store";
            case MI_Update:
                return "Update";
            case MI_Next:
                return "Next";
            case MI_Save:
                return "Save";
            case MI_Print:
                return "Print";
            case MI_Microphone_Enable:
                return "Microphone Enable";
            case MI_Cine:
                return "Cine";
            case MI_Transmit_Power:
                return "Transmit Power";
            case MI_Volume:
                return "Volume";
            case MI_Focus:
                return "Focus";
            case MI_Depth:
                return "Depth";
            case MI_Soft_Step_Primary:
                return "Soft Step - Primary";
            case MI_Soft_Step_Secondary:
                return "Soft Step - Secondary";
            case MI_Depth_Gain_Compensation:
                return "Depth Gain Compensation";
            case MI_Zoom_Select:
                return "Zoom Select";
            case MI_Zoom_Adjust:
                return "Zoom Adjust";
            case MI_Spectral_Doppler_Mode_Select:
                return "Spectral Doppler Mode Select";
            case MI_Spectral_Doppler_Adjust:
                return "Spectral Doppler Adjust";
            case MI_Color_Doppler_Mode_Select:
                return "Color Doppler Mode Select";
            case MI_Color_Doppler_Adjust:
                return "Color Doppler Adjust";
            case MI_Motion_Mode_Select:
                return "Motion Mode Select";
            case MI_Motion_Mode_Adjust:
                return "Motion Mode Adjust";
            case MI_2D_Mode_Select:
                return "2-D Mode Select";
            case MI_2D_Mode_Adjust:
                return "2-D Mode Adjust";
            case MI_Soft_Control_Select:
                return "Soft Control Select";
            case MI_Soft_Control_Adjust:
                return "Soft Control Adjust";
            default:
                return "Unknown";
        }
    }

    public static string ToMonitor(uint usage)
    {
        switch (usage)
        {
            /* HID 1.11 Chapter 3 Table 1 says that
            ** USB Device Class Definitrion for Monitor Device
            ** This document didn't describe details.
            */
            default:
                return "Unknown";
        }
    }

    public static string ToPower(uint usage)
    {
        switch (usage)
        {
            /* HID 1.11 Chapter 3 Table 1 says that
            ** USB Device Class Definitrion for Power Device
            ** This document didn't describe details.
            */
            default:
                return "Unknown";
        }
    }

    public static string ToBarCodeScanner(uint usage)
    {
        switch (usage)
        {
            /* HID 1.11 Chapter 3 Table 1 says that
            ** USB Device Class Definitrion for Point of Sale Device
            ** This document didn't describe details.
            */
            default:
                return "Unknown";
        }
    }

    public static string ToScale(uint usage)
    {
        switch (usage)
        {
            /* HID 1.11 Chapter 3 Table 1 says that
            ** USB Device Class Definitrion for Point of Sale Device
            ** This document didn't describe details.
            */
            default:
                return "Unknown";
        }
    }

    public static string ToMSRDevices(uint usage)
    {
        /* Megnetic Stripe Reading Device */
        switch (usage)
        {
            /* HID 1.11 Chapter 3 Table 1 says that
            ** USB Device Class Definitrion for Point of Sale Device
            ** This document didn't describe details.
            */
            default:
                return "Unknown";
        }
    }

    public static string ToCameraControl(uint usage)
    {
        switch (usage)
        {
            /* HID 1.11 Chapter 3 Table 1 says that
            ** USB Device Class Definitrion for Image Class Device
            ** This document didn't describe details.
            */
            default:
                return "Unknown";
        }
    }

    public static string ToArcade(uint usage)
    {
        switch (usage)
        {
            /* HID 1.11 Chapter 3 Table 1 says that
            ** OAAF Definitrion for arcade and coinop related Devices
            ** This document didn't describe details.
            */
            default:
                return "Unknown";
        }
    }

    public static string ToUsage(uint usagePage, uint usage)
    {
        switch (usagePage)
        {
            case UP_Generic_Desktop:
                return ToGenericDesktop(usage);
            case UP_Simulation_Controls:
                return ToSimulationControls(usage);
            case UP_VR_Controls:
                return ToVRControls(usage);
            case UP_Sport_Controls:
                return ToSportControls(usage);
            case UP_Game_Controls:
                return ToGameControls(usage);
            case UP_Generic_Device_Controls:
                return ToGenericDeviceControls(usage);
            case UP_Keyboard_or_Keypad:
                return ToKeyboardOrKeypad(usage);
            case UP_LEDs:
                return ToLEDs(usage);
            case UP_Button:
                return ToButton(usage);
            case UP_Ordinal:
                return ToOrdinal(usage);
            case UP_Telephony:
                return ToTelephony(usage);
            case UP_Consumer:
                return ToConsumer(usage);
            case UP_Digitizer:
                return ToDigitizer(usage);
            case UP_PID_Page:
                return ToPIDPage(usage);
            case UP_Unicode:
                return ToUnicode(usage);
            case UP_Alphanumeric_Display:
                return ToAlphanumericDisplay(usage);
            case UP_Medical_Instruments:
                return ToMedicalInstruments(usage);
            // case UP_Monitor_pages_1:
            // case UP_Monitor_pages_2:
            // case UP_Monitor_pages_3:
            // case UP_Monitor_pages_4:
            //     return ri_Monitor(usage);
            // case UP_Power_pages_1:
            // case UP_Power_pages_2:
            // case UP_Power_pages_3:
            // case UP_Power_pages_4:
            //     return ri_Power(usage);
            // case UP_Bar_Code_Scanner_page:
            //     return ri_BarCodeScanner(usage);
            // case UP_Scale_page:
            //     return ri_Scale(usage);
            // case UP_MSR_Devices:
            //     return ri_MSRDevices(usage);
            // case UP_Camera_Control_Page:
            //     return ri_CameraControl(usage);
            // case UP_Arcade_Page:
            //     return ri_Arcade(usage);
            default:
                return "Unknown";
        }
    }

    //Undefined 0x00U;
    public const uint UP_Generic_Desktop = 0x01U;
    public const uint UP_Simulation_Controls = 0x02U;
    public const uint UP_VR_Controls = 0x03U;
    public const uint UP_Sport_Controls = 0x04U;
    public const uint UP_Game_Controls = 0x05U;
    public const uint UP_Generic_Device_Controls = 0x06U;
    public const uint UP_Keyboard_or_Keypad = 0x07U;
    public const uint UP_LEDs = 0x08U;
    public const uint UP_Button = 0x09U;
    public const uint UP_Ordinal = 0x0AU;
    public const uint UP_Telephony = 0x0BU;
    public const uint UP_Consumer = 0x0CU;
    public const uint UP_Digitizer = 0x0DU;
    //Reversed 0x0EU;
    public const uint UP_PID_Page = 0x0FU;
    public const uint UP_Unicode = 0x10U;
    //Reversed 0x11U~0x13U;
    public const uint UP_Alphanumeric_Display = 0x14U;
    //Reversed 0x15U~0x3FU;
    public const uint UP_Medical_Instruments = 0x40U;
    //Reversed 0x41U~0x7FU;
    public const uint UP_Monitor_pages_1 = 0x80U;
    public const uint UP_Monitor_pages_2 = 0x81U;
    public const uint UP_Monitor_pages_3 = 0x82U;
    public const uint UP_Monitor_pages_4 = 0x83U;
    public const uint UP_Power_pages_1 = 0x84U;
    public const uint UP_Power_pages_2 = 0x85U;
    public const uint UP_Power_pages_3 = 0x86U;
    public const uint UP_Power_pages_4 = 0x87U;
    //Reversed 0x88U~0x8BU;
    public const uint UP_Bar_Code_Scanner_page = 0x8CU;
    public const uint UP_Scale_page = 0x8DU;

    public const uint UP_MSR_Devices = 0x8EU; /* Magnetic Stripe Reading */
//Reversed point for sale pages 0x8FU;
public const uint UP_Camera_Control_Page = 0x90U;
    public const uint UP_Arcade_Page = 0x91U;
    //Reversed 0x92U~0xFEFFU;
    //Vendor-defined 0xFF00U~0xFFFFU
}