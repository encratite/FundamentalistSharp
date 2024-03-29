namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class CurrentAssets
	{
		public decimal? AccountsReceivableTradeNet { get; set; }
		public decimal? TotalReceivablesNet { get; set; }
		public decimal? TotalInventory { get; set; }
		public decimal? OtherCurrentAssetsTotal { get; set; }
		public decimal? TotalCurrentAssets { get; set; }
		public decimal? PropertyPlantEquipmentTotalGross { get; set; }
		public decimal? AccumulatedDepreciationTotal { get; set; }
		public decimal? IntangiblesNet { get; set; }
		public decimal? LongTermInvestments { get; set; }
		public decimal? TotalAssets { get; set; }
		public decimal? ShortTermInvestments { get; set; }
		public decimal? CashAndShortTermInvestments { get; set; }
		public decimal? CashEquivalents { get; set; }
		public decimal? GoodwillNet { get; set; }
		public decimal? AccountsReceivableTradeGross { get; set; }
		public decimal? ReceivablesOther { get; set; }
		public decimal? InventoriesFinishedGoods { get; set; }
		public decimal? InventoriesWorkInProgress { get; set; }
		public decimal? InventoriesRawMaterials { get; set; }
		public decimal? OtherCurrentAssets { get; set; }
		public decimal? BuildingsGross { get; set; }
		public decimal? LandImprovementsGross { get; set; }
		public decimal? MachineryEquipmentGross { get; set; }
		public decimal? OtherPropertyPlantEquipmentGross { get; set; }
		public decimal? IntangiblesGross { get; set; }
		public decimal? AccumulatedIntangibleAmortization { get; set; }
		public decimal? LtInvestmentAffiliateCompanies { get; set; }
		public decimal? OtherLongTermAssets { get; set; }
		public decimal? OtherLongTermAssetsTotal { get; set; }
		public decimal? PayableAccrued { get; set; }
		public decimal? AccruedExpenses { get; set; }
		public decimal? PropertyPlantEquipmentTotalNet { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				AccountsReceivableTradeNet,
				TotalReceivablesNet,
				TotalInventory,
				OtherCurrentAssetsTotal,
				TotalCurrentAssets,
				PropertyPlantEquipmentTotalGross,
				AccumulatedDepreciationTotal,
				IntangiblesNet,
				LongTermInvestments,
				TotalAssets,
				ShortTermInvestments,
				CashAndShortTermInvestments,
				CashEquivalents,
				GoodwillNet,
				AccountsReceivableTradeGross,
				ReceivablesOther,
				InventoriesFinishedGoods,
				InventoriesWorkInProgress,
				InventoriesRawMaterials,
				OtherCurrentAssets,
				BuildingsGross,
				LandImprovementsGross,
				MachineryEquipmentGross,
				OtherPropertyPlantEquipmentGross,
				IntangiblesGross,
				AccumulatedIntangibleAmortization,
				LtInvestmentAffiliateCompanies,
				OtherLongTermAssets,
				OtherLongTermAssetsTotal,
				PayableAccrued,
				AccruedExpenses,
				PropertyPlantEquipmentTotalNet
			);
		}

		public static FeatureName[] GetFeatureNames(CurrentAssets @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(CurrentAssets), nameof(AccountsReceivableTradeNet), @this?.AccountsReceivableTradeNet),
				new FeatureName(nameof(CurrentAssets), nameof(TotalReceivablesNet), @this?.TotalReceivablesNet),
				new FeatureName(nameof(CurrentAssets), nameof(TotalInventory), @this?.TotalInventory),
				new FeatureName(nameof(CurrentAssets), nameof(OtherCurrentAssetsTotal), @this?.OtherCurrentAssetsTotal),
				new FeatureName(nameof(CurrentAssets), nameof(TotalCurrentAssets), @this?.TotalCurrentAssets),
				new FeatureName(nameof(CurrentAssets), nameof(PropertyPlantEquipmentTotalGross), @this?.PropertyPlantEquipmentTotalGross),
				new FeatureName(nameof(CurrentAssets), nameof(AccumulatedDepreciationTotal), @this?.AccumulatedDepreciationTotal),
				new FeatureName(nameof(CurrentAssets), nameof(IntangiblesNet), @this?.IntangiblesNet),
				new FeatureName(nameof(CurrentAssets), nameof(LongTermInvestments), @this?.LongTermInvestments),
				new FeatureName(nameof(CurrentAssets), nameof(TotalAssets), @this?.TotalAssets),
				new FeatureName(nameof(CurrentAssets), nameof(ShortTermInvestments), @this?.ShortTermInvestments),
				new FeatureName(nameof(CurrentAssets), nameof(CashAndShortTermInvestments), @this?.CashAndShortTermInvestments),
				new FeatureName(nameof(CurrentAssets), nameof(CashEquivalents), @this?.CashEquivalents),
				new FeatureName(nameof(CurrentAssets), nameof(GoodwillNet), @this?.GoodwillNet),
				new FeatureName(nameof(CurrentAssets), nameof(AccountsReceivableTradeGross), @this?.AccountsReceivableTradeGross),
				new FeatureName(nameof(CurrentAssets), nameof(ReceivablesOther), @this?.ReceivablesOther),
				new FeatureName(nameof(CurrentAssets), nameof(InventoriesFinishedGoods), @this?.InventoriesFinishedGoods),
				new FeatureName(nameof(CurrentAssets), nameof(InventoriesWorkInProgress), @this?.InventoriesWorkInProgress),
				new FeatureName(nameof(CurrentAssets), nameof(InventoriesRawMaterials), @this?.InventoriesRawMaterials),
				new FeatureName(nameof(CurrentAssets), nameof(OtherCurrentAssets), @this?.OtherCurrentAssets),
				new FeatureName(nameof(CurrentAssets), nameof(BuildingsGross), @this?.BuildingsGross),
				new FeatureName(nameof(CurrentAssets), nameof(LandImprovementsGross), @this?.LandImprovementsGross),
				new FeatureName(nameof(CurrentAssets), nameof(MachineryEquipmentGross), @this?.MachineryEquipmentGross),
				new FeatureName(nameof(CurrentAssets), nameof(OtherPropertyPlantEquipmentGross), @this?.OtherPropertyPlantEquipmentGross),
				new FeatureName(nameof(CurrentAssets), nameof(IntangiblesGross), @this?.IntangiblesGross),
				new FeatureName(nameof(CurrentAssets), nameof(AccumulatedIntangibleAmortization), @this?.AccumulatedIntangibleAmortization),
				new FeatureName(nameof(CurrentAssets), nameof(LtInvestmentAffiliateCompanies), @this?.LtInvestmentAffiliateCompanies),
				new FeatureName(nameof(CurrentAssets), nameof(OtherLongTermAssets), @this?.OtherLongTermAssets),
				new FeatureName(nameof(CurrentAssets), nameof(OtherLongTermAssetsTotal), @this?.OtherLongTermAssetsTotal),
				new FeatureName(nameof(CurrentAssets), nameof(PayableAccrued), @this?.PayableAccrued),
				new FeatureName(nameof(CurrentAssets), nameof(AccruedExpenses), @this?.AccruedExpenses),
				new FeatureName(nameof(CurrentAssets), nameof(PropertyPlantEquipmentTotalNet), @this?.PropertyPlantEquipmentTotalNet)
			);
		}
	}
}
