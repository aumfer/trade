#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;
using System.Linq;
using System.Collections.Generic;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class Euclid : Strategy
    {
        #region EuclidTickSource
        struct EuclidTick
        {
            public DateTime Time;
            public double Price;
            public double Volume;
            public double Bid;
            public double Ask;
            public double Tick;
        }

        sealed class EuclidTickSource
        {
			readonly Strategy Strategy;
			readonly PeriodType PeriodType;
			readonly double PeriodValue;
			
			public EuclidTickSource(Strategy strategy, PeriodType periodType, double periodValue)
			{
				Strategy = strategy;
				PeriodType = periodType;
				PeriodValue = periodValue;
			}
			
			public IEnumerable<EuclidBar> Bars(List<EuclidTick> ticks)
			{
				var b = new EuclidBar();
				for (var i = ticks.Count-1; i >= 0; --i)
                {
					var t = ticks[i];
                    b.OnTick(t);
                    if (b.IsComplete(PeriodType, PeriodValue))
                    {
                        yield return b;
                        b = new EuclidBar();
                    }
                }
			}
        }
        #endregion

        #region EuclidBar
        struct EuclidBar
        {
            public void OnTick(EuclidTick t)
            {
                if (StartTime == DateTime.MinValue || StartTime > t.Time)
                {
                    StartTime = t.Time;
                }
				if (EndTime == DateTime.MinValue || EndTime < t.Time)
				{
                	EndTime = t.Time;
				}

                {
                    var delta = t.Price - Vwap;
                    var r = delta * t.Volume / (Volume + t.Volume);
                    Vwap += r;
                    m2 += Volume * delta * r;
                    Volume += t.Volume;
                }

                var mid = (t.Bid + t.Ask) / 2.0;
                if (t.Price > mid)
                {
                    BidVolume += t.Volume;
                }
                else if (t.Price < mid)
                {
                    AskVolume += t.Volume;
                }

                if (Open == 0.0)
                {
                    Open = t.Price;
                }
                if (t.Price > High || High == 0.0)
                {
                    High = t.Price;
                }
                if (t.Price < Low || Low == 0.0)
                {
                    Low = t.Price;
                }
                Close = t.Price;

                Tick += t.Tick;
                ++TradeCount;
            }
			
			public bool IsComplete(PeriodType periodType, double periodValue)
			{
				switch (periodType)
				{
					default:
						throw new Exception("Invalid PeriodType");
					case PeriodType.Tick:
						return TradeCount >= periodValue;
					case PeriodType.Volume:
						return Volume >= periodValue;
					case PeriodType.Second:
						return Duration >= TimeSpan.FromSeconds(periodValue);
					case PeriodType.Minute:
						return Duration >= TimeSpan.FromMinutes(periodValue);
					case PeriodType.Day:
						return Duration >= TimeSpan.FromDays(periodValue);
				}
			}
			
			public double ZScore(double value)
			{
				return (value - Vwap) / VwStdDev;
			}
			public double ZValue(double zscore)
			{
				return zscore * VwStdDev + Vwap;
			}

            double m2;

            public double Volume { get; private set; }
            public double BidVolume { get; private set; }
            public double AskVolume { get; private set; }
            public double High { get; private set; }
            public double Low { get; private set; }
            public double Open { get; private set; }
            public double Close { get; private set; }
            public double Tick { get; private set; }
            public double TradeCount { get; private set; }
            public DateTime StartTime { get; private set; }
            public DateTime EndTime { get; private set; }

            public double BidAskVolume
            {
                get
                {
                    var totalVolume = BidVolume + AskVolume;
                    var ratio = totalVolume > 0.0 ? (BidVolume - AskVolume) / totalVolume : 0.0;
                    return ratio;
                }
            }

            public double Vwap
            {
                get;
                set;
            }
            public double VwVariance
            {
                get
                {
                    return (m2 / Volume) * (TradeCount / (TradeCount - 1));
                }
            }
            public double VwStdDev
            {
                get
                {
                    return Math.Sqrt(VwVariance);
                }
            }
            public double VwUpperStdDev
            {
                get
                {
                    return Vwap + VwStdDev;
                }
            }
            public double VwLowerStdDev
            {
                get
                {
                    return Vwap - VwStdDev;
                }
            }

            public TimeSpan Duration
            {
                get
                {
                    return EndTime - StartTime;
                }
            }
        }
        #endregion

        #region Variables
        int positionSize = 1;
		double minRR = 2.0;
        int debugMode = 1;

        double tick;

        List<EuclidTick> ticks;
        #endregion

        #region Constructors
        public Euclid()
        {
        }
        #endregion

        #region Methods
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
			ticks = new List<EuclidTick>();
			
            Add(Instrument.FullName, PeriodType.Tick, 1, MarketDataType.Last);
            Add(Instrument.FullName, PeriodType.Tick, 1, MarketDataType.Bid);
            Add(Instrument.FullName, PeriodType.Tick, 1, MarketDataType.Ask);
            Add("^TICK", PeriodType.Tick, 1, MarketDataType.Last);
			Add(Instrument.FullName, PeriodType.Day, 1);

            ExitOnCloseSeconds = (int)TimeSpan.FromMinutes(35.0).TotalSeconds;
            ExcludeTradeHistoryInBacktest = DebugMode == 0;
            MultiThreadSupport = true;

            CalculateOnBarClose = true;
        }

        protected override void OnStartUp()
        {
			Print("OnStartUp " + BarsPeriod.Id.ToString() + " " + BarsPeriod.Value);
        }

        void OnTrade()
        {
            var t = new EuclidTick()
            {
                Time = Time[0],
                Price = Close[0],
                Volume = Volume[0],
                //Bid = Closes[2][0],
                //Ask = Closes[3][0],
                Tick = tick
            };
			if (Closes[2].Count > 0)
			{
				t.Bid = Closes[2][0];
			}
			if (Closes[3].Count > 0)
			{
				t.Ask = Closes[3][0];
			}
            ticks.Add(t);
            tick = 0.0;

            if (/*!Historical || */Time[0].Minute != Time[1].Minute)
            {
                OnStrategy();
            }
        }

        void OnBid()
        {
        }
        void OnAsk()
        {
        }

        void OnTick()
        {
            var t = Close[0];
            tick += t;
        }

        void OnFlat()
        {
            if (IsTimeToEnter)
            {
                var bars = CurrentBars;
                if (bars.Any(b => b.Length < 2)) return;
				
				var close = Close[0];
				var zscores = bars.Select(b => b[0].ZScore(close)).ToArray();
				var ranges = bars.Select(b => b[0].VwStdDev / bars.Last()[0].VwStdDev).ToArray();

                var stop = Instrument.MasterInstrument.Round2TickSize(bars[2][0].Vwap);
                //var shortTarget = Instrument.MasterInstrument.Round2TickSize(bars.Min(b => b[0].VwLowerStdDev));
				var shortTarget = Instrument.MasterInstrument.Round2TickSize(bars[1][0].VwLowerStdDev);
                //var longTarget = Instrument.MasterInstrument.Round2TickSize(bars.Max(b => b[0].VwUpperStdDev));
				var longTarget = Instrument.MasterInstrument.Round2TickSize(bars[1][0].VwUpperStdDev);
				var shortRisk = stop - close;
				var shortProfit = close - shortTarget;
                var longRisk = close - stop;
				var longProfit = longTarget - close;

                if (true
					&& shortRisk > bars[1][0].VwStdDev && shortRisk > shortProfit * MinRR
					&& close > shortTarget + Instrument.MasterInstrument.TickSize && stop > close + Instrument.MasterInstrument.TickSize
					//&& zscores[0] > 1.0
					//&& zscores[1] > 1.0
					//&& zscores[2] < 0.0
					)
                {
                    SetStopLoss(CalculationMode.Price, stop);
					SetProfitTarget(CalculationMode.Price, shortTarget);
					EnterShortLimit(1, true, PositionSize, shortTarget + Instrument.MasterInstrument.TickSize, "EnterShortLimit");
					//EnterShort();
					if (!Historical || DebugMode > 0)
					{
						Print(String.Format("{0}\t{1}\tENTER SHORT 1@{2}\tTARGET {3}\tSTOP {4}", Time[0], Instrument.FullName, close, shortTarget, stop));
					}
                }
                else if (true
					&& longRisk > bars[1][0].VwStdDev && longRisk > longProfit * MinRR
					&& close < longTarget - Instrument.MasterInstrument.TickSize && stop < close - Instrument.MasterInstrument.TickSize
					//&& zscores[0] < -1.0
					//&& zscores[1] < -1.0
					//&& zscores[2] > 0.0
					)
                {
                    SetStopLoss(CalculationMode.Price, stop);
					SetProfitTarget(CalculationMode.Price, longTarget);
					EnterLongLimit(1, true, PositionSize, longTarget - Instrument.MasterInstrument.TickSize, "EnterLongLimit");
					//EnterLong();
					if (!Historical || DebugMode > 0)
					{
						Print(String.Format("{0}\t{1}\tENTER LONG 1@{2}\tTARGET {3}\tSTOP {4}", Time[0], Instrument.FullName, close, longTarget, stop));
					}
                }
				
				int maxTicks = (int)Math.Ceiling(bars.Max(bs => bs.Sum(b => b.TradeCount)));
				if (maxTicks < ticks.Count)
				{
					ticks = ticks.Skip(ticks.Count - maxTicks).ToList();
					//GC.Collect();
				}
            }
        }

        void OnLong()
        {
            if (IsTimeToExit)
            {
                ExitLong();
            }
        }

        void OnShort()
        {
            if (IsTimeToExit)
            {
                ExitShort();
            }
        }

        void OnStrategy()
        {
            switch (Position.MarketPosition)
            {
                case MarketPosition.Flat:
                    OnFlat();
                    break;
                case MarketPosition.Long:
                    OnLong();
                    break;
                case MarketPosition.Short:
                    OnShort();
                    break;
            }
        }

        void OnPrimaryBar()
        {
            if (!IsTimeToEnter || IsTimeToExit)
            {
                return;
            }
			
			if (Historical && DebugMode < 1)
			{
				return;
			}

            var bars = CurrentBars;
            if (bars.Any(b => b.Length < 2)) return;

            if (bars[1][0].Tick > bars[1][0].Tick)
            {
                DrawTriangleUp("mid VWAP " + CurrentBar, true, Time[0], bars[1][0].Vwap, Color.DarkBlue);
            }
            else if (bars[1][0].Tick < bars[1][0].Tick)
            {
                DrawTriangleDown("mid VWAP " + CurrentBar, true, Time[0], bars[1][0].Vwap, Color.DarkBlue);
            }
            else
            {
                DrawDot("mid VWAP " + CurrentBar, true, Time[0], bars[1][0].Vwap, Color.DarkBlue);
            }

            DrawDot("mid VWAP + StdDev " + CurrentBar, false, Time[0], bars[1][0].Vwap + bars[1][0].VwStdDev, Color.MediumBlue);
            DrawDot("mid VWAP - StdDev " + CurrentBar, false, Time[0], bars[1][0].Vwap - bars[1][0].VwStdDev, Color.MediumBlue);

            if (bars[2][0].Tick > bars[2][0].Tick)
            {
                DrawTriangleUp("lg VWAP " + CurrentBar, true, Time[0], bars[2][0].Vwap, Color.DarkOrange);
            }
            else if (bars[2][0].Tick < bars[1][0].Tick)
            {
                DrawTriangleDown("lg VWAP " + CurrentBar, true, Time[0], bars[2][0].Vwap, Color.DarkOrange);
            }
            else
            {
                DrawDot("lg VWAP " + CurrentBar, true, Time[0], bars[2][0].Vwap, Color.DarkOrange);
            }

            DrawDot("lg VWAP + StdDev " + CurrentBar, false, Time[0], bars[2][0].Vwap + bars[2][0].VwStdDev, Color.Orange);
            DrawDot("lg VWAP - StdDev " + CurrentBar, false, Time[0], bars[2][0].Vwap - bars[2][0].VwStdDev, Color.Orange);
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
            try
            {
                switch (BarsInProgress)
                {
                    case 0:
                        OnPrimaryBar();
                        break;
                    case 1:
                        OnTrade();
                        break;
                    case 2:
                        OnBid();
                        break;
                    case 3:
                        OnAsk();
                        break;
                    case 4:
                        OnTick();
                        break;

                }
            }
            catch (Exception e)
            {
                Print(e.Message);
                Print(e.StackTrace);
                throw e;
            }
        }
		
		EuclidBar[] SmallBars
		{
			get
			{
				return new EuclidTickSource(this, PeriodType.Volume, SMA(Volumes[5], 5)[0] * 0.01).Bars(ticks).Take(2).ToArray();
			}
		}
		EuclidBar[] MediumBars
		{
			get
			{
				return new EuclidTickSource(this, PeriodType.Volume, SMA(Volumes[5], 5)[0] * 0.1).Bars(ticks).Take(2).ToArray();
			}
		}
		EuclidBar[] LargeBars
		{
			get
			{
				return new EuclidTickSource(this, PeriodType.Volume, SMA(Volumes[5], 5)[0] * 1).Bars(ticks).Take(2).ToArray();
			}
		}

        EuclidBar[][] CurrentBars
        {
            get
            {
				return new EuclidBar[][]
				{
					SmallBars,
					MediumBars,
					LargeBars
				};
            }
        }
        #endregion

        #region Properties
		/// <summary>
		/// </summary>
		[Description("Minimum Risk:Reward")]
		[GridCategory("Parameters")]
		public double MinRR
		{
			get { return minRR; }
			set { minRR = value; }
		}
		
		public int PositionSize
		{
			get { return positionSize; }
			set { positionSize = Math.Max(1, value); }
		}
		
        bool IsTimeToEnter
        {
            get
            {
				return Time[0].TimeOfDay > TimeSpan.FromHours(9.5) && Time[0].TimeOfDay < TimeSpan.FromHours(12.0);
                //return TradingHours.IsTimeToEnter(Instrument.FullName, Time[0].TimeOfDay);
				//return !Bars.SessionBreak;
				//return !IsTimeToExit;
            }
        }
        bool IsTimeToExit
        {
            get
            {
				return !IsTimeToEnter;
				//return Bars.Session.NextEndTime - Time[0] <= TimeSpan.FromSeconds(ExitOnCloseSeconds);
                //return TradingHours.IsTimeToExit(Instrument.FullName, Time[0].TimeOfDay);
            }
        }
        #endregion

        #region Parameters
        public int DebugMode
        {
            get { return debugMode; }
            set { debugMode = value; }
        }
        #endregion
    }
}
