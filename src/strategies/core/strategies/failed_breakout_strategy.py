import numpy as np
import pandas as pd

class FailedBreakoutStrategy:

    @staticmethod
    def is_failed_bo(df: pd.DataFrame, max_accept_bars: int = 3) -> bool:
        return FailedBreakoutStrategy.is_failed_brbo(df, max_accept_bars) or FailedBreakoutStrategy.is_failed_blbo(df, max_accept_bars)
    
    # Wyckoff-Aligned Failed Bear Auction (Single-Close Acceptance)
    # Acceptance on the SAME bar is invalid in Wyckoff, but OK here
    @staticmethod
    def is_failed_brbo(df: pd.DataFrame, max_accept_bars: int = 3) -> bool:
        df = df.copy()
        df["Timestamp"] = pd.to_datetime(df["Timestamp"])

        if df.empty:
            return False

        # --- Define current and previous day based on data ---
        last_ts = df["Timestamp"].max()
        current_day = last_ts.normalize()
        prev_ts = df[df["Timestamp"] < current_day]["Timestamp"].max()
        if pd.isna(prev_ts):
            return False 
        prev_day = prev_ts.normalize()
        if pd.isna(prev_day):
            return False  # no data for previous day

        # --- compute LOY ---
        df_prev_day = df[df["Timestamp"].dt.normalize() == prev_day]
        if df_prev_day.empty:
            return False

        loy = df_prev_day["Low"].min()

        # --- signal detection ---
        df_current_day = df[df["Timestamp"].dt.normalize() == current_day]
        if df_current_day.empty:
            return False

        df_current_day.sort_values("Timestamp", inplace=True)
        df_current_day.reset_index(drop=True, inplace=True)

        lows = df_current_day["Low"].values
        closes = df_current_day["Close"].values
        n = len(df_current_day)

        for i in range(n):
            # 1. Break below LOY
            if lows[i] < loy:
                # Wyckoff: rejection must occur quickly
                search_end = min(i + max_accept_bars + 1, n)

                # 2. Acceptance back above LOY (single decisive close)
                # SAME bar acceptance allowed
                for j in range(i, search_end):
                    if closes[j] > loy:

                        # 3. No acceptance below LOY afterward
                        if j + 1 < n:
                            if np.all(closes[j + 1:] >= loy):
                                return True
                            else:
                                return False
                        else:
                            return True  # last bar was acceptance                        
                return False
        return False
    
    # Wyckoff-Aligned Failed Bull Auction (Single-Close Acceptance)
    # Acceptance on the SAME bar is invalid in Wyckoff, but OK here
    @staticmethod
    def is_failed_blbo(df: pd.DataFrame, max_accept_bars: int = 3) -> bool:
        df = df.copy()
        df["Timestamp"] = pd.to_datetime(df["Timestamp"])

        if df.empty:
            return False

        # --- Define current and previous day based on data ---
        last_ts = df["Timestamp"].max()
        current_day = last_ts.normalize()
        prev_ts = df[df["Timestamp"] < current_day]["Timestamp"].max()
        if pd.isna(prev_ts):
            return False 
        prev_day = prev_ts.normalize()
        if pd.isna(prev_day):
            return False  # no data for previous day

        # --- compute HOY ---
        df_prev_day = df[df["Timestamp"].dt.normalize() == prev_day]
        if df_prev_day.empty:
            return False

        hoy = df_prev_day["High"].max()

        # --- signal detection ---
        df_current_day = df[df["Timestamp"].dt.normalize() == current_day]
        if df_current_day.empty:
            return False

        df_current_day.sort_values("Timestamp", inplace=True)
        df_current_day.reset_index(drop=True, inplace=True)

        highs = df_current_day["High"].values
        closes = df_current_day["Close"].values
        n = len(df_current_day)

        for i in range(n):
            # 1. Bullish attempt: break above HOY
            if highs[i] > hoy:
                # Wyckoff: rejection must occur quickly
                search_end = min(i + max_accept_bars + 1, n)

                # 2. Acceptance back below HOY (single decisive close)
                # SAME bar failure allowed (close back below HOY)
                for j in range(i, search_end):
                    if closes[j] < hoy:

                        # 3. No acceptance below LOY afterward
                        if j + 1 < n:
                            if np.all(closes[j + 1:] <= hoy):
                                return True
                            else:
                                return False
                        else:
                            return True  # last bar was acceptance
                return False
        return False
