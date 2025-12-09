namespace QuantLab.MarketData.Hub.Services.Download.Ibkr;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using IBApi;
using QuantLab.MarketData.Hub.Models.Domain;
using IBApiBar = IBApi.Bar;
using QuantLabBar = QuantLab.MarketData.Hub.Models.Domain.Bar;

public class IbkrTwsService : EWrapper
{
    private readonly ILogger<IbkrTwsService> _logger;
    private readonly EClientSocket _client;
    private readonly EReaderMonitorSignal _signal;
    private int _nextRequestId = 0;

    private string _symbol;
    private BarInterval _barInterval;
    private TaskCompletionSource<List<QuantLabBar>> _tcs;
    private List<QuantLabBar> _bars;

    public IbkrTwsService(ILogger<IbkrTwsService> logger)
    {
        _logger = logger;
        _signal = new EReaderMonitorSignal();
        _client = new EClientSocket(this, _signal);

        _symbol = string.Empty;
        _bars = [];
        _tcs = new TaskCompletionSource<List<QuantLabBar>>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        _client.eConnect("127.0.0.1", 7496, 1, false);

        var reader = new EReader(_client, _signal);
        reader.Start();

        new Thread(() =>
        {
            while (_client.IsConnected())
            {
                _signal.waitForSignal();
                reader.processMsgs();
            }
        })
        {
            IsBackground = true,
        }.Start();
    }

    public Task<List<QuantLabBar>> GetTwsHistoricalDataAsync(
        Contract contract,
        string durationStr,
        string barSizeSetting
    )
    {
        int reqId = ++_nextRequestId;

        _tcs = new TaskCompletionSource<List<QuantLabBar>>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _symbol = contract.Symbol;
        _barInterval = GetBarInterval(barSizeSetting);
        _bars = [];

        _client.reqHistoricalData(
            reqId,
            contract,
            "",
            durationStr,
            barSizeSetting,
            "TRADES",
            0,
            1,
            false,
            null
        );

        return _tcs.Task;
    }

    public void historicalData(int reqId, IBApiBar bar)
    {
        var quantLabBar = new QuantLabBar(
            _symbol,
            _barInterval,
            ToIST(bar.Time),
            (decimal)bar.Open,
            (decimal)bar.High,
            (decimal)bar.Low,
            (decimal)bar.Close,
            (int)bar.Volume
        );
        _bars.Add(quantLabBar);
    }

    private static BarInterval GetBarInterval(string barSizeSetting)
    {
        return barSizeSetting switch
        {
            "5 mins" => BarInterval.FiveMinutes,
            "15 mins" => BarInterval.FifteenMinutes,
            "1 day" => BarInterval.OneDay,
            _ => BarInterval.FiveMinutes,
        };
    }

    private static string ToIST(string time)
    {
        var dateFormat = time.Contains(' ') ? "yyyyMMdd  HH:mm:ss" : "yyyyMMdd";
        DateTime parsed = DateTime.ParseExact(time, dateFormat, CultureInfo.InvariantCulture);
        TimeZoneInfo cetZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(parsed, cetZone);
        TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        DateTime istTime = TimeZoneInfo.ConvertTime(utcTime, istZone);
        var result = time.Contains(' ')
            ? istTime.ToString("yyyy-MM-dd HH:mm:ss")
            : $"{istTime:yyyy-MM-dd} 09:15:00";
        return result;
    }

    public void historicalDataEnd(int reqId, string start, string end)
    {
        _tcs.TrySetResult(_bars);
        _symbol = string.Empty;
        _bars = [];
    }

    public void error(int reqId, int errorCode, string errorMsg)
    {
        if (errorCode == 2104 || errorCode == 2106 || errorCode == 2158)
        {
            _logger.LogInformation("IBKR TWS API - {errorCode}: {errorMsg}", errorCode, errorMsg);
            return;
        }
        _logger.LogError("IBKR TWS API Error {errorCode}: {errorMsg}", errorCode, errorMsg);
        _tcs.TrySetException(new Exception($"IBKR Error {errorCode}: {errorMsg}"));
    }

    public void error(Exception e)
    {
        _logger.LogError("IBKR TWS API Error : {e}", e);
    }

    public void connectionClosed()
    {
        _logger.LogInformation("IBKR TWS API Connection Closed");
    }

    public void error(string str)
    {
        _logger.LogError("IBKR TWS API Error : {str}", str);
    }

    public void error(
        int id,
        long errorTime,
        int errorCode,
        string errorMsg,
        string advancedOrderRejectJson
    )
    {
        _logger.LogError(
            "IBKR TWS API Error {id} : {errorTime} : {errorCode}: {errorMsg} : {advancedOrderRejectJson}",
            id,
            errorTime,
            errorCode,
            errorMsg,
            advancedOrderRejectJson
        );
    }

    public void nextValidId(int orderId)
    {
        _logger.LogInformation("Connected! nextValidId={OrderId}", orderId);
        _nextRequestId = orderId;
    }

    public void connectAck()
    {
        if (_client.AsyncEConnect)
            _client.startApi();
    }

    public void currentTime(long time) { }

    public void tickPrice(int tickerId, int field, double price, TickAttrib attribs) { }

    public void tickSize(int tickerId, int field, decimal size) { }

    public void tickString(int tickerId, int field, string value) { }

    public void tickGeneric(int tickerId, int field, double value) { }

    public void tickEFP(
        int tickerId,
        int tickType,
        double basisPoints,
        string formattedBasisPoints,
        double impliedFuture,
        int holdDays,
        string futureLastTradeDate,
        double dividendImpact,
        double dividendsToLastTradeDate
    ) { }

    public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract) { }

    public void tickOptionComputation(
        int tickerId,
        int field,
        int tickAttrib,
        double impliedVolatility,
        double delta,
        double optPrice,
        double pvDividend,
        double gamma,
        double vega,
        double theta,
        double undPrice
    ) { }

    public void tickSnapshotEnd(int tickerId) { }

    public void managedAccounts(string accountsList) { }

    public void accountSummary(
        int reqId,
        string account,
        string tag,
        string value,
        string currency
    ) { }

    public void accountSummaryEnd(int reqId) { }

    public void bondContractDetails(int reqId, ContractDetails contract) { }

    public void updateAccountValue(
        string key,
        string value,
        string currency,
        string accountName
    ) { }

    public void updatePortfolio(
        Contract contract,
        decimal position,
        double marketPrice,
        double marketValue,
        double averageCost,
        double unrealizedPNL,
        double realizedPNL,
        string accountName
    ) { }

    public void updateAccountTime(string timestamp) { }

    public void accountDownloadEnd(string account) { }

    public void orderStatus(
        int orderId,
        string status,
        decimal filled,
        decimal remaining,
        double avgFillPrice,
        long permId,
        int parentId,
        double lastFillPrice,
        int clientId,
        string whyHeld,
        double mktCapPrice
    ) { }

    public void openOrder(int orderId, Contract contract, Order order, OrderState orderState) { }

    public void openOrderEnd() { }

    public void contractDetails(int reqId, ContractDetails contractDetails) { }

    public void contractDetailsEnd(int reqId) { }

    public void execDetails(int reqId, Contract contract, Execution execution) { }

    public void execDetailsEnd(int reqId) { }

    public void fundamentalData(int reqId, string data) { }

    public void historicalDataUpdate(int reqId, IBApiBar bar) { }

    public void marketDataType(int reqId, int marketDataType) { }

    public void updateMktDepth(
        int tickerId,
        int position,
        int operation,
        int side,
        double price,
        decimal size
    ) { }

    public void updateMktDepthL2(
        int tickerId,
        int position,
        string marketMaker,
        int operation,
        int side,
        double price,
        decimal size,
        bool isSmartDepth
    ) { }

    public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange) { }

    public void position(string account, Contract contract, decimal pos, double avgCost) { }

    public void positionEnd() { }

    public void realtimeBar(
        int reqId,
        long date,
        double open,
        double high,
        double low,
        double close,
        decimal volume,
        decimal WAP,
        int count
    ) { }

    public void scannerParameters(string xml) { }

    public void scannerData(
        int reqId,
        int rank,
        ContractDetails contractDetails,
        string distance,
        string benchmark,
        string projection,
        string legsStr
    ) { }

    public void scannerDataEnd(int reqId) { }

    public void receiveFA(int faDataType, string faXmlData) { }

    public void verifyMessageAPI(string apiData) { }

    public void verifyCompleted(bool isSuccessful, string errorText) { }

    public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge) { }

    public void verifyAndAuthCompleted(bool isSuccessful, string errorText) { }

    public void displayGroupList(int reqId, string groups) { }

    public void displayGroupUpdated(int reqId, string contractInfo) { }

    public void positionMulti(
        int requestId,
        string account,
        string modelCode,
        Contract contract,
        decimal pos,
        double avgCost
    ) { }

    public void positionMultiEnd(int requestId) { }

    public void accountUpdateMulti(
        int requestId,
        string account,
        string modelCode,
        string key,
        string value,
        string currency
    ) { }

    public void accountUpdateMultiEnd(int requestId) { }

    public void securityDefinitionOptionParameter(
        int reqId,
        string exchange,
        int underlyingConId,
        string tradingClass,
        string multiplier,
        HashSet<string> expirations,
        HashSet<double> strikes
    ) { }

    public void securityDefinitionOptionParameterEnd(int reqId) { }

    public void softDollarTiers(int reqId, SoftDollarTier[] tiers) { }

    public void familyCodes(FamilyCode[] familyCodes) { }

    public void symbolSamples(int reqId, ContractDescription[] contractDescriptions) { }

    public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions) { }

    public void tickNews(
        int tickerId,
        long timeStamp,
        string providerCode,
        string articleId,
        string headline,
        string extraData
    ) { }

    public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap) { }

    public void tickReqParams(
        int tickerId,
        double minTick,
        string bboExchange,
        int snapshotPermissions
    ) { }

    public void newsProviders(NewsProvider[] newsProviders) { }

    public void newsArticle(int requestId, int articleType, string articleText) { }

    public void historicalNews(
        int requestId,
        string time,
        string providerCode,
        string articleId,
        string headline
    ) { }

    public void historicalNewsEnd(int requestId, bool hasMore) { }

    public void headTimestamp(int reqId, string headTimestamp) { }

    public void histogramData(int reqId, HistogramEntry[] data) { }

    public void rerouteMktDataReq(int reqId, int conId, string exchange) { }

    public void rerouteMktDepthReq(int reqId, int conId, string exchange) { }

    public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements) { }

    public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL) { }

    public void pnlSingle(
        int reqId,
        decimal pos,
        double dailyPnL,
        double unrealizedPnL,
        double realizedPnL,
        double value
    ) { }

    public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done) { }

    public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done) { }

    public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done) { }

    public void tickByTickAllLast(
        int reqId,
        int tickType,
        long time,
        double price,
        decimal size,
        TickAttribLast tickAttribLast,
        string exchange,
        string specialConditions
    ) { }

    public void tickByTickBidAsk(
        int reqId,
        long time,
        double bidPrice,
        double askPrice,
        decimal bidSize,
        decimal askSize,
        TickAttribBidAsk tickAttribBidAsk
    ) { }

    public void tickByTickMidPoint(int reqId, long time, double midPoint) { }

    public void orderBound(long permId, int clientId, int orderId) { }

    public void completedOrder(Contract contract, Order order, OrderState orderState) { }

    public void completedOrdersEnd() { }

    public void replaceFAEnd(int reqId, string text) { }

    public void wshMetaData(int reqId, string dataJson) { }

    public void wshEventData(int reqId, string dataJson) { }

    public void userInfo(int reqId, string whiteBrandingId) { }

    public void currentTimeInMillis(long timeInMillis) { }

    public void tickSize(int tickerId, int field, int size) { }

    public void tickOptionComputation(
        int tickerId,
        int field,
        double impliedVolatility,
        double delta,
        double optPrice,
        double pvDividend,
        double gamma,
        double vega,
        double theta,
        double undPrice
    ) { }

    public void updatePortfolio(
        Contract contract,
        double position,
        double marketPrice,
        double marketValue,
        double averageCost,
        double unrealizedPNL,
        double realizedPNL,
        string accountName
    ) { }

    public void orderStatus(
        int orderId,
        string status,
        double filled,
        double remaining,
        double avgFillPrice,
        int permId,
        int parentId,
        double lastFillPrice,
        int clientId,
        string whyHeld,
        double mktCapPrice
    ) { }

    public void commissionReport(CommissionReport commissionReport) { }

    public void updateMktDepth(
        int tickerId,
        int position,
        int operation,
        int side,
        double price,
        int size
    ) { }

    public void updateMktDepthL2(
        int tickerId,
        int position,
        string marketMaker,
        int operation,
        int side,
        double price,
        int size,
        bool isSmartDepth
    ) { }

    public void position(string account, Contract contract, double pos, double avgCost) { }

    public void realtimeBar(
        int reqId,
        long date,
        double open,
        double high,
        double low,
        double close,
        long volume,
        double WAP,
        int count
    ) { }

    public void positionMulti(
        int requestId,
        string account,
        string modelCode,
        Contract contract,
        double pos,
        double avgCost
    ) { }

    public void pnlSingle(
        int reqId,
        int pos,
        double dailyPnL,
        double unrealizedPnL,
        double realizedPnL,
        double value
    ) { }

    public void tickByTickAllLast(
        int reqId,
        int tickType,
        long time,
        double price,
        int size,
        TickAttribLast tickAttriblast,
        string exchange,
        string specialConditions
    ) { }

    public void tickByTickBidAsk(
        int reqId,
        long time,
        double bidPrice,
        double askPrice,
        int bidSize,
        int askSize,
        TickAttribBidAsk tickAttribBidAsk
    ) { }
}
