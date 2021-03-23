using McMaster.Extensions.CommandLineUtils;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StonkAtlas.QTLogger
{
    class Program
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "Path to token file")]
        public string TokenPath { get; } = "./token.json";

        [Option(Description = "Refresh Token from the questrade website")]
        public string RefreshToken { get; } = "";

        private void OnExecute()
        {
            Console.WriteLine("Press F1 to exit");

            QTLogger qtlogger = new QTLogger(TokenPath, RefreshToken);
            qtlogger.AddSymbol("",2000);
            qtlogger.Start();
            //Begin console loop
            //Hold console open if program tries to exit
            bool userWantsToExit = false;
            while (!userWantsToExit)
            {
                Thread.Sleep(100);
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.F1:
                            userWantsToExit = true;
                            break;
                        case ConsoleKey.F2:
                            try
                            {
                                Console.WriteLine("Type Search Term:");
                                var searchTerm = Console.ReadLine();
                                qtlogger.GetSymbol(searchTerm);
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, "Error");
                            }
                            break;
                        case ConsoleKey.F3:
                            //Begin symbol searching
                            try
                            {
                                foreach (var s in USSymbolList.symbols)
                                {
                                    qtlogger.GetSymbol(s);
                                    Thread.Sleep(300);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, "Error");
                            }
                            break;
                        case ConsoleKey.F4:
                            qtlogger.SaveSymbol();
                            break;
                        case ConsoleKey.F5:
                            //BigQuery bq = new BigQuery("cloud1-colin-1");
                            break;
                        default:
                            logger.Info("Key not recognized.");
                            break;
                    }
                }
                LogManager.LogFactory.Flush();
            }
        }
    }
}
