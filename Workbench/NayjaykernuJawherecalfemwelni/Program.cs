// See https://aka.ms/new-console-template for more information

var gestureThresholdSettings = new GestureThresholdSettings();
gestureThresholdSettings.HoldingDuration = TimeSpan.FromMilliseconds(1000);

Console.WriteLine("Hello, World!");

record GestureThresholdSettings
{
    public TimeSpan HoldingDuration = TimeSpan.FromMilliseconds(800);
}