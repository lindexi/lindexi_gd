namespace WifaberfearnairWherneyeaquqaki;

public static class AppBuilderExtensions
{
    public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder) =>
        builder
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("WifaberfearnairWherneyeaquqaki/Assets/Fonts/OpenSansRegular.ttf", "OpenSansRegular");
                fonts.AddFont("WifaberfearnairWherneyeaquqaki/Assets/Fonts/OpenSansSemibold.ttf", "OpenSansSemibold");
            });
}
