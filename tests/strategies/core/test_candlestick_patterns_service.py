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
