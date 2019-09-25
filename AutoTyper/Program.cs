namespace AutoTyper
{
    #region Usings

    using System;
    using System.Windows.Forms;

    #endregion

    internal static class Program
    {
        #region Méthodes privées

        /// <summary>
        ///     Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        private static void Main(params string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var scenario = "AutoTyper.xml";
            if (args.Length >= 1)
            {
                scenario = args[0];
            }

            using (var mainForm = new MainForm(scenario))
            {
                Application.Run(mainForm);
            }
        }

        #endregion
    }
}
