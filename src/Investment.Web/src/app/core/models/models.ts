export interface Asset {
  assetId: number;
  assetCode: string;
  assetName: string;
  assetType: string;
  currency: string;
  externalTicker?: string;
  notes?: string;
  isDailyAccrualFund: boolean;
  dailyAccrualAnnualRatePercent: number;
  goldCashbackPerGram: number;
  closedRealizedPnL: number;
  isActive: boolean;
  createdAt: Date;
  portfolios: string[];
}

export interface CreateAsset {
  assetCode: string;
  assetName: string;
  assetType: string;
  currency: string;
  externalTicker?: string;
  notes?: string;
  isDailyAccrualFund: boolean;
  dailyAccrualAnnualRatePercent?: number;
  goldCashbackPerGram?: number;
  isActive: boolean;
}

export interface AssetSummary {
  assetId: number;
  assetCode: string;
  assetName: string;
  assetType: string;
  isDailyAccrualFund: boolean;
  dailyAccrualAnnualRatePercent: number;
  goldCashbackPerGram: number;
  isClosedPosition: boolean;
  totalUnitsHeld: number;
  averageBuyPrice: number;
  totalCostBasis: number;
  totalFeesPaid: number;
  totalPaidIncludingFees: number;
  currentPrice: number;
  currentValue: number;
  unrealizedPnL: number;
  unrealizedPnLPercent: number;
  realizedPnL: number;
  realizedPnLPercent: number;
  totalPnL: number;
  totalPnLPercent: number;
}

export interface ExternalAssetSearchResult {
  assetCode: string;
  assetName: string;
  assetType: string;
  currency: string;
  externalTicker?: string;
  isDailyAccrualFund: boolean;
}

export interface EnsureAssetRequest {
  assetCode: string;
  assetName: string;
  assetType: string;
  currency: string;
  externalTicker?: string;
  isDailyAccrualFund?: boolean;
}

export interface CreateManualAsset {
  assetCode: string;
  assetName: string;
  assetType: string;
  currency: string;
  notes?: string;
  initialPrice?: number;
  isDailyAccrualFund: boolean;
  dailyAccrualAnnualRatePercent?: number;
  goldCashbackPerGram?: number;
}

export interface AssetFinancialSettings {
  goldCashbackPerGram?: number;
  dailyAccrualAnnualRatePercent?: number;
}

/** Manual asset + first transaction in one step (أصل آخر). */
export interface CreateManualAssetDraft {
  assetCode: string;
  assetName: string;
  assetType: string;
  currency: string;
  notes?: string;
  isDailyAccrualFund: boolean;
  transactionType: string;
  transactionDate: Date;
  quantity: number;
  pricePerUnit: number;
  fees: number;
  manufacturingFeePerGram: number;
}

export interface SetAssetCurrentPrice {
  price: number;
  priceDate?: Date;
}

export interface CreateTransactionDraft {
  asset: ExternalAssetSearchResult;
  transactionType: string;
  transactionDate: Date;
  quantity: number;
  pricePerUnit: number;
  fees: number;
  manufacturingFeePerGram: number;
  dividendKind?: string;
  notes?: string;
}

export interface Transaction {
  transactionId: number;
  assetId: number;
  assetCode: string;
  assetName: string;
  assetType: string;
  isDailyAccrualFund: boolean;
  transactionType: string;
  transactionDate: Date;
  quantity: number;
  pricePerUnit: number;
  totalAmount: number;
  fees: number;
  manufacturingFeePerGram: number;
  netAmount: number;
  dividendKind?: string;
  notes?: string;
  createdAt: Date;
}

export interface CreateTransaction {
  assetId: number;
  transactionType: string;
  transactionDate: Date;
  quantity: number;
  pricePerUnit: number;
  fees: number;
  manufacturingFeePerGram: number;
  dividendKind?: string;
  notes?: string;
}

export interface Price {
  priceId: number;
  assetId: number;
  assetCode: string;
  assetName: string;
  priceDate: Date;
  price: number;
  source: string;
  createdAt: Date;
}

export interface CreatePrice {
  assetId: number;
  priceDate: Date;
  price: number;
}

export interface BulkPriceItem {
  assetCode: string;
  date: Date;
  price: number;
}

export interface Portfolio {
  portfolioId: number;
  name: string;
  description?: string;
  createdAt: Date;
  assets: Asset[];
}

export interface CreatePortfolio {
  name: string;
  description?: string;
}

export interface PortfolioSummary {
  portfolioId: number;
  name: string;
  totalInvested: number;
  currentValue: number;
  unrealizedPnL: number;
  unrealizedPnLPercent: number;
  realizedPnL: number;
  assetSummaries: AssetSummary[];
}

export interface Holding {
  assetId: number;
  assetCode: string;
  assetName: string;
  assetType: string;
  isDailyAccrualFund: boolean;
  dailyAccrualAnnualRatePercent: number;
  goldCashbackPerGram: number;
  totalUnitsHeld: number;
  weightedAverageBuyPrice: number;
  totalCostBasis: number;
  totalFeesPaid: number;
  totalPaidIncludingFees: number;
  currentPrice: number;
  currentValue: number;
  unrealizedPnL: number;
  unrealizedPnLPercent: number;
  realizedPnL: number;
  realizedPnLPercent: number;
  totalPnL: number;
  totalPnLPercent: number;
}

export interface AssetPerformance {
  assetId: number;
  assetCode: string;
  assetName: string;
  assetType: string;
  startValue: number;
  endValue: number;
  returnPercent: number;
  investedInPeriod: number;
}

export interface Performance {
  period: string;
  fromDate: Date;
  toDate: Date;
  startingValue: number;
  endingValue: number;
  netInvestedCapital: number;
  absoluteReturn: number;
  percentageReturn: number;
  assetBreakdown: AssetPerformance[];
}

export interface PortfolioAnalyticsSummary {
  totalInvestedCapital: number;
  totalCurrentValue: number;
  totalUnrealizedPnL: number;
  totalUnrealizedPnLPercent: number;
  totalRealizedPnL: number;
  totalFeesPaid: number;
  portfolioReturnSinceInception: number;
}

export interface PriceFetchStatus {
  currentMode: string;
  lastRunTime?: Date;
  activeAssetCount: number;
  assetsWithTicker: number;
  dailyApiCallsUsed: number;
  lastAssetsUpdated?: number;
}

export interface PriceProviderKeyStatus {
  index: number;
  label: string;
  name: string;
  email: string;
  subscriptionType: string;
  apiRequestsUsedToday: number;
  dailyRateLimit: number;
  extraLimit: number;
  totalAvailable: number;
  remaining: number;
  available: boolean;
}

export interface PriceProviderStatus {
  hasApiKey?: boolean;
  name: string;
  email: string;
  subscriptionType: string;
  apiRequestsUsedToday: number;
  dailyRateLimit: number;
  extraLimit: number;
  totalAvailable: number;
  remaining: number;
  keyCount?: number;
  keys?: PriceProviderKeyStatus[];
}

export interface PriceFetchLog {
  id: number;
  fetchDate: Date;
  mode: string;
  assetsUpdated: number;
  totalAssets: number;
  errors?: string;
  success: boolean;
  durationMs: number;
}
