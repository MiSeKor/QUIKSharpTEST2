﻿using QuikSharp;
using QuikSharp.DataStructures;
using QuikSharp.DataStructures.Transaction;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Condition = QuikSharp.DataStructures.Condition;


namespace QUIKSharpTEST2
{
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
        private decimal _StepLevel = 0.001M; 
        private decimal _Cels = 0.01M; 
        private bool _isactiv = false;
        private int _Levels = 5;
        private int _Quantity = 5;
        private decimal StopLoss = decimal.Zero;
        private bool StrategyFlag = false;
        private Operation _operation = Operation.Buy;
        private Strategy _strategys = Strategy.Default;
        private DispatcherTimer timer = new();
        private string IdSK;
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
        public void Unsubscribe()
        {
            if (!Isactiv)
            {
                _quik.OrderBook.Unsubscribe(ClassCode, SecurityCode).Wait();
                _quik.Events.OnQuote -= Events_OnQuote;
                _quik.Events.OnOrder -= Events_OnOrder;
                _quik.Events.OnStopOrder -= Events_OnStopOrder;
                _quik.Events.OnDepoLimit -= Events_OnDepoLimit;
                _quik.Events.OnFuturesClientHolding -= EventsOnOnFuturesClientHolding;
                if (_quik.Candles.IsSubscribed(ClassCode, SecurityCode, CandleInterval.M5).Result)
                {
                    _quik.Candles.Unsubscribe(ClassCode, SecurityCode, CandleInterval.M5).Wait();
                }
                _quik.Candles.NewCandle -= Events_NewCandle_M5_StrategyIntersMA; 
            }  
        }

        private void GetBaseParam(Quik quik, string secCode)
        {
            try
            {
                IdSK = secCode;
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
                        //MessageBox.Show("НЕТ ТАКОГО ИНСТРУМЕНТА " + SecurityCode); return;

                        Console.WriteLine("Tool.GetBaseParam. Ошибка: classCode не определен.");
                        Lot = 0;
                        GuaranteeProviding = 0;
                        this.LastPrice = -999999;
                        return;
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

            //quik.Candles.Subscribe(ClassCode, secCode, CandleInterval.M1).Wait();
            //if (quik.Candles.IsSubscribed(ClassCode, secCode, CandleInterval.M1).Result)
            //{
            //    Debug.WriteLine("Подписались на 1 минуту " + secCode + " ...");
            //    quik.Candles.NewCandle += CandlesOnNewCandle;
            //}

            //Console.WriteLine("Подписываемся на стакан...");
            quik.OrderBook.Subscribe(ClassCode, SecurityCode).Wait();
            if (quik.OrderBook.IsSubscribed(ClassCode, SecurityCode).Result)
            {
                // var toolOrderBook = new OrderBook();
                // Console.WriteLine("Подписка на стакан прошла успешно.");
                _quik.Events.OnQuote += Events_OnQuote; // пользовать для обновления LastPrice
                // Console.WriteLine("Подписываемся на колбэк 'OnQuote'...");
                // Console.WriteLine(SecurityCode + " Все ОК");
            }
            else Console.WriteLine(SecurityCode + " Все ПЛОХО");

            _quik.Events.OnOrder += Events_OnOrder;
            _quik.Events.OnStopOrder += Events_OnStopOrder;
            //_quik.Events.OnTransReply += Events_OnTransReply;
            //_quik.Events.OnParam += Events_OnParam_Strategy_IntersMA;
            _quik.Events.OnDepoLimit += Events_OnDepoLimit;
            _quik.Events.OnFuturesClientHolding +=EventsOnOnFuturesClientHolding; 
            SwitchStrategys(Strategys);
        }

        public void Log(string s)
        {
            Console.WriteLine(s);
        } 
        private void GetDepoLimit()
        {
            if (ClassCode == "SPBFUT")
            {
                //var T = _quik.Trading.GetFuturesHolding(FirmID, AccountID,
                //    this.SecurityCode, 0).Result;// проверить работу этого кода в боевом КВИКЕ
                //Positions = T != null ? Positions = Convert.ToDecimal(T.totalNet / this.Lot) : Positions = 0;

                Positions = Convert.ToDecimal(_quik.Trading.GetFuturesHolding(FirmID, AccountID,
                    this.SecurityCode, 1).Result?.totalNet); // проверка оператора "?"
            } 

            //Positions = Convert.ToDecimal(_quik.Trading.GetDepo(СlientCode, this.FirmID, // <<== ЭТОТ код показывает только Т0
            //        this.SecurityCode, this.AccountID).Result.DepoCurrentBalance / this.Lot);

            if (ClassCode == "QJSIM")
            {
                Positions = Convert.ToDecimal(_quik.Trading.GetDepo(СlientCode, this.FirmID, // <<== ЭТОТ код только Т0
                       this.SecurityCode, this.AccountID).Result?.DepoCurrentBalance / this.Lot);
            }
            

            if (ClassCode == "TQBR")
            {
                try
                {
                    Positions = Convert.ToDecimal(_quik.Trading.GetDepoEx(FirmID, СlientCode,
                        SecurityCode, // <<== ЭТОТ код на боевом КВИКЕ РАБОТАЕТ и показывает Т1
                        AccountID, 1).Result?.CurrentBalance / this.Lot);// проверка оператора "?"

                    //var T = _quik.Trading.GetDepoEx(FirmID, СlientCode,
                    //    SecurityCode, // <<== ЭТОТ код на боевом КВИКЕ РАБОТАЕТ и показывает Т1
                    //    AccountID, 1).Result;

                    //Positions = T != null ? Positions = Convert.ToDecimal(T.CurrentBalance / this.Lot) : Positions = 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            } 
        }

        private void Timer_Tick(object sender, EventArgs e)
        { 
            
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
        ///     Расчет отступа размером = _otstup в % от указаннго price
        /// </summary>
        private decimal CalclOtstup(decimal price, decimal _otstup)
        {
            var otstup = price * _otstup;
            otstup = ((otstup % this.Step) != 0) ? otstup - (otstup % this.Step) : otstup;
            return otstup;
        }

        public bool Сheck_Isactiv(bool val)
        {
            if (!val)
            {
                KillOperationOrders();
                ListStopOrder.Clear();
                StrategyFlag = true;
                StopLoss = decimal.Zero;
                Log("ОТКЛЮЧЕНА АВТОМАТИКА " + this.SecurityCode);
            }
            else
            {
                Log("ВКЛЮЧЕНА АВТОМАТИКА " + this.SecurityCode);
            }
            return val;
        }
        public bool SwitchStrategys(Strategy val)
        { 
            switch (val)
            {
                case Strategy.Default:
                    _quik.Events.OnParam += Events_OnParam_Strategy_Default;
                    _quik.Events.OnParam -= Events_OnParam1_Strategy_ToTrend;
                    _quik.Events.OnParam -= Events_OnParam_Strategy_MoveNet;
                    _quik.Candles.NewCandle -= Events_NewCandle_M5_StrategyIntersMA;
                    break;
                case Strategy.ToTrend:
                    _quik.Events.OnParam -= Events_OnParam_Strategy_Default;
                    _quik.Events.OnParam += Events_OnParam1_Strategy_ToTrend;
                    _quik.Events.OnParam -= Events_OnParam_Strategy_MoveNet;
                    _quik.Candles.NewCandle -= Events_NewCandle_M5_StrategyIntersMA;
                    break;
                case Strategy.MoveNet:
                    _quik.Events.OnParam -= Events_OnParam_Strategy_Default;
                    _quik.Events.OnParam -= Events_OnParam1_Strategy_ToTrend;
                    _quik.Events.OnParam += Events_OnParam_Strategy_MoveNet;
                    _quik.Candles.NewCandle -= Events_NewCandle_M5_StrategyIntersMA;
                    break;
                case Strategy.IntersMA:
                    _quik.Events.OnParam -= Events_OnParam_Strategy_Default;
                    _quik.Events.OnParam -= Events_OnParam1_Strategy_ToTrend;
                    _quik.Events.OnParam -= Events_OnParam_Strategy_MoveNet;
                    if (_quik.Candles.IsSubscribed(ClassCode, SecurityCode, CandleInterval.M1).Result)
                    {
                        _quik.Candles.Unsubscribe(ClassCode, SecurityCode, CandleInterval.M1).Wait();
                        Log("отписка М1");
                    }
                    if (!_quik.Candles.IsSubscribed(ClassCode, SecurityCode, CandleInterval.M5).Result)
                    {
                        Log("нет подписки М5");
                        _quik.Candles.Subscribe(ClassCode, SecurityCode, CandleInterval.M5).Wait();
                        if (_quik.Candles.IsSubscribed(ClassCode, SecurityCode, CandleInterval.M5).Result)
                            Log("подписка М5");
                    } 
                    _quik.Candles.NewCandle += Events_NewCandle_M5_StrategyIntersMA;
                    break; 
            }

            return true;
        }
         
        private void Events_OnParam_Strategy_MoveNet(Param par)
        { 
            int i = 3; 

            if (par.SecCode == SecurityCode && Isactiv)
            {
                if (operation == Operation.Buy)
                {
                    if (StopLoss == decimal.Zero ||
                        this.ListStopOrder.Count == 0 &&
                        this.LastPrice > StopLoss + CalclOtstup(StopLoss, this.StepLevel) * i ||
                        this.ListStopOrder.Count > 0 &&
                        this.LastPrice > this.ListStopOrder[0].ConditionPrice
                        + CalclOtstup(this.ListStopOrder[0].ConditionPrice, this.StepLevel) * i)
                    {
                        SetUpNetwork();
                        Log("СРАБОТАЛ SetUpNetwork " + this.SecurityCode);
                    }
                    // СТОП УБЫТКА = StopLoss
                    if (this.lastPrice < StopLoss && this.ListStopOrder.Count == 0 && StopLoss != decimal.Zero)
                    {
                        this.KillOperationOrders();
                        //this.CloseAllpositions();
                        StopLoss = decimal.Zero;
                        this.Isactiv = false;
                        Log("СРАБОТАЛ StopLoss");
                    }
                }

                if (operation == Operation.Sell)
                {
                    if (StopLoss == decimal.Zero ||
                        this.ListStopOrder.Count == 0 &&
                        this.LastPrice < StopLoss - CalclOtstup(StopLoss, this.StepLevel) * i ||
                        this.ListStopOrder.Count > 0 &&
                        this.LastPrice < this.ListStopOrder[0].ConditionPrice
                        - CalclOtstup(this.ListStopOrder[0].ConditionPrice, this.StepLevel) * i)
                    {
                        SetUpNetwork();
                        Log("СРАБОТАЛ SetUpNetwork " + this.SecurityCode);
                    }
                    // СТОП УБЫТКА = StopLoss
                    if (this.lastPrice > StopLoss && this.ListStopOrder.Count == 0 && StopLoss != decimal.Zero)
                    {
                        this.KillOperationOrders();
                        //this.CloseAllpositions();
                        StopLoss = decimal.Zero;
                        this.Isactiv = false;
                        Log("СРАБОТАЛ StopLoss");
                    }

                }
            }
        }

        private void Events_OnParam1_Strategy_ToTrend(Param par)
        {
            if (par.SecCode != SecurityCode) return; 

        }

        private void Events_OnParam_Strategy_Default(Param par)
        {
            
            decimal otstup;

            if (par.SecCode == SecurityCode && Isactiv)
            {
                if (operation == Operation.Buy)
                {
                    if (StopLoss == decimal.Zero ||
                        this.ListStopOrder.Count == 0 &&
                        this.LastPrice > StopLoss + CalclOtstup(StopLoss, this.StepLevel) * this.Levels)
                    {
                        SetUpNetwork();
                        Log("СРАБОТАЛ SetUpNetwork " + this.SecurityCode);
                    }
                    //добавление СтопОрдеров
                    if (this.ListStopOrder.Count > 0 &&
                        this.ListStopOrder.Count < this.Levels && this.LastPrice > this.ListStopOrder[0].ConditionPrice
                        + CalclOtstup(this.ListStopOrder[0].ConditionPrice, this.StepLevel)
                        + CalclOtstup(this.ListStopOrder[0].ConditionPrice, this.Cels) + Step * 2)
                    {
                        otstup = ClassCode == "SPBFUT" ? StepLevel : CalclOtstup(this.ListStopOrder[0].ConditionPrice, this.StepLevel);
                        this.ListStopOrder.Insert(0, CreateStopOrder(this.ListStopOrder[0].ConditionPrice + otstup, Operation.Buy));
                        Log("ДОБАВЛЕН СТОП ОРДЕР НА " + this.operation + " по цене:" + (this.ListStopOrder[0].ConditionPrice) + " " + this.SecurityCode);
                    }
                    // СТОП УБЫТКА = StopLoss
                    if (this.lastPrice < StopLoss && this.ListStopOrder.Count == 0 && StopLoss != decimal.Zero)
                    {
                        this.KillOperationOrders();
                        //this.CloseAllpositions();
                        StopLoss = decimal.Zero;
                        this.Isactiv = false;
                        Log("СРАБОТАЛ StopLoss");
                    }
                }


                if (operation == Operation.Buy)
                {
                    if (StopLoss == decimal.Zero ||
                        this.ListStopOrder.Count == 0 &&
                        this.LastPrice < StopLoss - CalclOtstup(StopLoss, this.StepLevel) * this.Levels)
                    {
                        SetUpNetwork();
                        Log("СРАБОТАЛ SetUpNetwork " + this.SecurityCode);
                    }
                    //добавление СтопОрдеров
                    if (this.ListStopOrder.Count > 0 &&
                        this.ListStopOrder.Count < Levels && LastPrice < ListStopOrder[0].ConditionPrice
                        - CalclOtstup(ListStopOrder[0].ConditionPrice, this.StepLevel)
                        - CalclOtstup(ListStopOrder[0].ConditionPrice, this.Cels) - Step * 2)
                    {
                        otstup = ClassCode == "SPBFUT" ? StepLevel : CalclOtstup(this.ListStopOrder[0].ConditionPrice, this.StepLevel);
                        this.ListStopOrder.Insert(0, CreateStopOrder(this.ListStopOrder[0].ConditionPrice + otstup, Operation.Buy));
                        Log("ДОБАВЛЕН СТОП ОРДЕР НА " + this.operation + " по цене:" + (this.ListStopOrder[0].ConditionPrice) + " " + this.SecurityCode);
                    }
                    // СТОП УБЫТКА = StopLoss
                    if (this.lastPrice > StopLoss && this.ListStopOrder.Count == 0 && StopLoss != decimal.Zero)
                    {
                        this.KillOperationOrders();
                        //this.CloseAllpositions();
                        StopLoss = decimal.Zero;
                        this.Isactiv = false;
                        Log("СРАБОТАЛ StopLoss");
                    }

                }
            }
        } 

        private void Events_NewCandle_M5_StrategyIntersMA(Candle candle)
        { 
            string FAST = "FAST", SLOW = "SLOW"; int i = 3;
            if (candle.SecCode == SecurityCode && Isactiv)
            {

                var N = _quik.Candles.GetNumCandles(FAST).Result;// количество свечек 
                var LineFAST = _quik.Candles.GetCandles(FAST, 0, N - i, i).Result;
                var LineSLOW = _quik.Candles.GetCandles(SLOW, 0, N - i, i).Result;

                Log("LineFAST " + LineFAST[0].Close + " " + LineFAST[1].Close + " " + LineFAST[2].Close/* + " " + LineFAST[3].Close*/);
                Log("LineSLOW " + LineSLOW[0].Close + " " + LineSLOW[1].Close + " " + LineSLOW[2].Close/* + " " + LineSLOW[3].Close*/);

                if (/*operation == Operation.Buy &&*/
                    LineFAST[2].Close > LineSLOW[2].Close &&
                    LineFAST[1].Close > LineSLOW[1].Close &&
                    LineFAST[0].Close < LineSLOW[0].Close)
                {
                    // ПОКУПКА
                    Log("ПОКУПКА");
                    if (Positions == 0)
                    {
                        _quik.Orders.SendLimitOrder(ClassCode, SecurityCode, AccountID, Operation.Buy, lastPrice + Step * 2, Quantity);
                    }
                    if (Positions < 0) // разворот
                    {
                        _quik.Orders.SendLimitOrder(ClassCode, SecurityCode, AccountID, Operation.Buy, lastPrice + Step * 2, (int)Positions * 2);
                    }
                }
                //if (operation != Operation.Sell) return;
                if (/*operation == Operation.Sell &&*/
                    LineFAST[2].Close < LineSLOW[2].Close &&
                    LineFAST[1].Close < LineSLOW[1].Close &&
                    LineFAST[0].Close > LineSLOW[0].Close)
                {
                    //ПРОДАЖА
                    Log("ПРОДАЖА");
                    if (Positions == 0)
                    {
                        _quik.Orders.SendLimitOrder(ClassCode, SecurityCode, AccountID, Operation.Sell, lastPrice - Step * 2, Quantity);
                    }
                    if (Positions > 0) // разворот
                    {
                        _quik.Orders.SendLimitOrder(ClassCode, SecurityCode, AccountID, Operation.Sell, lastPrice - Step * 2, (int)Positions * 2);
                    }
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

                //var t =transReply.Comment;
                //var tt = transReply.ErrorCode;
                //var ttt = transReply.ResultMsg;
                //var ttttt = transReply.Flags;
                //var tttt = transReply.Flags;

                //foreach (var stpOrder in ListStopOrder.ToList())
                //{
                //    if (transReply.TransID != stpOrder.TransId) continue;
                //    //if (!transReply.TransID.ToString().Contains(stpOrder.Comment)) continue; 
                //    if (transReply.Status <= 3) continue;
                //    ListStopOrder.Remove(stpOrder);
                //    Log(">>> transReply <<< СОВПАДЕНЕ УДАЛЕНЕ " +stpOrder.TransId+" "+
                //        stpOrder.Comment + " " + stpOrder.SecCode);
                //}
            }
        }

        private void Events_OnStopOrder(StopOrder stopOrder)
        { 
            if (stopOrder.SecCode == SecurityCode)
            {
                Log("Стоп-Ордер № - " + stopOrder.OrderNum + ", TransID - " 
                    + stopOrder.TransId + ",  SecCode - " + stopOrder.SecCode + " - " 
                    + stopOrder.Operation + ", State - " + stopOrder.State + ", Comment - " + stopOrder.Comment);

                foreach (var ord in this.ListStopOrder.ToList())// если закрыть вручную в квике надо синхронизировать
                {
                    if (ord.TransId == stopOrder.TransId && ord.State == State.Canceled)
                    {
                        this.ListStopOrder.Remove(ord);
                        Log("Events_OnStopOrder УДАЛЕНИЕ TransID - " + stopOrder.TransId + ", SecCode - " + stopOrder.SecCode);
                    }
                }
            } 
        }

        private void Events_OnOrder(Order order)
        {
            decimal cel;
            decimal price;

            if (order.SecCode == SecurityCode)
            {
                //  https://stackoverflow.com/questions/604831/collection-was-modified-enumeration-operation-may-not-execute?newreg=3a15fe8e1a0940398352d3337acc3062
                //Вызов this.ListStopOrder.ToList() копирует значения this.ListStopOrder в 
                //отдельный список в начале foreach. Больше ничто не имеет доступа к этому 
                //списку(у него даже нет имени переменной!), Поэтому ничто не может 
                //изменить его внутри цикла.
                foreach (var spOrder in this.ListStopOrder.ToList())
                {
                    if (order.TransID == spOrder.TransId && order.State == State.Completed)
                    {
                        this.ListStopOrder.Remove(spOrder);
                        cel = ClassCode == "SPBFUT" ? cel = this.Cels : cel = CalclOtstup(spOrder.ConditionPrice, this.Cels);
                        Operation op = this.operation == Operation.Buy ? op = Operation.Sell : op = Operation.Buy;
                        price = this.operation == Operation.Buy ? order.Price + cel : order.Price - cel;
                        CreateStopOrder(price, op);
                        Log("ВЫСТАВЛЕН ОРДЕР НА "+ op + " по цене: " + price + " " + SecurityCode);
                    }
                }
                ////для опоздавших и не снятых ордеров ордеров, на всякий слуай
                //if (order.TransID != 0 &&
                //    order.Operation == operation &&
                //    order.State == State.Completed &&
                //    Isactiv)
                //{ 
                //    cel = ClassCode == "SPBFUT" ? cel = this.Cels : cel = CalclOtstup(order.Price, this.Cels);
                //    Operation op = this.operation == Operation.Buy ? op = Operation.Sell : op = Operation.Buy;
                //    price = this.operation == Operation.Buy ? order.Price + cel : order.Price - cel;
                //    CreateStopOrder(price, op, order.Quantity) ;
                //    Log("ВЫСТАВЛЕН ОРДЕР >>> не из листа <<< , НА " + op + " по цене: " + price + " " + SecurityCode);
                //}

                //Console.WriteLine("Оrder № - " + order.OrderNum + ", TransID - " + order.TransID + ",  SecCode - " + order.SecCode + " - " + order.Operation + ", State - " + order.State);
            }
        }

        private void Events_OnQuote(OrderBook orderbook)
        {
            if (orderbook.sec_code != SecurityCode) return;
            GetLastPrice(); 
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

            if (Strategys == Strategy.MoveNet && this.ListStopOrder.Count > 0)
            {
                //StrategyFlag = true;
                pr = this.operation == Operation.Buy ?
                    pr -= CalclOtstup(pr, this.StepLevel) :
                    pr += CalclOtstup(pr, this.StepLevel); 

                KillOperationOrders();
                if (ListStopOrder.Count == 0)
                {
                    Log("Чистка от сетки " + this.operation + "- ордеров перед переносом сетки " + this.Name);
                }
                else
                {
                    Log("Ждем завершения расчета MIN/MAX " + this.operation + "- ордера  " + this.Name);
                }
            }

            //foreach (var i in Enumerable.Range(0, this.Levels)){}
            if (ListStopOrder.Count != 0) return ListStopOrder;
            for (int n = 0; n < this.Levels; n++)
            {
                //Application.Current.Dispatcher.Invoke(new Action(() => { wnd.Log(s);}));
                // await Task.Run(() => { тут метод/задачка   });
                // await Task.Delay(500);
                //wnd.Dispatcher.Invoke(new Action(() => {}));
                //await Task.Run(() => { });  
                ListStopOrder.Add(CreateStopOrder(pr, this.operation));
                //CreateStopOrder(pr, Operation.Buy);
                //     await Task.Run(() => {  });
                if (n == 0)
                {
                    if (ClassCode == "SPBFUT")
                    {
                        otstup = StepLevel;
                    }
                    else
                    {
                        otstup = pr * this.StepLevel;
                        otstup = ((otstup % this.Step) != 0) ? otstup - (otstup % this.Step) : otstup;
                        //flag = false;
                    }
                }

                pr = this.operation == Operation.Buy ? pr -= otstup : pr += otstup;
            } 

            var ToBuySel = CalclOtstup(ListStopOrder[ListStopOrder.Count - 1].ConditionPrice, this.StepLevel)+ this.Step;
            this.StopLoss = this.operation == Operation.Buy ?
                ListStopOrder[ListStopOrder.Count - 1].ConditionPrice - ToBuySel :
                ListStopOrder[ListStopOrder.Count - 1].ConditionPrice + ToBuySel;
            return ListStopOrder;
        }
        private StopOrder CreateStopOrder(decimal pr, Operation BuySel)
        { 
                StopOrder stopOrder = new()
                {
                    ClientCode = this.СlientCode,
                    Account = this.AccountID,
                    ClassCode = this.ClassCode,
                    SecCode = this.SecurityCode,
                    Offset = Convert.ToDecimal(((this.Step).ToString()).TrimEnd('0')),//писец пилорама, зато работает!
                    OffsetUnit = OffsetUnits.PRICE_UNITS,
                    Spread = Convert.ToDecimal(((this.Step).ToString()).TrimEnd('0')),
                    SpreadUnit = OffsetUnits.PRICE_UNITS,
                    StopOrderType = StopOrderType.TakeProfit,
                    Condition = BuySel == Operation.Buy ? Condition.LessOrEqual : Condition.MoreOrEqual,
                    ConditionPrice = Math.Round(pr, this.PriceAccuracy),
                     ConditionPrice2 = 0, //не нужна для тей-профит
                    Price = 0,  //не нужна для тей-профит
                    Operation = BuySel,
                    Quantity = this.Quantity,
                    Comment = BuySel == Operation.Buy ? "Buy" : "Sel",
                };

                var t = _quik.StopOrders.CreateStopOrder(stopOrder).Result;
                stopOrder.TransId = t;
                return stopOrder; 
        }
        private StopOrder CreateStopOrder(decimal pr, Operation BuySel, int _Quantity)
        {
            StopOrder stopOrder = new()
            {
                ClientCode = this.СlientCode,
                Account = this.AccountID,
                ClassCode = this.ClassCode,
                SecCode = this.SecurityCode,
                Offset = Convert.ToDecimal(((this.Step).ToString()).TrimEnd('0')),//писец пилорама, зато работает!
                OffsetUnit = OffsetUnits.PRICE_UNITS,
                Spread = Convert.ToDecimal(((this.Step).ToString()).TrimEnd('0')),
                SpreadUnit = OffsetUnits.PRICE_UNITS,
                StopOrderType = StopOrderType.TakeProfit,
                Condition = BuySel == Operation.Buy ? Condition.LessOrEqual : Condition.MoreOrEqual,
                ConditionPrice = Math.Round(pr, this.PriceAccuracy),
                ConditionPrice2 = 0, //не нужна для тей-профит
                Price = 0,  //не нужна для тей-профит
                Operation = BuySel,
                Quantity = _Quantity,
                Comment = BuySel == Operation.Buy ? "Buy" : "Sel",
            };

            var t = _quik.StopOrders.CreateStopOrder(stopOrder).Result;
            stopOrder.TransId = t;
            return stopOrder;
        }
        /// <summary>
        /// Закроет все, Ордеры Buy и Sell и this.Positions 
        /// </summary> 
        public async Task CloseAllpositions()
        { 
            await KillAllOrders().ConfigureAwait(false); 
            if (this.Positions == 0) return;
            int pos = this.Positions > 0 ? pos = (int)this.Positions : pos = (int)-this.Positions;
            Operation Oper = this.Positions > 0 ? Oper = Operation.Sell : Oper = Operation.Buy;
            await _quik.Orders.SendMarketOrder(this.ClassCode, this.SecurityCode, this.AccountID, Oper, pos).ConfigureAwait(false) ;
            Log(SecurityCode + " CloseAllpositions");  
        }

        /// <summary>
        /// Закроет все ордеры по направлению this.operation
        /// </summary> 
        public void KillOperationOrders()
        {
            //if (this.Strategys == Strategy.Default) this.Isactiv = false; 
            //this.ListStopOrder.Clear(); // закомментировать когда будет сделано ожидание расчета минимума-максимума
            //this.StopLoss = 0;

            var orders =   _quik.Orders.GetOrders(this.ClassCode, this.SecurityCode).Result;
            foreach (var order in orders.Where(order => order.State == State.Active))
            {
                  _quik.Orders.KillOrder(order); 
            }

            //var Stoporders = await _quik.StopOrders.GetStopOrders(this.ClassCode, this.SecurityCode).ConfigureAwait(true);
            //foreach (var stoporder in Stoporders.Where(stoporder => 
            //             stoporder.State == State.Active 
            //             && stoporder.Operation == this.operation 
            //             && (stoporder.Flags & 0x8000) == 0)) // (0x8000)  Идет расчет минимума-максимума 
            //{
            //    await _quik.StopOrders.KillStopOrder(stoporder).ConfigureAwait(false);
            //}

            var Stoporders = _quik.StopOrders.GetStopOrders(this.ClassCode, this.SecurityCode).Result;

            foreach (var stoporder in Stoporders.Where(stoporder =>
                         stoporder.State == State.Active
                         && stoporder.Operation == this.operation//))
                                                                 //&& stoporder.FilledQuantity == stoporder.Quantity
                                                                 //&& stoporder.FilledQuantity == 0))
            && (stoporder.Flags & 0x8000) == 0)) // (0x8000)  Идет расчет минимума-максимума 
            { 
                foreach (var s in this.ListStopOrder.ToList().Where(s =>
                            stoporder.TransId == s.TransId/* && (stoporder.Flags & 0x8000) != 0*/))
                {
                    //ордеры по которым идут расчеты останутся в листе стоп-ордеров
                    this.ListStopOrder.Remove(s);
                    _quik.StopOrders.KillStopOrder(stoporder);
                }
            }

            Log(SecurityCode + " Kill Operation Orders");
        }  
        public async Task KillAllOrders()
        {
            this.ListStopOrder.Clear();
            //this.Isactiv = false;
            this.StrategyFlag = false;
            this.StopLoss = 0;

            var orders = await _quik.Orders.GetOrders(this.ClassCode, this.SecurityCode).ConfigureAwait(true);
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

            var Stoporders = await _quik.StopOrders.GetStopOrders(this.ClassCode, this.SecurityCode).ConfigureAwait(true);
            if (Stoporders.Count != 0)
            {
                foreach (var stoporder in Stoporders)
                {
                    if (stoporder.State == State.Active)
                    {
                        await _quik.StopOrders.KillStopOrder(stoporder).ConfigureAwait(true);
                    }
                }
            }
            Console.WriteLine(SecurityCode + " Kill All Orders");
        }
        private void CandlesOnNewCandle(Candle candle)
        {
            MessageBox.Show("проверка будет ли работать этот метод без включениЯ стратегии пересеченияМА");
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
        ///     Strategys test
        /// </summary>
        public Strategy Strategys // { get; set; } = Operation.Buy;
        {
            get => _strategys;
            set => SetField(ref _strategys, value,null, SwitchStrategys(value));
        }

        /// <summary>
        ///     Статус активности
        /// </summary>
        public bool Isactiv
        {
            get => _isactiv;
            set => SetField(ref _isactiv, value, null, Сheck_Isactiv(value)); 
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
        ///     Лист Стоп-Ордеров по направлению "operation"
        /// </summary>
        public ObservableCollection<StopOrder> ListStopOrder { get; set; } = [];

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

    public enum Strategy
    {
        /// <summary>
        /// Обычная стратегия, есть StopLoss, есть добавление сетки в исходное состояние
        /// если LastPrice отодвинется на некоторое расстояние от цены ближайшего ордера
        /// </summary>
        Default,
        /// <summary>
        /// Обычная стратегия, есть StopLoss, есть добавление сетки в исходное состояние
        /// если LastPrice отодвинется на некоторое расстояние от цены ближайшего ордера
        /// </summary>
        ToTrend,
        /// <summary>
        /// Стратегия такая ка Default, но сетка движется за ценой если та уходит дальше
        /// заданных пределов
        /// </summary>
        MoveNet,
        /// <summary>
        /// Стратегия пересечения средних Moving Average, медленной с периодом 20
        /// и быстрой с периодом 5, =====график надо поставить на 5 минут=====
        /// </summary>
        IntersMA
    }
}


