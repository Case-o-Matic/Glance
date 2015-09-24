using System;

namespace Glance.Editor
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        public static bool ShowInternalInformation { get; private set; }
        public static bool IsDebugModeActivated { get; private set; }
        public static bool IsTelemetryActivated { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            #if DEBUG
            ShowInternalInformation = true;
            #endif
            EvaluateCommandArgs(Environment.GetCommandLineArgs());

            using (var game = new EditorGame())
            {
                game.Exiting += OnGameExit;
                game.Run();
            }
        }

        private static void EvaluateCommandArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-Debug":
                        IsDebugModeActivated = true;
                        break;

                    case "-Telemetry":
                        IsTelemetryActivated = false;
                        break;
                }
            }
        }

        private static void OnGameExit(object sender, EventArgs e)
        {
            // TODO: Send telemetry data
        }
    }
#endif
}
