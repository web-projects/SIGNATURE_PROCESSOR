using System;

namespace SignatureProcessorApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string [] args)
        {
            App app = new App();
            app.Run(new MainWindow());
        }
    }
}
