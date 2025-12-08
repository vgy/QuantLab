import pytest
import numpy as np
import pandas as pd

from strategies.core.strategies.rsi_strategy import RsiStrategy

@pytest.fixture
def df_base():
    """Base DataFrame with enough rows."""
    return pd.DataFrame({"Close": np.linspace(100, 110, 50)})

def test_is_rsi_overbought_InsufficientData_ReturnsFalse(df_base):
    # Arrange
    df = df_base.iloc[:5]  # less than period=14

    # Act
    result = RsiStrategy.is_rsi_overbought(df)

    # Assert
    assert result is False

def test_is_rsi_overbought_RsiOverThreshold_ReturnsTrue(mocker, df_base):
    # Arrange
    rsi_values = np.array([50] * 40 + [80] * 10)  # last values >=70
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)

    # Act
    result = RsiStrategy.is_rsi_overbought(df_base, overbought=70, duration=10)

    # Assert
    assert result

def test_is_rsi_overbought_RsiNotOverThreshold_ReturnsFalse(mocker, df_base):
    # Arrange
    rsi_values = np.array([50] * 50)
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)

    # Act
    result = RsiStrategy.is_rsi_overbought(df_base)

    # Assert
    assert not result

def test_is_rsi_overbought_DurationClipped_ReturnsTrue(mocker, df_base):
    # Arrange
    rsi_values = np.array([80] * 50)
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)

    # Act
    result = RsiStrategy.is_rsi_overbought(df_base, duration=9)

    # Assert
    assert result

def test_is_rsi_oversold_InsufficientData_ReturnsFalse(df_base):
    # Arrange
    df = df_base.iloc[:5]

    # Act
    result = RsiStrategy.is_rsi_oversold(df)

    # Assert
    assert result is False

def test_is_rsi_oversold_RsiBelowThreshold_ReturnsTrue(mocker, df_base):
    # Arrange
    rsi_values = np.array([40] * 40 + [20] * 10)
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)

    # Act
    result = RsiStrategy.is_rsi_oversold(df_base, oversold=30, duration=10)

    # Assert
    assert result

def test_is_rsi_oversold_RsiNotBelowThreshold_ReturnsFalse(mocker, df_base):
    # Arrange
    rsi_values = np.array([40] * 50)
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)

    # Act
    result = RsiStrategy.is_rsi_oversold(df_base)

    # Assert
    assert not result

def test_is_rsi_bullish_divergence_InsufficientData_ReturnsFalse(df_base):
    # Arrange
    df = df_base.iloc[:5]

    # Act
    result = RsiStrategy.is_rsi_bullish_divergence(df)

    # Assert
    assert result is False

def test_is_rsi_bullish_divergence_NoPivotLows_ReturnsFalse(mocker, df_base):
    # Arrange
    rsi_values = pd.Series(np.linspace(40, 60, len(df_base)))
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)
    mocker.patch("strategies.core.strategies.rsi_strategy.argrelextrema", return_value=(np.array([]),))

    # Act
    result = RsiStrategy.is_rsi_bullish_divergence(df_base)

    # Assert
    assert result is False

def test_is_rsi_bullish_divergence_OnePivotLow_ReturnsFalse(mocker, df_base):
    # Arrange
    rsi_values = pd.Series(np.linspace(40, 60, len(df_base)))
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)
    mocker.patch("strategies.core.strategies.rsi_strategy.argrelextrema", return_value=(np.array([5]),))

    # Act
    result = RsiStrategy.is_rsi_bullish_divergence(df_base)

    # Assert
    assert result is False

def test_is_rsi_bullish_divergence_ValidDivergence_ReturnsTrue(mocker):
    # Arrange
    prices = np.array([100, 95, 98, 90, 92, 93])  # pivot lows at 95 (i=1) and 90 (i=3)
    df = pd.DataFrame({"Close": prices})

    rsi_values = pd.Series(np.array([40, 30, 35, 35, 38, 40]))  # RSI low1=30, low2=35 (higher)
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)

    mocker.patch("strategies.core.strategies.rsi_strategy.argrelextrema",
                 return_value=(np.array([1, 3]),))  # pivot lows

    # Act
    result = RsiStrategy.is_rsi_bullish_divergence(df, period=6, duration=6)

    # Assert
    assert result is True

def test_is_rsi_bullish_divergence_NoDivergence_ReturnsFalse(mocker):
    # Arrange
    prices = np.array([100, 95, 98, 90, 92, 93])
    df = pd.DataFrame({"Close": prices})

    # RSI makes lower low instead of higher low
    rsi_values = np.array([40, 30, 35, 20, 25, 30])
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)

    mocker.patch("strategies.core.strategies.rsi_strategy.argrelextrema",
                 return_value=(np.array([1, 3]),))

    # Act
    result = RsiStrategy.is_rsi_bullish_divergence(df, duration=6)

    # Assert
    assert result is False

def test_is_rsi_bearish_divergence_InsufficientData_ReturnsFalse(df_base):
    # Arrange
    df = df_base.iloc[:5]

    # Act
    result = RsiStrategy.is_rsi_bearish_divergence(df)

    # Assert
    assert result is False

def test_is_rsi_bearish_divergence_NoPivotHighs_ReturnsFalse(mocker, df_base):
    # Arrange
    rsi_values = pd.Series(np.linspace(50, 60, len(df_base)))
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)
    mocker.patch("strategies.core.strategies.rsi_strategy.argrelextrema", return_value=(np.array([]),))

    # Act
    result = RsiStrategy.is_rsi_bearish_divergence(df_base)

    # Assert
    assert result is False

def test_is_rsi_bearish_divergence_OnePivotHigh_ReturnsFalse(mocker, df_base):
    # Arrange
    rsi_values = pd.Series(np.linspace(50, 60, len(df_base)))
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)
    mocker.patch("strategies.core.strategies.rsi_strategy.argrelextrema", return_value=(np.array([5]),))

    # Act
    result = RsiStrategy.is_rsi_bearish_divergence(df_base)

    # Assert
    assert result is False

def test_is_rsi_bearish_divergence_ValidDivergence_ReturnsTrue(mocker):
    # Arrange
    prices = np.array([100, 105, 102, 110, 108, 107])  # pivot highs at 105 (i=1), 110 (i=3)
    df = pd.DataFrame({"Close": prices})

    rsi_values = pd.Series(np.array([60, 70, 65, 65, 62, 60]))  # RSI high1=70, high2=65 (lower)
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)

    mocker.patch("strategies.core.strategies.rsi_strategy.argrelextrema",
                 return_value=(np.array([1, 3]),))  # pivot highs

    # Act
    result = RsiStrategy.is_rsi_bearish_divergence(df, period=6, duration=6)

    # Assert
    assert result is True

def test_is_rsi_bearish_divergence_NoDivergence_ReturnsFalse(mocker):
    # Arrange
    prices = np.array([100, 105, 102, 110, 108, 107])
    df = pd.DataFrame({"Close": prices})

    # RSI makes higher high instead of lower → not bearish divergence
    rsi_values = np.array([60, 70, 65, 75, 74, 72])
    mocker.patch("strategies.core.strategies.rsi_strategy.ta.RSI", return_value=rsi_values)

    mocker.patch("strategies.core.strategies.rsi_strategy.argrelextrema",
                 return_value=(np.array([1, 3]),))

    # Act
    result = RsiStrategy.is_rsi_bearish_divergence(df, duration=6)

    # Assert
    assert result is False


