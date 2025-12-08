import pytest
import numpy as np
import pandas as pd
from unittest.mock import MagicMock
from strategies.core.strategies.candlestick_patterns_strategy import CandlestickPatternsStrategy
import talib

@pytest.fixture
def sample_df():
    """Small OHLC dataframe for testing."""
    size = 20
    return pd.DataFrame({
        "Open": np.random.rand(size),
        "High": np.random.rand(size) + 1,
        "Low": np.random.rand(size),
        "Close": np.random.rand(size)
    })


def test_contains_candlestick_pattern_NoPatternsProvided_RaisesKeyError(mocker, sample_df):
    # Arrange
    mocker.patch("strategies.core.strategies.candlestick_patterns_strategy.get_patterns", return_value=[])

    # Act / Assert
    with pytest.raises(KeyError):
        CandlestickPatternsStrategy.contains_candlestick_pattern(
            df=sample_df, group="bullish", subgroup="sub", pattern="nonexistent"
        )


def test_contains_candlestick_pattern_BullishSignalPresent_ReturnsTrue(mocker, sample_df):
    # Arrange
    mocker.patch("strategies.core.strategies.candlestick_patterns_strategy.get_patterns", return_value=["cdlhammer"])
    fake_result = np.array([0] * 15 + [100])  # bullish last values

    mock_func = MagicMock(return_value=fake_result)
    mocker.patch.object(talib, "CDLHAMMER", mock_func)

    # Act
    result = CandlestickPatternsStrategy.contains_candlestick_pattern(
        sample_df, "bullish", "sub", "hammer", duration=10
    )

    # Assert
    assert result is True


def test_contains_candlestick_pattern_BearishSignalPresent_ReturnsTrue(mocker, sample_df):
    # Arrange
    mocker.patch("strategies.core.strategies.candlestick_patterns_strategy.get_patterns", return_value=["cdlshootingstar"])
    fake_result = np.array([0] * 15 + [-50])  # bearish

    mocker.patch.object(talib, "CDLSHOOTINGSTAR", MagicMock(return_value=fake_result))

    # Act
    result = CandlestickPatternsStrategy.contains_candlestick_pattern(
        sample_df, "bearish", "sub", "shootingstar", duration=10
    )

    # Assert
    assert result is True


@pytest.mark.parametrize("group", ["neutral", "all"])
def test_contains_candlestick_pattern_NeutralOrAllGroup_ReturnsTrue(mocker, sample_df, group):
    # Arrange
    mocker.patch("strategies.core.strategies.candlestick_patterns_strategy.get_patterns", return_value=["cdldoji"])
    fake_result = np.array([0] * 10 + [1])  # non-zero → detected for neutral/all

    mocker.patch.object(talib, "CDLDOJI", MagicMock(return_value=fake_result))

    # Act
    result = CandlestickPatternsStrategy.contains_candlestick_pattern(
        sample_df, group, "sub", "doji", duration=5
    )

    # Assert
    assert result is True


def test_contains_candlestick_pattern_NoMatchingPattern_ReturnsFalse(mocker, sample_df):
    # Arrange
    mocker.patch("strategies.core.strategies.candlestick_patterns_strategy.get_patterns", return_value=["cdlhammer"])
    fake_result = np.zeros(20)  # no signal

    mocker.patch.object(talib, "CDLHAMMER", MagicMock(return_value=fake_result))

    # Act
    result = CandlestickPatternsStrategy.contains_candlestick_pattern(
        sample_df, "bullish", "sub", "hammer"
    )

    # Assert
    assert result is False


def test_contains_candlestick_pattern_ResultTooShort_ReturnsFalse(mocker, sample_df):
    # Arrange
    mocker.patch("strategies.core.strategies.candlestick_patterns_strategy.get_patterns", return_value=["cdlhammer"])
    fake_result = np.array([1, -1])  # shorter than duration

    mocker.patch.object(talib, "CDLHAMMER", MagicMock(return_value=fake_result))

    # Act
    result = CandlestickPatternsStrategy.contains_candlestick_pattern(
        sample_df, "bullish", "sub", "hammer", duration=10
    )

    # Assert
    assert result is False

def test_contains_candlestick_pattern_TalibError_ReturnsFalse(mocker, sample_df, caplog):
    # Arrange
    mocker.patch("strategies.core.strategies.candlestick_patterns_strategy.get_patterns", return_value=["cdlhammer"])

    def raise_error(*args, **kwargs):
        raise ValueError("TALIB failed")

    mocker.patch.object(talib, "CDLHAMMER", raise_error)

    # Act
    with caplog.at_level("WARNING"):
        result = CandlestickPatternsStrategy.contains_candlestick_pattern(
            sample_df, "bullish", "sub", "hammer"
        )

    # Assert
    assert result is False


def test_contains_candlestick_pattern_MultiplePatternsOneMatches_ReturnsTrue(mocker, sample_df):
    # Arrange
    mocker.patch("strategies.core.strategies.candlestick_patterns_strategy.get_patterns",
                 return_value=["cdlhammer", "cdldoji"])

    # first pattern returns no signal
    fake_result1 = np.zeros(20)

    # second pattern returns signal
    fake_result2 = np.array([0] * 18 + [5])

    mocker.patch.object(talib, "CDLHAMMER", MagicMock(return_value=fake_result1))
    mocker.patch.object(talib, "CDLDOJI", MagicMock(return_value=fake_result2))

    # Act
    result = CandlestickPatternsStrategy.contains_candlestick_pattern(
        sample_df, "neutral", "sub", "mixed"
    )

    # Assert
    assert result is True
