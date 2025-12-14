import os
import pandas as pd
from loguru import logger
from strategies.core.strategies.bollingerbands_strategy import BollingerBandsStrategy
from strategies.core.strategies.candlestick_patterns_strategy import CandlestickPatternsStrategy
from strategies.core.strategies.macd_strategy import MacdStrategy
from strategies.core.strategies.pivotpoints_strategy import PivotPointsStrategy
from strategies.core.strategies.rsi_strategy import RsiStrategy
from strategies.utils.config_loader import load_config
from strategies.core.strategies.failed_breakout_strategy import FailedBreakoutStrategy

class StrategyPipeline:
    # Map strategy names to functions
    STRATEGY_MAP = {
        "is_near_lower_bb": BollingerBandsStrategy.is_near_lower_bb,
        "is_near_upper_bb": BollingerBandsStrategy.is_near_upper_bb,
        "contains_candlestick_pattern": CandlestickPatternsStrategy.contains_candlestick_pattern,
        "is_bullish_macd_crossover": MacdStrategy.is_bullish_macd_crossover,
        "is_bearish_macd_crossover": MacdStrategy.is_bearish_macd_crossover,
        "is_last_close_near_pivotpoints": PivotPointsStrategy.is_last_close_near_pivotpoints,
        "is_rsi_overbought": RsiStrategy.is_rsi_overbought,
        "is_rsi_oversold": RsiStrategy.is_rsi_oversold,
        "is_rsi_bullish_divergence": RsiStrategy.is_rsi_bullish_divergence,
        "is_rsi_bearish_divergence": RsiStrategy.is_rsi_bearish_divergence,
        "is_failed_bo": FailedBreakoutStrategy.is_failed_bo,
        "is_failed_blbo": FailedBreakoutStrategy.is_failed_blbo,
        "is_failed_brbo": FailedBreakoutStrategy.is_failed_brbo,
    }

    INTERVAL = "1d"

    def __init__(self):
        self.symbols = []
        self.directory = load_config().get("data").get("directory")
        folder_path = os.path.join(self.directory, StrategyPipeline.INTERVAL)
        # select only files with format "<INTERVAL>-<symbol>.csv"
        for filename in os.listdir(folder_path):
            if filename.startswith(StrategyPipeline.INTERVAL + "-") and filename.endswith(".csv"):
                self.symbols.append(filename[len(StrategyPipeline.INTERVAL) + 1 : -4])

    def load_data(self, symbol, interval):
        folder_path = os.path.join(self.directory, interval)
        full_file_name = os.path.join(folder_path, f"{interval}-{symbol}.csv")
        if not os.path.exists(full_file_name):
            return None
        df = pd.read_csv(full_file_name)
        return df

    def run_pipeline(self, strategies):
        """
        {
            "strategies": [
                {"strategy":"contains_candlestick_pattern","interval":"1h","params":{"group":"bullish","subgroup":"all","pattern":"all","duration":1}},
                {"strategy":"is_last_close_near_pivotpoints","interval":"1h"},
                {"strategy":"is_rsi_oversold","interval":"5min","params":{"duration":14}},
                {"strategy":"is_near_lower_bb","interval":"5min","params":{"duration":9}},
                {"strategy":"is_bullish_macd_crossover","interval":"5min","params":{"duration":9}},
                {"strategy":"is_rsi_bullish_divergence","interval":"5min","params":{"duration":14}},
                {"strategy":"is_failed_bo","interval":"5min"},
                {"strategy":"is_failed_blbo","interval":"5min"},
                {"strategy":"is_failed_brbo","interval":"5min"},
            ]
        }
        """
        valid_symbols = []
        df = pd.DataFrame.empty
        for symbol in self.symbols:
            passed = True
            prior_interval = ""
            try:
                for item in strategies:
                    strategy_func = StrategyPipeline.STRATEGY_MAP[item.strategy]
                    if prior_interval != item.interval:
                        df = self.load_data(symbol, item.interval)
                        prior_interval = item.interval
                    if df is None:
                        passed = False
                        break

                    result = strategy_func(df, **item.params) if item.params else strategy_func(df)
                    if not result:
                        passed = False
                        break

                if passed:
                    valid_symbols.append(symbol)
            except Exception as e:
                logger.warning(f"Skipping {symbol}: {e}")        
        logger.info(f"Found {len(valid_symbols)} symbols for strategies:'{strategies}'")
        return valid_symbols
