import pytest
import pandas as pd
from datetime import datetime, timedelta
from strategies.core.strategies.failed_breakout_strategy import FailedBreakoutStrategy


# --- Helper to generate a 5-min dataframe ---
def make_5min_df(start: datetime, highs, lows, closes):
    timestamps = [start + timedelta(minutes=5*i) for i in range(len(closes))]
    data = {
        "Symbol": ["ABC"]*len(closes),
        "Interval": ["5min"]*len(closes),
        "Timestamp": timestamps,
        "Open": closes,  # dummy
        "High": highs,
        "Low": lows,
        "Close": closes,
        "Volume": [1000]*len(closes)
    }
    return pd.DataFrame(data)


# ================================
# Tests for _is_failed_brbo
# ================================
@pytest.mark.parametrize(
    "yesterday_lows,today_lows,today_closes,max_accept_bars,expected",
    [
        # Single-bar breakout and acceptance (SAME BAR)
        ([100, 101], [99, 102], [102, 103], 3, True),
        # Break below LOY but no close back above
        ([100], [99, 98, 97], [97, 98, 97], 3, False),
        # Break below LOY and acceptance delayed within max_accept_bars
        ([100], [99, 101, 102], [99, 101, 102], 3, True),
        # Break below LOY but acceptance occurs after max_accept_bars
        ([100], [99, 98, 97, 101], [99, 98, 97, 101], 2, False),
        # No break at all
        ([100], [101, 102, 103], [101, 102, 103], 3, False),
    ]
)
def test__is_failed_brbo_VariousScenarios_ReturnsExpected(
    yesterday_lows, today_lows, today_closes, max_accept_bars, expected
):
    # Arrange
    yesterday = pd.Timestamp.now().normalize() - pd.Timedelta(days=1)
    today = pd.Timestamp.now().normalize()
    df_yesterday = make_5min_df(
        start=yesterday,
        highs=[low+2 for low in yesterday_lows],
        lows=yesterday_lows,
        closes=[low+1 for low in yesterday_lows]
    )
    df_today = make_5min_df(
        start=today,
        highs=[max(low,close)+1 for low,close in zip(today_lows, today_closes)],
        lows=today_lows,
        closes=today_closes
    )
    df = pd.concat([df_yesterday, df_today], ignore_index=True)

    # Act
    result = FailedBreakoutStrategy.is_failed_brbo(df, max_accept_bars=max_accept_bars)

    # Assert
    assert result == expected


# ================================
# Tests for _is_failed_blbo
# ================================
@pytest.mark.parametrize(
    "yesterday_highs,today_highs,today_closes,max_accept_bars,expected",
    [
        # Single-bar bull breakout and failure (SAME BAR)
        ([100, 101], [103, 99], [99, 98], 3, True),
        # Break above HOY but no close back below
        ([100], [102, 103, 104], [103, 104, 105], 3, False),
        # Break above HOY and failure within max_accept_bars
        ([100], [102, 101, 99], [101, 99, 98], 3, True),
        # Break above HOY but failure delayed beyond max_accept_bars
        ([100], [102, 103, 104, 99], [103, 104, 105, 99], 2, False),
        # No break at all
        ([100], [99, 98, 97], [99, 98, 97], 3, False),
    ]
)
def test__is_failed_bullbo_VariousScenarios_ReturnsExpected(
    yesterday_highs, today_highs, today_closes, max_accept_bars, expected
):
    # Arrange
    yesterday = pd.Timestamp.now().normalize() - pd.Timedelta(days=1)
    today = pd.Timestamp.now().normalize()
    df_yesterday = make_5min_df(
        start=yesterday,
        highs=yesterday_highs,
        lows=[h-2 for h in yesterday_highs],
        closes=[h-1 for h in yesterday_highs]
    )
    df_today = make_5min_df(
        start=today,
        highs=today_highs,
        lows=[min(h,c)-1 for h,c in zip(today_highs, today_closes)],
        closes=today_closes
    )
    df = pd.concat([df_yesterday, df_today], ignore_index=True)

    # Act
    result = FailedBreakoutStrategy.is_failed_blbo(df, max_accept_bars=max_accept_bars)

    # Assert
    assert result == expected


# ======================
# Tests for is_failed_bo
# ======================
def test_is_failed_bo_BothBreakouts_ReturnsTrue():
    # Arrange
    yesterday = pd.Timestamp.now().normalize() - pd.Timedelta(days=1)
    today = pd.Timestamp.now().normalize()

    # Bear breakout
    df_yesterday = make_5min_df(
        start=yesterday,
        highs=[100], lows=[100], closes=[100]
    )
    df_today = make_5min_df(
        start=today,
        highs=[102, 103], lows=[99, 102], closes=[102, 103]
    )

    # Bull breakout
    df_today_bull = make_5min_df(
        start=today,
        highs=[103, 105], lows=[102, 101], closes=[101, 100]
    )

    df = pd.concat([df_yesterday, df_today, df_today_bull], ignore_index=True)

    # Act
    result = FailedBreakoutStrategy.is_failed_bo(df)

    # Assert
    assert result is True


def test_is_failed_bo_NoBreakouts_ReturnsFalse():
    # Arrange
    yesterday = pd.Timestamp.now().normalize() - pd.Timedelta(days=1)
    today = pd.Timestamp.now().normalize()

    df_yesterday = make_5min_df(
        start=yesterday,
        highs=[100, 101], lows=[99, 100], closes=[100, 101]
    )
    df_today = make_5min_df(
        start=today,
        highs=[101, 102], lows=[100, 101], closes=[101, 102]
    )

    df = pd.concat([df_yesterday, df_today], ignore_index=True)

    # Act
    result = FailedBreakoutStrategy.is_failed_bo(df)

    # Assert
    assert result is False


# ================================
# Edge Cases
# ================================
def test__is_failed_brbo_EmptyDF_ReturnsFalse():
    # Arrange
    df = pd.DataFrame(columns=["Symbol","Interval","Timestamp","Open","High","Low","Close","Volume"])

    # Act
    result = FailedBreakoutStrategy.is_failed_brbo(df)

    # Assert
    assert result is False


def test__is_failed_bullbo_EmptyDF_ReturnsFalse():
    # Arrange
    df = pd.DataFrame(columns=["Symbol","Interval","Timestamp","Open","High","Low","Close","Volume"])

    # Act
    result = FailedBreakoutStrategy.is_failed_blbo(df)

    # Assert
    assert result is False


def test__is_failed_brbo_NoYesterday_ReturnsFalse():
    # Arrange
    today = pd.Timestamp.now().normalize()
    df_today = make_5min_df(
        start=today,
        highs=[102, 103],
        lows=[101, 102],
        closes=[102, 103]
    )

    # Act
    result = FailedBreakoutStrategy.is_failed_brbo(df_today)

    # Assert
    assert result is False


def test__is_failed_bullbo_NoYesterday_ReturnsFalse():
    # Arrange
    today = pd.Timestamp.now().normalize()
    df_today = make_5min_df(
        start=today,
        highs=[102, 103],
        lows=[101, 102],
        closes=[102, 103]
    )

    # Act
    result = FailedBreakoutStrategy.is_failed_blbo(df_today)

    # Assert
    assert result is False
