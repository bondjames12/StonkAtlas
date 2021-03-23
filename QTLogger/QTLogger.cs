using NLog;
using StonkAtlas.QTLogger.QuestradeAPI;
using StonkAtlas.QTLogger.QuestradeAPI.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace StonkAtlas.QTLogger
{
    public class QTLogger
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private string _tokenPath;
        private bool _isRunning = false;
        private bool _isAuthenticated = false;
        private Questrade _qTrade;
        private List<System.Timers.Timer> _timers = new List<System.Timers.Timer>();
        private SymbolCache _symbolCache = new SymbolCache();
        object _lockObj = new object();

        public QTLogger(string tokenpath, string refreshToken)
        {
            _tokenPath = Path.GetFullPath(tokenpath);
            _logger.Info($"Loading Refresh Token from: {_tokenPath}");
            string token = "";
            AuthenticateResp tokens = null;
            try
            {
                tokens = JsonSerializer.Deserialize<AuthenticateResp>(File.ReadAllText(_tokenPath));
            }
            catch (Exception) { }

            if (!string.IsNullOrWhiteSpace(refreshToken))
                token = refreshToken;
            else
            {
                if (tokens != null)
                    token = tokens.refresh_token;
            }

            _qTrade = new Questrade(token);

            //Add method to events when raised
            _qTrade.OnSuccessfulAuthentication += QTrade_OnSuccessfulAuthentication;
            _qTrade.OnUnsuccessfulAuthentication += QTrade_OnUnsuccessfulAuthentication;
            _qTrade.OnAccountsRecieved += QTrade_OnAccountsRecieved;
            _qTrade.OnQuoteStreamRecieved += QTrade_OnStreamRecieved;
            _qTrade.OnOrderNotifRecieved += QTrade_OnOrderNotifRecieved;
            _qTrade.OnGeneralErrorRecieved += QTrade_OnGeneralErrorRecieved;
            _qTrade.OnSymbolSearchRecieved += QTrade_OnSymbolSearchRecieved;


            //Subscribe to Level 1 data stream
            //string symbolId = "5953026";
            //Starts stream. Return object is used for error handling.
            //Task.Run(() => qTrade.StreamQuoteAsync(symbolId, new CancellationTokenSource()));

            //Subscribe to notification stream
            // Task.Run(() => qTrade.SubToOrderNotifAsync(new CancellationTokenSource()));
        }

        public void Start()
        {
            Task.Run(() => _qTrade.Authenticate()); //Make authentication
        }

        /// <summary>
        /// After authentication is logger is not running this is executed
        /// </summary>
        public void Run()
        {
            foreach (var t in _timers)
            {
                t.Start();
            }
            _isRunning = true;
        }

        public void Stop()
        {
            foreach(var t in _timers)
            {
                t.Stop();
            }
            _isRunning = false;
        }

        public void Restart()
        {
            if (_isRunning) Stop();
            Start();
        }

        /// <summary>
        /// Add a symbol to the logger
        /// </summary>
        public void AddSymbol(string symbol, float intervalms, bool startImmediately = false)
        {
            //Add a timer to start logging this symbol
            var timer = new System.Timers.Timer(intervalms);
            timer.Elapsed += OnTimedEvent;
            if (startImmediately) timer.Start();
            _timers.Add(timer);
        }

        public void SaveSymbol()
        {
            string syms = System.Text.Json.JsonSerializer.Serialize(_symbolCache._newDiscoveredSymbols);
            File.WriteAllText(Path.GetFullPath("./symbols.json"), syms);
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            _logger.Info("The Elapsed event was raised at {0}", e.SignalTime);
            Task.Run(() => _qTrade.GetAccountsAsync(new CancellationTokenSource()));
        }

        public void GetSymbol(string searchTerm)
        {
            Task.Run(() => _qTrade.GetSymbolsAsync(searchTerm, new CancellationTokenSource()));
        }

        private void QTrade_OnSymbolSearchRecieved(object sender, APISymbolSearchReturnArgs e)
        {
            _symbolCache.Add(e.symbols.symbols);
        }

        private void QTrade_OnStreamRecieved(object sender, APIStreamQuoteRecievedArgs e)
        {
            var quoteResp = e.quotes;
            for (int i = 0; i < quoteResp.quotes.Length; i++)
            {
                _logger.Info(string.Format("{0} - Bid: {1}, BidSize: {2}, Ask: {3}, AskSize: {4}",
                e.time.ToString("HH:mm:ss"), quoteResp.quotes[i].bidPrice, quoteResp.quotes[i].bidSize, quoteResp.quotes[i].askPrice, quoteResp.quotes[i].askSize));
            }
        }

        private void QTrade_OnOrderNotifRecieved(object sender, APIOrderNotificationRecievedArg e)
        {
            for (int i = 0; i < e.OrderNotif.orders.Length; i++)
            {
                _logger.Info(string.Format("{0} - Account: {1}, Symbol: {2}", e.time.ToString("HH:mm:ss"), e.OrderNotif.accountNumber, e.OrderNotif.orders[i].symbol));
            }
        }

        private void QTrade_OnUnsuccessfulAuthentication(object sender, UnsuccessfulAuthArgs e)
        {
            _logger.Info("Authentication unsuccessful. " + e.resp.ReasonPhrase);
            _isAuthenticated = false;
            Task.Run(() => _qTrade.Authenticate());
        }

        private void QTrade_OnSuccessfulAuthentication(object sender, SuccessAuthEventArgs e)
        {
            _logger.Info(string.Format("Access token will expire on: {0} {1}", e.response.expires_in_date.ToLongDateString(), e.response.expires_in_date.ToLongTimeString()));
            lock (_lockObj)
            {
                //Save token to file
                System.IO.File.WriteAllText(_tokenPath, JsonSerializer.Serialize<AuthenticateResp>(e.response));
                _isAuthenticated = true;
                if (!_isRunning) Run();
            }
        }


        private void QTrade_OnAccountsRecieved(object sender, APIAccountsReturnArgs e)
        {
            for (int i = 0; i < e.accounts.accounts.Length; i++)
            {
                _logger.Info(string.Format("{0} {1} : {2}"
                    , e.accounts.accounts[i].clientAccountType, e.accounts.accounts[i].type, e.accounts.accounts[i].number));

            }
        }

        private void QTrade_OnOrderProcessingErrorRecieved(object sender, OrderProcessingErrorEventArgs e)
        {
            _logger.Info(string.Format("Error code: {0}. {1} Order ID: {2}", e.OrderProcesssingErrorResp.code, e.OrderProcesssingErrorResp.message, e.OrderProcesssingErrorResp.orderId));
        }

        private void QTrade_OnGeneralErrorRecieved(object sender, GeneralErrorEventArgs e)
        {
            _logger.Error(string.Format("Error code: {0}. {1}", e.GeneralErrorResp.code, e.GeneralErrorResp.message));
            _isAuthenticated = false;
            //Attempt to restart QT Logger
            Restart();
        }
    }
}
