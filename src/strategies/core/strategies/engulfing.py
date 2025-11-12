import os
from typing import List
import pandas as pd
from loguru import logger
from strategies.utils.config_loader import load_config

class EngulfingStrategy:
    def __init__(self, interval: str):
        self.interval = interval
        config = load_config()
        data = config.get("data")
        directory = data.get("directory")
        self.folder_path = os.path.join(directory, interval)

    def get_symbols(self) -> List[str]:
        symbols = []
        for file_name in os.listdir(self.folder_path):
            # Select only files with format "1d-<symbol>.csv"
            if file_name.startswith(f"{self.interval}-") and file_name.endswith(".csv"):
                full_file_name = os.path.join(self.folder_path, file_name)
                if not os.path.isfile(full_file_name):
                    continue
                try:
                    df = pd.read_csv(full_file_name)
                    if df.empty:
                        continue
                    if len(df) > 1:
                        o1, c1 = df.iloc[-2]['Open'], df.iloc[-2]['Close']
                        o2, c2 = df.iloc[-1]['Open'], df.iloc[-1]['Close']
                        if (((c1 < o1) and (c2 > o2) and (o2 < c1) and (c2 > o1)) or
                        ((c1 > o1) and (c2 < o2) and (o2 > c1) and (c2 < o1))):
                            symbol = file_name.split("-")[1].replace(".csv", "")
                            symbols.append(symbol)
                except Exception as e:
                    logger.warning(f"Skipping {file_name}: {e}")
        logger.info(f"EngulfingStrategy: Found {len(symbols)} symbols")
        return symbols
