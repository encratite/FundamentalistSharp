﻿namespace Fundamentalist.Trainer
{
	internal class Stock
	{
		public decimal InitialInvestment { get; set; }
		public DateTime BuyDate { get; set; }
		public decimal BuyPrice { get; set; }
		public DataPoint Data { get; set; }
	}
}
