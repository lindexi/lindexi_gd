// See https://aka.ms/new-console-template for more information
/*
 	    System.Private.CoreLib.dll!System.Threading.Monitor.Wait(object obj, int millisecondsTimeout)
    	System.Private.CoreLib.dll!System.Threading.ManualResetEventSlim.Wait(int millisecondsTimeout = -1, System.Threading.CancellationToken cancellationToken)
    	System.Private.CoreLib.dll!System.Threading.Tasks.Task.SpinThenBlockingWait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken)
    	System.Private.CoreLib.dll!System.Threading.Tasks.Task.InternalWaitCore(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken)
    	System.Private.CoreLib.dll!System.Threading.Tasks.Task.InternalWait(int millisecondsTimeout = -1, System.Threading.CancellationToken cancellationToken = IsCancellationRequested = false)
    	System.Private.CoreLib.dll!System.Threading.Tasks.Task.Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken)
    	System.Private.CoreLib.dll!System.Threading.Tasks.Task.Wait()
   >	JerechairlallwuHerekuchurde.dll!Program.<Main>$(string[] args = {string[0]})
   
*/
var taskCompletionSource = new TaskCompletionSource();
taskCompletionSource.Task.Wait();
Console.WriteLine("Hello, World!");
