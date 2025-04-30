namespace GelwhalhahonelGilerewalfee.Views;

static class UsagePageAndIdConverter
{
    public static string ConvertToString(ushort usagePage, ushort usageId)
    {
        // https://www.usb.org/sites/default/files/hut1_6.pdf
        // Digitizers Page (0x0D)
        /*
           Usage ID Usage Name Usage Types Section
           00 Undefined
           01 Digitizer CA 16.1
           02 Pen CA 16.1
           03 Light Pen CA 16.1
           04 Touch Screen CA 16.1
           05 Touch Pad CA 16.1
           06 Whiteboard CA 16.1
           07 Coordinate Measuring Machine CA 16.1
           08 3D Digitizer CA 16.1
           09 Stereo Plotter CA 16.1
           0A Articulated Arm CA 16.1
           0B Armature CA 16.1
           0C Multiple Point Digitizer CA 16.1
           0D Free Space Wand CA 16.1
           0E Device Configuration [7] CA 16.7
           0F Capacitive Heat Map Digitizer [54] CA 16.9
           10-1F Reserved
           20 Stylus [55] CA/CL 16.2
           21 Puck CL 16.2
           22 Finger CL 16.2
           23 Device settings [7] CL 16.7
           24 Character Gesture [45] CL 16.8
           25-2F Reserved
           30 Tip Pressure DV 16.3.1
           31 Barrel Pressure DV 16.3.1
           32 In Range MC 16.3.1
           33 Touch MC 16.3.1
           34 Untouch OSC 16.3.1
           35 Tap OSC 16.3.1
           36 Quality DV 16.3.1
           37 Data Valid MC 16.3.1
           38 Transducer Index DV 16.3.1
           39 Tablet Function Keys CL 16.3.1
           3A Program Change Keys CL 16.3.1
           3B Battery Strength DV 16.3.1
           3C Invert MC 16.3.1
           3D X Tilt DV 16.3.2
           3E Y Tilt DV 16.3.2
           3F Azimuth DV 16.3.3
           40 Altitude DV 16.3.3
           41 Twist DV 16.3.3
           42 Tip Switch MC 16.4
           43 Secondary Tip Switch MC 16.4
           44 Barrel Switch MC 16.4
           45 Eraser MC 16.4
           46 Tablet Pick MC 16.4
           47 Touch Valid [3] MC 16.5
           48 Width [3] DV 16.5
           49 Height [3] DV 16.5
           4A-50 Reserved
           51 Contact Identifier [7] DV 16.6
           52 Device Mode [7] DV 16.7
           53 Device Identifier [7] DV/SV 16.7
           54 Contact Count [7] DV 16.6
           55 Contact Count Maximum [7] SV 16.6
           56 Scan Time [51] DV 16.5
           57 Surface Switch [51] DF 16.5
           58 Button Switch [51] DF 16.5
           59 Pad Type [51] SF 16.5
           5A Secondary Barrel Switch [18] MC 16.4
           5B Transducer Serial Number [18] SV 16.3.1
           5C Preferred Color [25] DV 16.3.1
           5D Preferred Color is Locked [31] MC 16.3.1
           5E Preferred Line Width [31] DV 16.3.1
           5F Preferred Line Width is Locked [31] MC 16.3.1
           60 Latency Mode [51] DF 16.5
           61 Gesture Character Quality [45] DV 16.8
           62 Character Gesture Data Length [45] DV 16.8
           63 Character Gesture Data [45] DV 16.8
           64 Gesture Character Encoding [45] NAry 16.8
           65 UTF8 Character Gesture Encoding [45] Sel 16.8
           66 UTF16 Little Endian Character Gesture Encoding [45] Sel 16.8
           67 UTF16 Big Endian Character Gesture Encoding [45] Sel 16.8
           68 UTF32 Little Endian Character Gesture Encoding [45] Sel 16.8
           69 UTF32 Big Endian Character Gesture Encoding [45] Sel 16.8
           6A Capacitive Heat Map Protocol Vendor ID [54] SV 16.9
           6B Capacitive Heat Map Protocol Version [54] SV 16.9
           6C Capacitive Heat Map Frame Data [54] DV 16.9
           6D Gesture Character Enable [63] DF 16.8
           6E Transducer Serial Number Part 2 [70] SV 16.3.1
           6F No Preferred Color [71] DF 16.3.1
           70 Preferred Line Style [31] NAry 16.3.1
           71 Preferred Line Style is Locked [31] MC 16.3.1
           72 Ink [31] Sel 16.3.1
           73 Pencil [31] Sel 16.3.1
           74 Highlighter [31] Sel 16.3.1
           75 Chisel Marker [31] Sel 16.3.1
           76 Brush [31] Sel 16.3.1
           77 No Preference [31] Sel 16.3.1
           78-7F Reserved
           80 Digitizer Diagnostic [31] CL 16.7
           81 Digitizer Error [31] NAry 16.7
           82 Err Normal Status [31] Sel 16.7
           83 Err Transducers Exceeded [31] Sel 16.7
           84 Err Full Trans Features Unavailable [31] Sel 16.7
           85 Err Charge Low [31] Sel 16.7
           86-8F Reserved
           90 Transducer Software Info [36] CL 16.3.1
           91 Transducer Vendor Id [36] SV 16.3.1
           92 Transducer Product Id [36] SV 16.3.1
           93 Device Supported Protocols [36] NAry/CL 16.3.1
           94 Transducer Supported Protocols [36] NAry/CL 16.3.1
           95 No Protocol [36] Sel 16.3.1
           96 Wacom AES Protocol [36] Sel 16.3.1
           97 USI Protocol [36] Sel 16.3.1
           98 Microsoft Pen Protocol [55] Sel 16.3.1
           99-9F Reserved
           A0 Supported Report Rates [36] SV/CL 16.3.1
           A1 Report Rate [36] DV 16.3.1
           A2 Transducer Connected [36] SF 16.3.1
           A3 Switch Disabled [36] Sel 16.3.1
           A4 Switch Unimplemented [36] Sel 16.3.1
           A5 Transducer Switches [36] CL 16.3.1
           A6 Transducer Index Selector [75] DV 16.3.1
           A7-AF Reserved
           B0 Button Press Threshold [78] DV 16.5
           B1-FFFF Reserved
           
         */
        if ((HidUsagePage) usagePage == HidUsagePage.Digitizer)
        {
            var usageIdText = usageId switch
            {
                0x01 => "Digitizer",
                0x02 => "Pen",
                0x03 => "Light Pen",
                0x04 => "Touch Screen",
                0x05 => "Touch Pad",
                0x06 => "Whiteboard",
                0x08 => "3D Digitizer",
                0x09 => "Stereo Plotter",
                0x20 => "Stylus",
                0x22 => "Finger",
                0x30 => "Tip Pressure",
                0x31 => "Barrel Pressure",
                0x32 => "In Range",
                0x33 => "Touch",
                0x34 => "Untouch",
                0x35 => "Tap",
                0x36 => "Quality",
                0x37 => "Data Valid",
                0x38 => "Transducer Index",
                0x39 => "Tablet Function Keys",
                0x3A => "Program Change Keys",
                0x3B => "Battery Strength",
                0x3C => "Invert",
                0x3D => "X Tilt",
                0x3E => "Y Tilt",
                0x3F => "Azimuth",
                0x40 => "Altitude",
                0x41 => "Twist",
                0x42 => "Tip Switch",
                0x43 => "Secondary Tip Switch",
                0x44 => "Barrel Switch",
                0x45 => "Eraser",
                0x46 => "Tablet Pick",
                0x47 => "Touch Valid",
                0x48 => "Width",
                0x49 => "Height",
                0x51 => "Contact Identifier",
                0x52 => "Device Mode",
                0x53 => "Device Identifier",
                0x54 => "Contact Count",
                0x55 => "Contact Count Maximum",
                0x56 => "Scan Time",
                0x57 => "Surface Switch",
                0x58 => "Button Switch",
                0x59 => "Pad Type",
                0x5A => "Secondary Barrel Switch",
                0x5B => "Transducer Serial Number",
                0x5C => "Preferred Color",
                0x5D => "Preferred Color is Locked",
                0x5E => "Preferred Line Width",
                0x5F => "Preferred Line Width is Locked",
                0x60 => "Latency Mode",
                0x61 => "Gesture Character Quality",
                0x62 => "Character Gesture Data Length",
                0x63 => "Character Gesture Data",
                0x64 => "Gesture Character Encoding",
                0x65 => "UTF8 Character Gesture Encoding",
                0x66 => "UTF16 Little Endian Character Gesture Encoding",
                0x67 => "UTF16 Big Endian Character Gesture Encoding",
                0x68 => "UTF32 Little Endian Character Gesture Encoding",
                0x69 => "UTF32 Big Endian Character Gesture Encoding",
                0x6A => "Capacitive Heat Map Protocol Vendor ID",
                0x6B => "Capacitive Heat Map Protocol Version",
                0x6C => "Capacitive Heat Map Frame Data",
                0x6D => "Gesture Character Enable",
                0x6E => "Transducer Serial Number Part 2",
                0x6F => "No Preferred Color",
                0x70 => "Preferred Line Style",
                0x71 => "Preferred Line Style is Locked",
                0x72 => "Ink",
                0x73 => "Pencil",
                0x74 => "Highlighter",
                0x75 => "Chisel Marker",
                0x76 => "Brush",
                0x77 => "No Preference",
                0x80 => "Digitizer Diagnostic",
                0x81 => "Digitizer Error",
                0x82 => "Err Normal Status",
                _ => $"0x{usageId:X2}"
            };
            return $"UsagePageId={(HidUsagePage) usagePage}({usagePage}) UsageId={usageIdText}({usageId})";
        }

        return $"UsagePageId={(HidUsagePage) usagePage}({usagePage}) UsageId={(HidUsage) usageId}({usageId})";
    }
}