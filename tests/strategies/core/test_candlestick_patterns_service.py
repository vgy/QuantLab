import numpy as np
import pandas as pd
import pytest
from unittest.mock import patch, MagicMock
from strategies.core.candlestick_patterns_service import CandlestickPatternsService
from strategies.constants.candlestick_patterns import reverse_lookup


# ---------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------
@pytest.fixture
def mock_config(mocker, tmp_path):
    """Mock load_config() to control directory behavior."""
    mock_dir = tmp_path / "data"
    mock_dir.mkdir()
    mocker.patch(
        "strategies.core.candlestick_patterns_service.load_config",
        return_value={"data": {"directory": str(mock_dir)}}
    )
    return mock_dir


@pytest.fixture
def service(mock_config):
    """Return a fresh CandlestickPatternsService."""
    return CandlestickPatternsService()

@pytest.fixture
def mock_patterns(mocker):
    """Mock get_patterns()."""
    return mocker.patch("strategies.core.candlestick_patterns_service.get_patterns")


@pytest.fixture
def mock_logger(mocker):
    return mocker.patch("strategies.core.candlestick_patterns_service.logger")

@pytest.fixture
def mock_talib(mocker):
    """Mock the talib module."""
    return mocker.patch("strategies.core.candlestick_patterns_service.talib")

# ---------------------------------------------------------------------
# Tests for get_symbols_for_pattern_and_interval
# ---------------------------------------------------------------------
def test_get_symbols_for_pattern_and_interval_InvalidPattern_RaisesKeyError(service):
    # Arrange
    group = "bullish"
    subgroup = "reversal"
    pattern = "NOT_IN_REVERSE_LOOKUP"
    interval = "1h"
    period = 3

    # Act / Assert
    with pytest.raises(KeyError):
        service.get_symbols_for_pattern_and_interval(group, subgroup, pattern, interval, period)


@pytest.mark.parametrize(
    "values,period,expected",
    [
        ([0, 0, 0, 100], 1, True),
        ([0, 0, 0, 0], 3, False),
        ([0, -100, 0, 0], 2, False),
        ([0, -100, 0, 100], 3, True),
    ]
)
def test_get_symbols_for_pattern_and_interval_TalibSignalProcessing_WorksCorrectly(
    service, mocker, mock_config, values, period, expected
):
    # Arrange
    group = "bullish"
    subgroup = "reversal"
    pattern = next(iter(reverse_lookup.keys()))
    interval = "1h"
    folder = mock_config / interval
    folder.mkdir()

    # Create one valid CSV file
    file_path = folder / f"{interval}-BTC.csv"
    df = pd.DataFrame({
        "Open": [1, 2, 3, 4],
        "High": [2, 3, 4, 5],
        "Low": [1, 2, 3, 4],
        "Close": [2, 3, 4, 5],
    })
    df.to_csv(file_path, index=False)

    # Mock talib
    talib_mock = mocker.patch("strategies.core.candlestick_patterns_service.talib")
    func_mock = MagicMock(return_value=np.array(values))
    setattr(talib_mock, pattern.upper(), func_mock)

    # Act
    result = service.get_symbols_for_pattern_and_interval(group, subgroup, pattern, interval, period)

    # Assert
    if expected:
        assert result == ["BTC"]
    else:
        assert result == []


def test_get_symbols_for_pattern_and_interval_EmptyCsvFile_SkipsFile(service, mocker, mock_config):
    # Arrange
    group = "bullish"
    subgroup = "reversal"
    pattern = next(iter(reverse_lookup.keys()))
    interval = "1h"
    folder = mock_config / interval
    folder.mkdir()

    file_path = folder / f"{interval}-ETH.csv"
    pd.DataFrame().to_csv(file_path, index=False)

    # Mock talib function
    talib_mock = mocker.patch("strategies.core.candlestick_patterns_service.talib")
    func_mock = MagicMock(return_value=np.array([0, 0, 0]))
    setattr(talib_mock, pattern, func_mock)

    # Act
    result = service.get_symbols_for_pattern_and_interval(group, subgroup, pattern, interval, 2)

    # Assert
    assert result == []


def test_get_symbols_for_pattern_and_interval_MissingColumns_SkipsFile(service, mocker, mock_config):
    # Arrange
    group = "bullish"
    subgroup = "reversal"
    pattern = next(iter(reverse_lookup.keys()))
    interval = "1h"
    folder = mock_config / interval
    folder.mkdir()

    file_path = folder / f"{interval}-XRP.csv"
    pd.DataFrame({"Close": [1, 2, 3]}).to_csv(file_path, index=False)

    # Mock talib
    talib_mock = mocker.patch("strategies.core.candlestick_patterns_service.talib")
    func_mock = MagicMock(return_value=np.array([0, 0, 0]))
    setattr(talib_mock, pattern, func_mock)

    # Act
    result = service.get_symbols_for_pattern_and_interval(group, subgroup, pattern, interval, 2)

    # Assert
    assert result == []


def test_get_symbols_for_pattern_and_interval_CsvThrowsException_SkipsFile(service, mocker, mock_config):
    # Arrange
    group = "bullish"
    subgroup = "reversal"
    pattern = next(iter(reverse_lookup.keys()))
    interval = "1h"
    folder = mock_config / interval
    folder.mkdir()

    file_path = folder / f"{interval}-SOL.csv"
    file_path.write_text("corrupted file content")

    # Force pd.read_csv to raise
    mocker.patch("pandas.read_csv", side_effect=Exception("CSV broken"))

    # Mock talib
    talib_mock = mocker.patch("strategies.core.candlestick_patterns_service.talib")
    func_mock = MagicMock(return_value=np.array([100, 0, 0]))
    setattr(talib_mock, pattern.upper(), func_mock)

    # Act
    result = service.get_symbols_for_pattern_and_interval(group, subgroup, pattern, interval, 1)

    # Assert
    assert result == []


def test_get_symbols_for_pattern_and_interval_MultipleFiles_CorrectSymbolExtraction(
    service, mocker, mock_config
):
    # Arrange
    group = "bullish"
    subgroup = "reversal"
    pattern = next(iter(reverse_lookup.keys()))
    interval = "1h"
    folder = mock_config / interval
    folder.mkdir()

    df = pd.DataFrame({
        "Open": [1, 2],
        "High": [2, 3],
        "Low": [1, 2],
        "Close": [2, 3]
    })

    file_a = folder / f"{interval}-BTC.csv"
    file_b = folder / f"{interval}-ADA.csv"
    df.to_csv(file_a, index=False)
    df.to_csv(file_b, index=False)

    # Mock talib so both files have signals
    talib_mock = mocker.patch("strategies.core.candlestick_patterns_service.talib")
    func_mock = MagicMock(return_value=np.array([0, 100]))
    setattr(talib_mock, pattern.upper(), func_mock)

    # Act
    result = service.get_symbols_for_pattern_and_interval(group, subgroup, pattern, interval, 1)

    # Assert
    assert set(result) == {"BTC", "ADA"}

def test_get_candlestick_patterns_for_symbol_interval_period_NoPatterns_KeyError(service, mock_patterns):
    # Arrange
    mock_patterns.return_value = []

    # Act / Assert
    with pytest.raises(KeyError):
        service.get_candlestick_patterns_for_symbol_interval_period("BTC", "1h", 5)

def test_get_candlestick_patterns_for_symbol_interval_period_FileMissing_ReturnsEmpty(service, mock_patterns, mock_logger):
    # Arrange
    mock_patterns.return_value = ["2025-11-04 09:15:00 - cdlengulfing"]

    # Act
    output = service.get_candlestick_patterns_for_symbol_interval_period("BTC", "1h", 5)

    # Assert
    assert output == []
    mock_logger.error.assert_called()


def test_get_candlestick_patterns_for_symbol_interval_period_EmptyFile_ReturnsEmpty(service, mock_patterns, mock_logger, tmp_path):
    # Arrange
    mock_patterns.return_value = ["2025-11-04 09:15:00 - cdlengulfing"]

    folder = tmp_path / "1h"
    folder.mkdir()
    file_path = folder / "1h-BTC.csv"
    file_path.write_text("")  # empty file

    # Act
    output = service.get_candlestick_patterns_for_symbol_interval_period("BTC", "1h", 5)

    # Assert
    assert output == []
    mock_logger.error.assert_called()


@pytest.mark.parametrize(
    "missing_col",
    ["Timestamp", "Open", "High", "Low", "Close"]
)
def test_get_candlestick_patterns_for_symbol_interval_period_MissingColumns_ReturnsEmpty(
    service, mock_patterns, mock_logger, tmp_path, missing_col
):
    # Arrange
    required_cols = ["Timestamp", "Open", "High", "Low", "Close"]
    mock_patterns.return_value = ["2025-11-04 09:15:00 - cdlengulfing"]

    folder = tmp_path / "1h"
    folder.mkdir()

    df = pd.DataFrame({
        col: [1, 2, 3] for col in required_cols if col != missing_col
    })

    file_path = folder / "1h-BTC.csv"
    df.to_csv(file_path, index=False)

    # Act
    output = service.get_candlestick_patterns_for_symbol_interval_period("BTC", "1h", 5)

    # Assert
    assert output == []
    mock_logger.error.assert_called()


def test_get_candlestick_patterns_for_symbol_interval_period_TalibError_ReturnsEmpty(
    service, mock_patterns, mock_talib, mock_logger, tmp_path
):
    # Arrange
    required_cols = ["Timestamp", "Open", "High", "Low", "Close"]
    mock_patterns.return_value = ["2025-11-04 09:15:00 - cdlengulfing"]
    folder = tmp_path / "1h"
    folder.mkdir()

    df = pd.DataFrame({
        col: [1, 2, 3, 4, 5] for col in required_cols
    })
    file_path = folder / "1h-BTC.csv"
    df.to_csv(file_path, index=False)

    mock_talib.CDLENGULFING.side_effect = Exception("TA-Lib error")

    # Act
    output = service.get_candlestick_patterns_for_symbol_interval_period("BTC", "1h", 5)

    # Assert
    assert output == []
    mock_logger.error.assert_called()


def test_get_candlestick_patterns_for_symbol_interval_period_PatternFound_ReturnsMatches(
    service, mock_patterns, mock_talib, mock_config, mock_logger
):
    # Arrange
    ptrn = "CDLENGULFING"
    mock_patterns.return_value = [ptrn]

    folder = mock_config / "1h"
    folder.mkdir()

    df = pd.DataFrame({
        "Timestamp": ["t1", "t2", "t3", "t4", "t5"],
        "Open": [1, 2, 3, 4, 5],
        "High": [2, 3, 4, 5, 6],
        "Low": [0, 1, 2, 3, 4],
        "Close": [1.5, 2.5, 3.5, 4.5, 5.5],
    })

    file_path = folder / "1h-BTC.csv"
    df.to_csv(file_path, index=False)

    mock_talib.CDLENGULFING.return_value = pd.Series([0, 0, 100, 0, -100])

    # Act
    result = service.get_candlestick_patterns_for_symbol_interval_period("BTC", "1h", 5)

    # Assert
    assert len(result) == 2
    assert "t3 - CDLENGULFING" in result
    assert "t5 - CDLENGULFING" in result
    assert result == sorted(result)
    mock_logger.info.assert_called()


def test_get_candlestick_patterns_for_symbol_interval_period_PatternOutsidePeriod_NoMatches(
    service, mock_patterns, mock_talib, mock_config, mock_logger
):
    # Arrange
    ptrn = "CDLENGULFING"
    mock_patterns.return_value = [ptrn]

    folder = mock_config / "1h"
    folder.mkdir()

    df = pd.DataFrame({
        "Timestamp": ["t1", "t2", "t3", "t4", "t5"],
        "Open": [1, 2, 3, 4, 5],
        "High": [2, 3, 4, 5, 6],
        "Low": [0, 1, 2, 3, 4],
        "Close": [1, 2, 3, 4, 5],
    })

    file_path = folder / "1h-BTC.csv"
    df.to_csv(file_path, index=False)

    mock_talib.CDLENGULFING.return_value = pd.Series([100, 0, 0, 0, 0])

    # Act
    result = service.get_candlestick_patterns_for_symbol_interval_period("BTC", "1h", 4)

    # Assert
    assert result == []
    mock_logger.info.assert_called()


def test_get_candlestick_patterns_for_symbol_interval_period_MultiplePatterns_MultipleMatches(
    service, mock_patterns, mock_talib, mock_config, mock_logger
):
    # Arrange
    mock_patterns.return_value = ["CDLDOJI", "CDLENGULFING"]

    folder = mock_config / "1h"
    folder.mkdir()

    df = pd.DataFrame({
        "Timestamp": ["t1", "t2", "t3", "t4"],
        "Open": [1, 2, 3, 4],
        "High": [2, 3, 4, 5],
        "Low": [0, 1, 2, 3],
        "Close": [1.5, 2.5, 3.5, 4.5],
    })

    file_path = folder / "1h-BTC.csv"
    df.to_csv(file_path, index=False)

    mock_talib.CDLDOJI.return_value = pd.Series([0, 0, 10, 0])
    mock_talib.CDLENGULFING.return_value = pd.Series([0, -20, 0, 0])

    # Act
    result = service.get_candlestick_patterns_for_symbol_interval_period("BTC", "1h", 3)

    # Assert
    assert "t3 - CDLDOJI" in result
    assert "t2 - CDLENGULFING" in result
    assert len(result) == 2
    assert result == sorted(result)
    mock_logger.info.assert_called()
