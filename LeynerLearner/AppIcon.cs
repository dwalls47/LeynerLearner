namespace LeynerLearner
{
    /// <summary>The app's icon (a lamed; LeynerLearner.ico, made by Tools\AppIcon\make-icon.ps1), for every window's title bar.</summary>
    static class AppIcon
    {
        static Icon? icon;

        public static Icon Value => icon ??= new Icon(
            typeof(AppIcon).Assembly.GetManifestResourceStream("LeynerLearner.ico")!);
    }
}
