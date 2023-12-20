#region Using declarations
using System;
using System.Collections.Generic;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public static class TradingHours
	{
		static TimeSpan OpenPadding = TimeSpan.FromMinutes(30.0);
		static TimeSpan ClosePadding = TimeSpan.FromMinutes(90.0);
		
		static Func<TimeSpan, bool> NormalHours(string start, string end)
		{
			var startTime = TimeSpan.Parse(start) + OpenPadding;
			var endTime = TimeSpan.Parse(end) - ClosePadding;
			return t => t >= startTime && t < endTime;
		}
		
		static readonly Dictionary<string, Func<TimeSpan, bool>> TradeHours = new Dictionary<string, Func<TimeSpan, bool>>()
		{
			{ "6A",		NormalHours("8:20", "15:00")		},	//Australian Dollar
			{ "6B",		NormalHours("8:20", "15:00")		},	//British Pound
			{ "6C",		NormalHours("8:20", "15:00")		},	//Canadian Dollar
			{ "6E",		NormalHours("8:20", "15:00")		},	//Euro
			{ "6J",		NormalHours("8:20", "15:00")		},	//Japanese Yen
			{ "6L",		NormalHours("8:20", "15:00")		},	//Brazilian Real
			{ "6M",		NormalHours("8:20", "15:00")		},	//Mexican Peso
			{ "6N",		NormalHours("8:20", "15:00")		},	//New Zealand Dollar
			{ "6R",		NormalHours("8:20", "15:00")		},	//Russian Ruble
			{ "6S",		NormalHours("8:20", "15:00")		},	//Swiss Franc
			{ "6Z",		NormalHours("8:20", "15:00")		},	//South African Rand
			//{ "CC",		NormalHours("4:00", "14:00")		},	//Cocoa
			{ "CL",		NormalHours("9:00", "14:30")		},	//Crude Oil
			//{ "CT",		OvernightHours("21:00", "14:30")	},	//Cotton
			//{ "DX",		OvernightHours("20:00", "17:00")	}, 	//US Dollar Index
			{ "EMD", 	NormalHours("9:30", "16:15")		},	//S&P 500
			{ "ES", 	NormalHours("9:30", "16:15")		},	//S&P 500
			//{ "FDAX",	NormalHours("1:50", "16:00")		},	//Dax
			//{ "FESX",	NormalHours("1:50", "16:00")		},	//Euro Stoxx
			//{ "FGBL",	NormalHours("2:00", "16:00")		},	//Euro 10-yr Bund
			//{ "FGBM",	NormalHours("2:00", "16:00")		},	//Euro 5-yr Bobl
			//{ "FGBS",	NormalHours("2:00", "16:00")		},	//Euro 2-yr Shatz
			{ "GC", 	NormalHours("8:20", "13:30")		},	//Gold
			{ "GE",		NormalHours("8:20", "15:00")		},	//Eurodollar
			{ "GF", 	NormalHours("10:05", "14:00")		},	//Feeder Cattle
			{ "HE", 	NormalHours("10:05", "14:00")		},	//Lean Hogs
			{ "HG", 	NormalHours("8:10", "13:00")		},	//Copper
			//{ "KC", 	NormalHours("3:30", "14:00")		},	//Coffee
			{ "LE", 	NormalHours("10:05", "14:00")		},	//Live Cattle
			{ "NG",		NormalHours("9:00", "14:30")		},	//Natural Gas
			{ "NKD", 	NormalHours("9:00", "16:15")		},	//Nikkei 255
			{ "NQ", 	NormalHours("9:30", "16:15")		},	//NASDAQ 100
			//{ "QH", 	NormalHours("9:00", "14:30")		},	//Heating Oil
			{ "RB", 	NormalHours("9:00", "14:30")		},	//Gasoline
			//{ "SB", 	NormalHours("2:30", "14:00")		},	//Sugar
			{ "SI", 	NormalHours("8:25", "13:25")		},	//Silver
			//{ "TF", 	NormalHours("9:30", "16:15")		},	//Russell 2000
			{ "YM", 	NormalHours("9:30", "16:15")		},	//DOW 30
			{ "ZB", 	NormalHours("8:20", "15:00")		},	//US 30-yr Bonds
			//{ "ZC", 	NormalHours("9:30", "14:00")		},	//Corn
			{ "ZF",		NormalHours("8:20", "15:00")		},	//US 5-yr Notes
			//{ "ZG", 	OvernightHours("19:16", "17:00")	}, 	//Gold
			//{ "ZI", 	OvernightHours("19:16", "17:00")	}, 	//Silver
			{ "ZL", 	NormalHours("9:30", "14:15")		},	//Soybean Oil
			{ "ZM", 	NormalHours("9:30", "14:15")		},	//Soybean Meal
			{ "ZN", 	NormalHours("8:20", "15:00")		},	//US 30-yr Notes
			{ "ZO", 	NormalHours("9:30", "14:15")		},	//Oats
			{ "ZQ",		NormalHours("8:20", "15:00")		},	//US 30-day Funds
			{ "ZR", 	NormalHours("9:30", "14:15")		},	//Rough Rice
			{ "ZS", 	NormalHours("9:30", "14:15")		},	//Soybeans
			{ "ZT",		NormalHours("8:20", "15:00")		},	//US 2-yr Notes
			{ "ZW", 	NormalHours("9:30", "14:15")		},	//Wheat
		};
		
		public static bool IsTimeToTrade(string instrument, TimeSpan time)
		{
			foreach (var kvp in TradeHours)
			{
				if (instrument.StartsWith(kvp.Key))
				{
					return kvp.Value(time);
				}
			}
			throw new Exception("No trade hours for " + instrument);
		}
		
		public static bool IsTimeToEnter(string instrument, TimeSpan time)
		{
			return IsTimeToTrade(instrument, time);
		}
		
		public static bool IsTimeToExit(string instrument, TimeSpan time)
		{
			return !IsTimeToTrade(instrument, time);
		}
	}
}
