import os
from typing import List
import numpy as np
import pandas as pd
import talib
from loguru import logger
from strategies.constants.candlestick_patterns import reverse_lookup
from strategies.utils.config_loader import load_config

class CandlestickPatternsService:

    def __init__(self):
        self.directory = load_config().get("data").get("directory")
    
    def get_info(self, pattern: str):
        if pattern not in reverse_lookup:
            raise KeyError(f"Unknown pattern: {pattern}")
        return reverse_lookup[pattern]
    
    def get_symbols_for_pattern_and_interval(self, pattern: str, interval: str, period: int) -> List[str]:
        if pattern not in reverse_lookup:
            raise KeyError(f"Unknown pattern: {pattern}")
        if not hasattr(talib, pattern):
            raise ValueError(f"{pattern} is not a valid TA-Lib function.")
        
        # Get the function dynamically
        func = getattr(talib, pattern)
        required_cols = ["Open", "High", "Low", "Close"]
        self.folder_path = os.path.join(self.directory, interval)
        symbols = []
        for file_name in os.listdir(self.folder_path):
            # Select only files with format "<interval>-<symbol>.csv"
            if file_name.startswith(f"{interval}-") and file_name.endswith(".csv"):
                full_file_name = os.path.join(self.folder_path, file_name)
                if not os.path.isfile(full_file_name):
                    continue
                try:
                    df = pd.read_csv(full_file_name)
                    if df.empty:
                        continue                    
                    if not all(col in df.columns for col in required_cols):
                        continue
                    result = func(df["Open"], df["High"], df["Low"], df["Close"])
                    if len(result) >= period and np.any(result[-period:] != 0):
                        # Extract symbol from filename: "<interval>-<symbol>.csv"
                        symbol = file_name.split("-")[1].replace(".csv", "")
                        symbols.append(symbol)
                except Exception as e:
                    logger.warning(f"Skipping {file_name}: {e}")
        logger.info(f"Found {len(symbols)} symbols for pattern '{pattern}' and '{interval}")
        return symbols

