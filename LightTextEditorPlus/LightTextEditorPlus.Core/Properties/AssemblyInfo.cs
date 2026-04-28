using System.Resources;
using System.Runtime.CompilerServices;

[assembly: NeutralResourcesLanguage("zh-Hans")]
[assembly: InternalsVisibleTo("LightTextEditorPlus.Core.TestsFramework")]
[assembly: InternalsVisibleTo("LightTextEditorPlus.Core.Tests")]
#if USE_SKIA || USE_AVALONIA || USE_Standard
[assembly: InternalsVisibleTo("LightTextEditorPlus.Skia.UnitTests")]
#endif

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
