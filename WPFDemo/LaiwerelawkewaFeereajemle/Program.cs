System.Windows.Forms.SendKeys.SendWait("Hello, World!");

/*
System.TypeInitializationException: The type initializer for 'System.Windows.Forms.SendKeys' threw an exception.
   ---> System.TypeInitializationException: The type initializer for 'System.Windows.Forms.ScaleHelper' threw an exception.
   ---> System.TypeLoadException: Could not load type 'System.Private.Windows.Core.OsVersion' from assembly 'System.Private.Windows.Core, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.
     at System.Windows.Forms.ScaleHelper.<InitializeStatics>g__GetPerMonitorAware|8_1()
     at System.Windows.Forms.ScaleHelper.InitializeStatics()
     --- End of inner exception stack trace ---
     at System.Windows.Forms.ScaleHelper.get_InitialSystemDpi()
     at System.Windows.Forms.Control..ctor(Boolean autoInstallSyncContext)
     at System.Windows.Forms.SendKeys.SKWindow..ctor()
     at System.Windows.Forms.SendKeys..cctor()
     --- End of inner exception stack trace ---
     at System.Windows.Forms.SendKeys.SendWait(String keys)
     at sendkey_error.MainWindow.Button_Click(Object sender, RoutedEventArgs e)
*/