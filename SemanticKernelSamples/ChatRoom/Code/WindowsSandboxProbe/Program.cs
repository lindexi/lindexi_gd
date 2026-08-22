const string MarkerContent = "windows-sandbox-integration-success";

string markerPath = Path.Combine(AppContext.BaseDirectory, "success.marker");
await File.WriteAllTextAsync(markerPath, MarkerContent);
Console.WriteLine(MarkerContent);
return 0;
