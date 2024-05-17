using QuikSharp;
using QuikSharp.DataStructures;
using QuikSharp.DataStructures.Transaction; 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using QUIKSharpTEST2;
using Condition = QuikSharp.DataStructures.Condition;

//
//  Уроки C# – Синтаксис, Директивы, Классы, Методы – Урок 2 
//***********************************************************
public class Tool : ViewModelBase //: MainWindow // <--наследование https://youtu.be/MZ0og1DNcCg?si=XMlLSsIsn5CqSXCU&t=1920
{

    private readonly Quik _quik;
    private readonly char separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
    //public MainWindow wnd => (MainWindow)Application.Current.MainWindow;
    //MainWindow wnd = (MainWindow)App.Current.MainWindow; // рабочий вариант пользования метода Log()
    //public Window2 wnd2 => (Window2)Application.Current.MainWindow;
    private decimal lastPrice;  
    private decimal positions; 
    private decimal _StepLevel = (decimal)0.001; 
    private decimal _Cels = 1; 
    private bool isactiv = false;
    private int _Levels = 5;
    private int _Quantity = 5;
    private Operation operation = Operation.Buy;
 
    //MainWindow wnd = new MainWindow();
    /// <summary>
    ///     Конструктор класса
    /// </summary>
    /// <param name="_quik"></param>
    /// <param name="securityCode">Код инструмента</param> 
    public Tool (Quik quik, string securityCode)
    {
        if (quik == null)
        {
            Console.WriteLine("Конструктор класса Quik = Null");
            return;
        }
        _quik = quik;
        GetBaseParam(quik, securityCode);
    }

    // public void Log(string s) // рабочие вариант пользования метода Log()
    // {
    //     Application.Current.Dispatcher.Invoke(new Action(() => { wnd.Log(s);}));
    //     //wnd.Log(s);
    // }
    //public void Log(string s) => wnd.Log(s);

    private void GetBaseParam(Quik quik, string secCode)
    {
        try
        {
            SecurityCode = secCode;
            ClassCode = quik.Class.GetSecurityClass("SPBFUT,TQBR,TQBS,TQNL,TQLV,TQNE,TQOB,SPBXM,QJSIM", secCode).Result;

            var codes = quik.Class.GetClientCodes().Result;
            if (codes.Count == 1)
                СlientCode = codes[0]; // для демо
            else
                СlientCode = codes[1]; // для боевого

            if (quik != null)
            {
                if (ClassCode != null && ClassCode != "")
                {
                    Name = quik.Class.GetSecurityInfo(ClassCode, SecurityCode).Result.ShortName;
                    AccountID = quik.Class.GetTradeAccount(ClassCode).Result;
                    FirmID = quik.Class.GetClassInfo(ClassCode).Result.FirmId;
                    Step = Convert.ToDecimal(quik.Trading.GetParamEx(ClassCode, SecurityCode, ParamNames.SEC_PRICE_STEP).Result.ParamValue.Replace('.', separator));
                    PriceAccuracy = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(ClassCode, SecurityCode, ParamNames.SEC_SCALE).Result.ParamValue.Replace('.', separator)));

                    if (ClassCode == "SPBFUT")
                    {
                        Console.WriteLine("Получаем 'guaranteeProviding'.");
                        Lot = 1;
                        GuaranteeProviding = Convert.ToDouble(quik.Trading.GetParamEx(ClassCode, SecurityCode, ParamNames.BUYDEPO).Result.ParamValue.Replace('.', separator));
                    }
                    else
                    {
                        Console.WriteLine("Получаем 'lot'.");
                        Lot = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(ClassCode, SecurityCode, ParamNames.LOTSIZE).Result.ParamValue.Replace('.', separator)));
                        GuaranteeProviding = 0;
                    }
                    GetDepoLimit();
                    GetLastPrice();
                }
                else
                {
                    Console.WriteLine("Tool.GetBaseParam. Ошибка: classCode не определен.");
                    Lot = 0;
                    GuaranteeProviding = 0;
                    //MessageBox.Show("НЕТ ПОТОКА от провайдера");
                }
            }
            else
            {
                Console.WriteLine("Tool.GetBaseParam. quik = null !");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка в методе GetBaseParam: " + e.Message); 
        }

        quik.Candles.Subscribe(ClassCode, secCode, CandleInterval.M1).Wait();
        if (quik.Candles.IsSubscribed(ClassCode, secCode, CandleInterval.M1).Result)
        {
            Debug.WriteLine("Подписались на 1 минуту " + secCode + " ...");
            quik.Candles.NewCandle += CandlesOnNewCandle;
        }

        Console.WriteLine("Подписываемся на стакан...");
        quik.OrderBook.Subscribe(ClassCode, SecurityCode).Wait();
        if (quik.OrderBook.IsSubscribed(ClassCode, SecurityCode).Result)
        {
            // var toolOrderBook = new OrderBook();
            // Console.WriteLine("Подписка на стакан прошла успешно.");
            // _quik.Events.OnQuote += Events_OnQuote; ;
            // Console.WriteLine("Подписываемся на колбэк 'OnQuote'...");
            // Console.WriteLine(SecurityCode + " Все ОК");
        }
        else Console.WriteLine(SecurityCode + " Все ПЛОХО");

        _quik.Events.OnOrder += Events_OnOrder;
        _quik.Events.OnStopOrder += Events_OnStopOrder;
        _quik.Events.OnTransReply += Events_OnTransReply;
        _quik.Events.OnParam += Events_OnParam;
        _quik.Events.OnDepoLimit += Events_OnDepoLimit; 
    }
      
    private void GetDepoLimit()
    {
        Positions = Convert.ToDecimal(_quik.Trading.GetDepo(СlientCode, this.FirmID,
            this.SecurityCode, this.AccountID).Result.DepoCurrentBalance / this.Lot); 
    }

    private void GetLastPrice()
    { 
            LastPrice = Convert.ToDecimal(_quik.Trading.GetParamEx(ClassCode, SecurityCode, "LAST").Result.ParamValue
                .Replace('.', separator)); 
    }
    private void Events_OnDepoLimit(DepoLimitEx dLimit)
    {
        if (dLimit.SecCode == SecurityCode)
        {
            GetDepoLimit();
        }
    }

    private void Events_OnParam(Param par)
    {
        if (par.SecCode == SecurityCode)
        {
            GetLastPrice();
            if (Isactiv)
            {

            }
        }  
    }

    private void Events_OnTransReply(TransactionReply transReply)
    {//https://youtu.be/vVehZG3trQ4?si=axTF5vwzTvpA4MMA
        if (transReply.SecCode == SecurityCode)
        {
            if (transReply.Status == 0) Console.WriteLine("Status " + transReply.Status + " Транзакция отправлена серверу");
            if (transReply.Status == 1) Console.WriteLine("Status " + transReply.Status + " Транзакция получена на сервер");
            if (transReply.Status == 2) Console.WriteLine("Status " + transReply.Status + " Ошибка при передаче Транзакции");
            if (transReply.Status == 3)
            {
                Console.WriteLine(" Reply ордер № " + transReply.OrderNum + "  TransID - " + transReply.TransID + " Цена: " + transReply.Price + " Объём: " + transReply.Quantity);
            }
            if (transReply.Status > 3)
            {
                Console.WriteLine("ОШИБКА " + transReply.TransID + " - " + transReply.ResultMsg);
            }
        }
    }

    private void Events_OnStopOrder(StopOrder stopOrder)
    {
        if (stopOrder.SecCode == SecurityCode)
        {
            Console.WriteLine("Стоп-Ордер № - " + stopOrder.OrderNum + ", TransID - " + stopOrder.TransId + ",  SecCode - " + stopOrder.SecCode + " - " + stopOrder.Operation + ", State - " + stopOrder.State);
        }
    }

    private void Events_OnOrder(Order order)
    {
        if (order.SecCode == SecurityCode)
            Console.WriteLine("Оrder № - " + order.OrderNum + ", TransID - " + order.TransID + ",  SecCode - " + order.SecCode + " - " + order.Operation + ", State - " + order.State);
    }

    private void Events_OnQuote(OrderBook orderbook)
    { 
        // if (orderbook.sec_code == SecurityCode)
        // {
        //     var bestBuy = orderbook.bid[orderbook.bid.Length - 1];
        //     var bestSell = orderbook.offer[0];
        //     Console.WriteLine(orderbook.sec_code + ":  bestBuy - " + bestBuy.price + " = " + bestBuy.quantity + " bestSell - " + bestSell.price + " = " + bestSell.quantity);
        //     wnd.Log(orderbook.sec_code + ":  bestBuy - " + bestBuy.price + " = " + bestBuy.quantity + " bestSell - " + bestSell.price + " = " + bestSell.quantity+ ",  this.LastPrice = " +this.LastPrice.ToString());
        //
        // } 
    }

    async Task TakeProfit_StopLoss(Tool tool, decimal price, Operation Bue_Sell, StopOrderType sot)
    {
    https://youtu.be/HWpMYBZCUU4?si=6-wo40ymV2TQ1jF9
        try
        {
            decimal pr1, pr2, pr3; Condition UppLou;
            if (Bue_Sell == Operation.Buy)  //если купить то по наименьшей цэне
            {
                UppLou = Condition.LessOrEqual;
                pr1 = (tool.LastPrice - (tool.LastPrice * (decimal)0.01));// для ТейкПрофит
                var pr11 = (pr1 % tool.Step);
                if (pr11 != 0) pr1 = pr1 - pr11;

                pr2 = (tool.LastPrice + (tool.LastPrice * (decimal)0.001));  //для СтопЛос стоп цена, остановить убыток для шора
                var pr22 = (pr2 % tool.Step);
                if (pr22 != 0) pr2 = pr2 - pr22;

                pr3 = pr2 + (pr2 * (decimal)0.0002);      // для СтопЛос цена покупки, остановить убыток для шора
                var pr33 = (pr3 % tool.Step);
                if (pr33 != 0) pr3 = pr3 - pr33;
            }
            else                            //если продать то по наибольшей цэне
            {
                UppLou = Condition.MoreOrEqual;
                pr1 = (tool.LastPrice + (tool.LastPrice * (decimal)0.01));// для ТейкПрофит
                var pr11 = (pr1 % tool.Step);
                if (pr11 != 0) pr1 = pr1 - pr11;

                pr2 = (tool.LastPrice - (tool.LastPrice * (decimal)0.001));  //для СтопЛос стоп цена, остановить убыток для шора
                var pr22 = (pr2 % tool.Step);
                if (pr22 != 0) pr2 = pr2 - pr22;

                pr3 = pr2 - (pr2 * (decimal)0.0002);      // для СтопЛос цена покупки, остановить убыток для шора
                var pr33 = (pr3 % tool.Step);
                if (pr33 != 0) pr3 = pr3 - pr33;
            }

            StopOrder stopOrder = new StopOrder()
            {
                ClientCode = tool.СlientCode,
                Account = tool.AccountID,
                ClassCode = tool.ClassCode,
                SecCode = tool.SecurityCode,
                Offset = (decimal)0.01,//Math.Round(5 * tool.Step, tool.PriceAccuracy),
                OffsetUnit = OffsetUnits.PERCENTS,
                Spread = (decimal)0.01,//Math.Round(1 * tool.Step, tool.PriceAccuracy),
                SpreadUnit = OffsetUnits.PERCENTS,
                StopOrderType = sot,
                Condition = UppLou,
                ConditionPrice = Math.Round(pr1, tool.PriceAccuracy),
                ConditionPrice2 = Math.Round(pr2, tool.PriceAccuracy), //не нужна для тей-профит
                Price = Math.Round(pr3, tool.PriceAccuracy),  //не нужна для тей-профит
                Operation = Bue_Sell,
                Quantity = 1,
            };

            await _quik.StopOrders.CreateStopOrder(stopOrder).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            MessageBox.Show(" LOAD - " + exception.Message); ;
        }
    }

    private void CandlesOnNewCandle(Candle candle)
    {
        //Console.WriteLine(candle.SecCode + "  "+candle.ToString());
    }

    #region Свойства

    /// <summary>
    ///     Краткое наименование инструмента (бумаги)
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    ///     Цена последней сделки
    /// </summary>
    public decimal LastPrice
    {
        get => lastPrice;
        set => SetField(ref lastPrice, value); 
    }

    /// <summary>
    ///     Позиция
    /// </summary>
    public decimal Positions //{ get; private set; }
    {
        get => positions;
        set => SetField(ref positions, value); 
    }
    /// <summary>
    ///     Buy / Sel
    /// </summary>
    public Operation Operation // { get; set; } = Operation.Buy;
    {
        get => operation;
        set => SetField(ref operation, value); 
    }
     
    /// <summary>
    ///     Статус активности
    /// </summary>
    public bool Isactiv
    {
        get => isactiv;
        set => SetField(ref isactiv, value); 
    }

    /// <summary>
    /// Количество уровней
    /// </summary>
    public int Levels
    {
        get => _Levels;
        set => SetField(ref _Levels, value);
    }

    /// <summary>
    /// Шаг сетки
    /// </summary>
    public decimal StepLevel
    {
        get => _StepLevel;
        set => SetField(ref _StepLevel, value);
    }

    /// <summary>
    /// Цель %
    /// </summary>
    public decimal Cels
    {
        get => _Cels;
        set => SetField(ref _Cels, value);
    }

    /// <summary>
    /// Количество лотов
    /// </summary>
    public int Quantity
    {
        get => _Quantity;
        set => SetField(ref _Quantity, value);
    }

    /// <summary>
    ///     Код инструмента (бумаги)
    /// </summary>
    public string SecurityCode { get; private set; }

    /// <summary>
    ///     Код класса инструмента (бумаги)
    /// </summary>
    public string ClassCode { get; private set; }

    /// <summary>
    ///     Код клиента
    /// </summary>
    public string СlientCode { get; private set; }

    /// <summary>
    ///     Счет клиента
    /// </summary>
    public string AccountID { get; private set; }

    /// <summary>
    ///     Код фирмы
    /// </summary>
    public string FirmID { get; private set; }

    /// <summary>
    ///     Количество акций в одном лоте
    ///     Для инструментов класса SPBFUT = 1
    /// </summary>
    public int Lot { get; private set; }

    /// <summary>
    ///     Точность цены (количество знаков после запятой)
    /// </summary>
    public int PriceAccuracy { get; private set; }

    /// <summary>
    ///     Шаг цены
    /// </summary>
    public decimal Step { get; private set; }

    /// <summary>
    ///     Гарантийное обеспечение (только для срочного рынка)
    ///     для фондовой секции = 0
    /// </summary>
    public double GuaranteeProviding { get; private set; }

    #endregion
} 


