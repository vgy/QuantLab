import pytest
import numpy as np
import pandas as pd
from strategies.core.strategies.macd_strategy import MacdStrategy

@pytest.fixture
def df_default():
    """Default OHLC-like DataFrame with enough rows."""
    values = np.random.rand(50)
    return pd.DataFrame({"Close": values})

def test_is_bullish_macd_crossover_InsufficientData_ReturnsFalse(df_default, mocker):
    # Arrange
    df_small = df_default.iloc[:10]  # slow=26 requires >26 rows

    # Act
    result = MacdStrategy.is_bullish_macd_crossover(df_small)

    # Assert
    assert result is False

@pytest.mark.parametrize(
    "macd_vals, signal_vals, expected",
    [
        # Bullish crossover at last step: macd crosses above signal
        ([0, 1, 1, 4], [0, 1, 2, 3], True),

        # No crossover
        ([0, 1, 2, 3], [0, 1, 2, 3], False),

        # Cross earlier but not in last duration window
        ([0, -1, 2, 3], [0, -1, 3, 4], False),
    ]
)
def test_is_bullish_macd_crossover_VariousOutcomes_ReturnsExpected(
    df_default, mocker, macd_vals, signal_vals, expected
):
    # Arrange
    df = df_default.iloc[:len(macd_vals)]
    macd_vals = np.array(macd_vals, dtype=float)
    signal_vals = np.array(signal_vals, dtype=float)

    mocker.patch(
        "strategies.core.strategies.macd_strategy.ta.MACD",
        return_value=(macd_vals, signal_vals, np.zeros_like(macd_vals))
    )

    # Act
    result = MacdStrategy.is_bullish_macd_crossover(df, slow=3, duration=4)

    # Assert
    assert result == expected

def test_is_bearish_macd_crossover_InsufficientData_ReturnsFalse(df_default):
    # Arrange
    df_small = df_default.iloc[:5]  # less than slow=26

    # Act
    result = MacdStrategy.is_bearish_macd_crossover(df_small)

    # Assert
    assert result is False

@pytest.mark.parametrize(
    "macd_vals, signal_vals, expected",
    [
        # Bearish crossover: MACD crosses below signal
        ([3, 2, 2, 0], [3, 2, 1, 1], True),

        # No crossover
        ([1, 2, 3], [1, 2, 3], False),

        # Cross earlier but outside last duration
        ([3, 0, -1], [3, 1, -2], False),
    ]
)
def test_is_bearish_macd_crossover_VariousOutcomes_ReturnsExpected(
    df_default, mocker, macd_vals, signal_vals, expected
):
    # Arrange
    df = df_default.iloc[:len(macd_vals)]
    macd_vals = np.array(macd_vals, dtype=float)
    signal_vals = np.array(signal_vals, dtype=float)

    mocker.patch(
        "strategies.core.strategies.macd_strategy.ta.MACD",
        return_value=(macd_vals, signal_vals, np.zeros_like(macd_vals))
    )

    # Act
    result = MacdStrategy.is_bearish_macd_crossover(df, slow=3, duration=4)

    # Assert
    assert result == expected


def test_is_bearish_macd_crossover_MacdContainsNaN_ReturnsFalse(df_default, mocker):
    # Arrange
    macd_vals = np.array([np.nan, np.nan, np.nan])
    signal_vals = np.array([np.nan, np.nan, np.nan])

    mocker.patch(
        "strategies.core.strategies.macd_strategy.ta.MACD",
        return_value=(macd_vals, signal_vals, np.zeros_like(macd_vals))
    )

    df = df_default.iloc[:3]

    # Act
    result = MacdStrategy.is_bearish_macd_crossover(df)

    # Assert
    assert result is False

def test_is_bearish_macd_crossover_MacdError_Raises(df_default, mocker):
    # Arrange
    def fail(*_, **__):
        raise RuntimeError("MACD calculation failed")

    mocker.patch("strategies.core.strategies.macd_strategy.ta.MACD", fail)

    df = df_default.iloc[:30]

    # Act / Assert
    with pytest.raises(RuntimeError):
        MacdStrategy.is_bearish_macd_crossover(df)
