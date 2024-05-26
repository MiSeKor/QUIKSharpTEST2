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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Runtime.ConstrainedExecution;

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
    private decimal _Cels = (decimal)0.01; 
    private bool _isactiv = false;
    private int _Levels = 5;
    private int _Quantity = 5;
    private decimal StopLoss = 0;
    private bool StopPriceFlag = true;
    private ObservableCollection<StopOrder> _ListStopOrder = []; // способ инициализации предложен Визуал Студией
    private Operation _operation = Operation.Buy; 
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
                        Cels = 30;
                        StepLevel = 15;
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
                    MessageBox.Show("НЕТ ТАКОГО ИНСТРУМЕНТА " + SecurityCode); return;
                    Console.WriteLine("Tool.GetBaseParam. Ошибка: classCode не определен.");
                    Lot = 0;
                    GuaranteeProviding = 0; 
                }
            }
            else
            {
                Console.WriteLine("Tool.GetBaseParam. quik = null !");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка в методе GetBaseParam: " + e.Message) ; 
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
        _quik.Events.OnFuturesClientHolding +=EventsOnOnFuturesClientHolding;
    }
 
    private void Log(string s)
    {
        Console.WriteLine(s);
    } 
    private void GetDepoLimit()
    {
        //if (ClassCode == "SPBFUT")
        //{
        //    Positions = Convert.ToDecimal(_quik.Trading.GetFuturesHolding(FirmID, AccountID,
        //        this.SecurityCode, 0).Result.totalNet); // проверить работу этого кода в боевом КВИКЕ
        //}
        //else
        //{}

        //Positions = Convert.ToDecimal(_quik.Trading.GetDepo(СlientCode, this.FirmID, // <<== ЭТОТ код показывает только Т0
        //        this.SecurityCode, this.AccountID).Result.DepoCurrentBalance / this.Lot);

        if (ClassCode == "QJSIM")
        {
            Positions = Convert.ToDecimal(_quik.Trading.GetDepo(СlientCode, this.FirmID, // <<== ЭТОТ код только Т0
                   this.SecurityCode, this.AccountID).Result.DepoCurrentBalance / this.Lot);
        }
        

        if (ClassCode == "TQBR")
        {
            try
            {
                var T = _quik.Trading.GetDepoEx(FirmID, СlientCode,
                    SecurityCode, // <<== ЭТОТ код на боевом КВИКЕ РАБОТАЕТ и показывает Т1
                    AccountID, 1).Result;

                Positions = T != null ? Positions = Convert.ToDecimal(T.CurrentBalance / this.Lot) : Positions = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        } 
    }

    private void GetLastPrice()
    { 
            LastPrice = Convert.ToDecimal(_quik.Trading.GetParamEx(ClassCode, SecurityCode, "LAST").Result.ParamValue
                .Replace('.', separator)); 
    }

    private void EventsOnOnFuturesClientHolding(FuturesClientHolding futpos)
    {
        if (futpos.secCode == SecurityCode)
        {
            GetDepoLimit();
        }
    }
    private void Events_OnDepoLimit(DepoLimitEx dLimit)
    {
        if (dLimit.SecCode == SecurityCode)
        {
            GetDepoLimit();
        }
    }
    /// <summary>
    ///     Расчет отступа размером в _otstup от указанной цены
    /// </summary>
    decimal CalclOtstup(decimal price, decimal _otstup)
    {
        var otstup = price * _otstup;
        otstup = ((otstup % this.Step) != 0) ? otstup - (otstup % this.Step) : otstup;
        return otstup;
    }
    private void Events_OnParam(Param par)
    {
        decimal otstup;
        if (par.SecCode == SecurityCode) GetLastPrice();
         
        //if (!Isactiv && ListStopOrder.Count > 0 && par.SecCode == SecurityCode)
        //{
        //    KillAllOrders();
        //    ListStopOrder.Clear();
        //    StopLoss = 0;
        //    Log("СРАБОТАЛА ДЕАКТИВАЦИЯ " + this.SecurityCode);
        //}
        if (Isactiv && par.SecCode == SecurityCode)
        {
            if (this.ListStopOrder.Count == 1)
                StopLoss = this.ListStopOrder[0].ConditionPrice -
                            CalclOtstup(this.ListStopOrder[0].ConditionPrice, this.StepLevel); // обозначает передел убытка

            if (this.ListStopOrder.Count == 0 && StopLoss == 0 ||
                this.ListStopOrder.Count == 0 &&
                StopLoss < CalclOtstup(StopLoss, this.StepLevel) * this.StepLevel + StopLoss) 
            {
                SetUpNetwork();
                Log("СРАБОТАЛ SetUpNetwork " + this.SecurityCode);
            }
            //добавление СтопОрдеров
            if (this.ListStopOrder.Count < Levels && LastPrice > ListStopOrder[0].ConditionPrice
                + CalclOtstup(ListStopOrder[0].ConditionPrice, this.Cels) + Step * 2)
            {
                otstup = ClassCode == "SPBFUT" ? StepLevel : CalclOtstup(this.ListStopOrder[0].ConditionPrice, this.StepLevel); 
                this.ListStopOrder.Add(CreateStopOrder(this.ListStopOrder[0].ConditionPrice + otstup, Operation.Buy).Result);
                this.ListStopOrder.Move(this.ListStopOrder.Count-1,0);
                Log("ДОБАВЛЕН СТОП ОРДЕР НА ПОКУПКУ по цене:" + (this.ListStopOrder[0].ConditionPrice + otstup).ToString()+" " + this.SecurityCode);
            }
            // СТОП УБЫТКА = StopLoss
            if (this.lastPrice < StopLoss)
            { 
                this.KillAllOrders();
                //this.Closeallpositions();
                this.Isactiv = false;
                Log("СРАБОТАЛ StopLoss");
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
            if (transReply.Status == 3) // это условие срабатывает когда все ТИП ТОП
            {
                //Console.WriteLine(" Reply ордер № " + transReply.OrderNum + "  TransID - " + transReply.TransID + " Цена: " + transReply.Price + " Объём: " + transReply.Quantity);
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
            Console.WriteLine("Стоп-Ордер № - " + stopOrder.OrderNum + ", TransID - " + stopOrder.TransId + ",  SecCode - " + stopOrder.SecCode + " - " + stopOrder.Operation + ", State - " + stopOrder.State + ", Comment - " + stopOrder.Comment);

        }
    }

    private void Events_OnOrder(Order order)
    {
        decimal cel;

        if (order.SecCode == SecurityCode)
        { 
            foreach (var spOrder in this.ListStopOrder)
            {
                if (order.TransID == spOrder.TransId && order.State == State.Completed)
                {
                    this.ListStopOrder.Remove(spOrder);
                    if (ClassCode == "SPBFUT")
                    {
                        cel = this.Cels;
                    }
                    else
                    {
                        cel = spOrder.ConditionPrice * this.Cels;
                        cel = ((cel % this.Step) != 0) ? cel - (cel % this.Step) : cel;
                    } 
                    //Operation op = spOrder.Operation == Operation.Buy? op = Operation.Sell : op = Operation.Buy;
                    CreateStopOrder(order.Price + cel, Operation.Sell);
                    Log("ВЫСТАВЛЕН ОРДЕР НА ПРОДАЖУ по цене: "+ order.Price + cel+" "+ SecurityCode);
                }
            } 
            //Console.WriteLine("Оrder № - " + order.OrderNum + ", TransID - " + order.TransID + ",  SecCode - " + order.SecCode + " - " + order.Operation + ", State - " + order.State);
        }
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
    private async Task<ObservableCollection<StopOrder>> SetUpNetwork()
    {
        //this.ListTransID = new List<long>();
        bool flag = true;
        decimal otstup = 0;
        decimal pr = this.lastPrice;
        for (int n = 0; n < this.Levels; n++)
        {
            //Application.Current.Dispatcher.Invoke(new Action(() => { wnd.Log(s);}));
            // await Task.Run(() => { тут метод/задачка   });
            // await Task.Delay(500);
            //wnd.Dispatcher.Invoke(new Action(() => {}));
            //await Task.Run(() => { });  
            ListStopOrder.Add(CreateStopOrder(pr,Operation.Buy).Result);
            //CreateStopOrder(pr, Operation.Buy);
            //     await Task.Run(() => {  });
            if (flag)
            {
                if (ClassCode == "SPBFUT")
                {
                    otstup = StepLevel;
                }
                else
                {
                    otstup = pr * this.StepLevel;
                    otstup = ((otstup % this.Step) != 0) ? otstup - (otstup % this.Step) : otstup;
                    flag = false;
                }
;
            }

            pr -= otstup;   // тоже самое что и: pr = pr - otstup; 
        }

        return ListStopOrder;
    }
    private async Task<StopOrder> CreateStopOrder(decimal pr, Operation BuySel)
    { 
            StopOrder stopOrder = new()
            {
                ClientCode = this.СlientCode,
                Account = this.AccountID,
                ClassCode = this.ClassCode,
                SecCode = this.SecurityCode,
                Offset = Convert.ToDecimal(((this.Step*3).ToString()).TrimEnd('0')),//Convert.ToDecimal(this.Step.ToString("N", CultureInfo.GetCultureInfo("ru-RU"))), писец пилорама, зато работает!
                OffsetUnit = OffsetUnits.PRICE_UNITS,
                Spread = Convert.ToDecimal(((this.Step).ToString()).TrimEnd('0')),//Convert.ToDecimal(this.Step.ToString("N", CultureInfo.GetCultureInfo("ru-RU"))),
                SpreadUnit = OffsetUnits.PRICE_UNITS,
                StopOrderType = StopOrderType.TakeProfit,
                Condition = BuySel == Operation.Buy ? Condition.LessOrEqual : Condition.MoreOrEqual,
                ConditionPrice = Math.Round(pr, this.PriceAccuracy),
                // ConditionPrice2 = Math.Round(pr2, this.PriceAccuracy), //не нужна для тей-профит
                // Price = Math.Round(pr3, this.PriceAccuracy),  //не нужна для тей-профит
                Operation = BuySel,
                Quantity = this.Quantity,
                Comment = "qwerty",//BuySel == Operation.Buy ? (_Comment = "Buy") : (_Comment = "Sel"),// похоже коммент совсем бесполезный, нигде в ордерах его нет
            };

            var t = await _quik.StopOrders.CreateStopOrder(stopOrder).ConfigureAwait(false);
            stopOrder.TransId = t;
            return stopOrder; 
    }
    public async Task Closeallpositions()
    {
        decimal KolLot;

        if (ClassCode == "SPBFUT")
        {
            var FiuchersPos = _quik.Trading.GetFuturesHolding(FirmID, AccountID, SecurityCode, 0).Result.totalNet;
            KolLot = (decimal)FiuchersPos;
        }
        else
        {
            KolLot = Positions;
        } 
            
             
        if (KolLot != 0)
        {
            if (KolLot != null && KolLot > 0)
            {
                await _quik.Orders.SendMarketOrder(this.ClassCode, this.SecurityCode, this.AccountID, Operation.Sell, (int)KolLot).ConfigureAwait(false);
            }
            else if (KolLot < 0)
            {
                await _quik.Orders.SendMarketOrder(this.ClassCode, this.SecurityCode, this.AccountID, Operation.Buy, (int)-KolLot).ConfigureAwait(false);
            }
            Console.WriteLine(SecurityCode + " Closeallpositions");
        }  
    }
    public async Task KillAllOrders()
    {
        this.ListStopOrder.Clear();
        this.Isactiv = false;

        var orders = _quik.Orders.GetOrders(this.ClassCode, this.SecurityCode).Result;
        if (orders.Count != 0)
        {
            foreach (var order in orders)
            {
                if (order.State == State.Active)
                {
                    await _quik.Orders.KillOrder(order).ConfigureAwait(true);
                }
            }
        }

        var Stoporders = _quik.StopOrders.GetStopOrders(this.ClassCode, this.SecurityCode).Result;
        if (Stoporders.Count != 0)
        {
            foreach (var stoporder in Stoporders)
            {
                if (stoporder.State == State.Active && stoporder.Operation == this.operation)
                {
                    await _quik.StopOrders.KillStopOrder(stoporder).ConfigureAwait(false);
                }
            }
        } 
        Console.WriteLine(SecurityCode + " Kill All Orders"); 
    }
    private void CandlesOnNewCandle(Candle candle)
    {
        //Console.WriteLine(candle.SecCode + "  "+candle.ToString());
    }

    #region Свойства

    /// <summary>
    ///     Краткое наименование инструмента (бумаги)
    /// </summary>
    public string Name { get; set; }

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
    public Operation operation // { get; set; } = Operation.Buy;
    {
        get => _operation;
        set => SetField(ref _operation, value); 
    }
     
    /// <summary>
    ///     Статус активности
    /// </summary>
    public bool Isactiv
    {
        get => _isactiv;
        set => SetField(ref _isactiv, value); 
    }

    /// <summary>
    /// Количество уровней сетки
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
    ///     Лист Стоп-Ордеров
    /// </summary>
    public ObservableCollection<StopOrder> ListStopOrder
    {
        get => _ListStopOrder;
        set => SetField(ref _ListStopOrder, value);
    }

    /// <summary>
    ///     Код инструмента (бумаги)
    /// </summary>
    public string SecurityCode { get; set; }

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


