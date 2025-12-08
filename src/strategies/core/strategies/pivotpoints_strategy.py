import os
import pandas as pd
from strategies.utils.config_loader import load_config

class PivotPointsStrategy:
    @staticmethod
    def _pivot_points(symbol: str) -> dict:
        """
        Calculate daily pivot points (PP, S1-S3, R1-R3) using the previous day's data.
        Assumes df contains at least one full day of OHLC data.
        """
        directory = load_config().get("data").get("directory")
        folder_path = os.path.join(directory, "1d")
        full_file_name = os.path.join(folder_path, f"1d-{symbol}.csv")
        if not os.path.exists(full_file_name):
            return None
        df = pd.read_csv(full_file_name)
        prev_day = df.iloc[-2]
        high, low, close = prev_day["High"], prev_day["Low"], prev_day["Close"]

        pp = (high + low + close) / 3
        r1 = 2*pp - low
        s1 = 2*pp - high
        r2 = pp + (high - low)
        s2 = pp - (high - low)
        r3 = high + 2*(pp - low)
        s3 = low - 2*(high - pp)

        return {"PP": pp, "R1": r1, "S1": s1, "R2": r2, "S2": s2, "R3": r3, "S3": s3}

    @staticmethod
    def is_last_close_near_pivotpoints(df: pd.DataFrame, levels=None, tolerance=0.01) -> bool:
        """
        Returns True if the latest close is near one of the specified pivot levels.
        levels = list of strings, e.g. ["PP","S1","R1"]
        tolerance = % distance allowed (e.g., 0.01 = 1%)
        """
        pivots = PivotPointsStrategy._pivot_points(df.iloc[0]["Symbol"])
        if not pivots:
            return False
        latest_close = df["Close"].iloc[-1]

        # If no specific levels provided, check all
        levels_to_check = levels if levels else pivots.keys()

        for lvl in levels_to_check:
            if lvl in pivots:
                if abs(latest_close - pivots[lvl]) / latest_close <= tolerance:
                    return True
        return False
