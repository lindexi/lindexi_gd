// Assume we have a function pointer to an unmanaged function
using System.Runtime.InteropServices;

IntPtr functionPointer = GetFunctionPointer();

// Create a delegate instance from the function pointer
UnmanagedFunction function = (UnmanagedFunction) Marshal.GetDelegateForFunctionPointer(functionPointer, typeof(UnmanagedFunction));

// Call the unmanaged function using the delegate
int result = function(5, 10);

IntPtr GetFunctionPointer() => IntPtr.Zero;

// Define the delegate matching the unmanaged function signature
delegate int UnmanagedFunction(int a, int b);