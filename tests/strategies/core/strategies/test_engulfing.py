import os
import pandas as pd
import pytest
from strategies.core.strategies.engulfing import EngulfingStrategy


def test_init_ValidConfig_SetsFolderPathCorrectly(mocker):
    """__init__(): Given valid config, should set folder_path properly."""
    # Arrange
    mock_load_config = mocker.patch(
        "strategies.core.strategies.engulfing.load_config",
        return_value={"data": {"directory": "/tmp/data"}}
    )
    interval = "1d"

    # Act
    strategy = EngulfingStrategy(interval)

    # Assert
    mock_load_config.assert_called_once()
    assert strategy.interval == interval
    assert strategy.folder_path == os.path.join("/tmp/data", interval)


@pytest.mark.parametrize(
    "files,expected_symbols,dfs,description",
    [
        # ✅ Bullish engulfing
        (
            ["1d-AAPL.csv"],
            ["AAPL"],
            {
                "1d-AAPL.csv": pd.DataFrame([
                    {"Open": 100, "Close": 90},
                    {"Open": 85, "Close": 105},
                ])
            },
            "Bullish engulfing"
        ),
        # ✅ Bearish engulfing
        (
            ["1d-MSFT.csv"],
            ["MSFT"],
            {
                "1d-MSFT.csv": pd.DataFrame([
                    {"Open": 100, "Close": 110},
                    {"Open": 112, "Close": 90},
                ])
            },
            "Bearish engulfing"
        ),
        # ⚙️ No engulfing pattern
        (
            ["1d-NO_ENGULF.csv"],
            [],
            {
                "1d-NO_ENGULF.csv": pd.DataFrame([
                    {"Open": 100, "Close": 101},
                    {"Open": 102, "Close": 103},
                ])
            },
            "No engulfing pattern"
        ),
        # ⚙️ Empty DataFrame
        (
            ["1d-EMPTY.csv"],
            [],
            {"1d-EMPTY.csv": pd.DataFrame()},
            "Empty CSV file"
        ),
    ],
)
def test_get_symbols_VariousPatterns_ReturnsExpectedSymbols(
    mocker, tmp_path, files, expected_symbols, dfs, description
):
    """get_symbols(): Given various file contents, should return only symbols with engulfing patterns."""
    # Arrange
    folder = tmp_path / "1d"
    folder.mkdir(parents=True, exist_ok=True)
    mocker.patch("strategies.core.strategies.engulfing.load_config", return_value={"data": {"directory": str(tmp_path)}})
    mocker.patch("os.listdir", return_value=files)
    mocker.patch("os.path.isfile", return_value=True)
    mocker.patch("pandas.read_csv", side_effect=lambda f: dfs[os.path.basename(f)])

    strategy = EngulfingStrategy("1d")

    # Act
    result = strategy.get_symbols()

    # Assert
    assert sorted(result) == sorted(expected_symbols), f"Failed case: {description}"


def test_get_symbols_FileNotCsvOrInvalidName_SkipsFile(mocker):
    """get_symbols(): Should skip non-matching file names."""
    # Arrange
    mocker.patch("strategies.core.strategies.engulfing.load_config", return_value={"data": {"directory": "/tmp"}})
    mocker.patch("os.listdir", return_value=["random.txt", "TSLA.csv", "1h-IBM.csv"])
    strategy = EngulfingStrategy("1d")

    # Act
    result = strategy.get_symbols()

    # Assert
    assert result == []


def test_get_symbols_FileDoesNotExist_SkipsFile(mocker):
    """get_symbols(): Should skip files that are not actual files."""
    # Arrange
    mocker.patch("strategies.core.strategies.engulfing.load_config", return_value={"data": {"directory": "/tmp"}})
    mocker.patch("os.listdir", return_value=["1d-TSLA.csv"])
    mocker.patch("os.path.isfile", return_value=False)
    strategy = EngulfingStrategy("1d")

    # Act
    result = strategy.get_symbols()

    # Assert
    assert result == []


def test_get_symbols_ReadCsvRaisesException_LogsWarningAndSkips(mocker):
    """get_symbols(): Should log warning and skip file if read_csv raises exception."""
    # Arrange
    mock_logger = mocker.patch("strategies.core.strategies.engulfing.logger")
    mocker.patch("strategies.core.strategies.engulfing.load_config", return_value={"data": {"directory": "/tmp"}})
    mocker.patch("os.listdir", return_value=["1d-FAIL.csv"])
    mocker.patch("os.path.isfile", return_value=True)
    mocker.patch("pandas.read_csv", side_effect=Exception("read error"))
    strategy = EngulfingStrategy("1d")

    # Act
    result = strategy.get_symbols()

    # Assert
    assert result == []
    mock_logger.warning.assert_called_once()
    mock_logger.warning.assert_called_with("Skipping 1d-FAIL.csv: read error")


def test_get_symbols_NoFilesFound_ReturnsEmptyList(mocker):
    """get_symbols(): Should return empty list if folder has no files."""
    # Arrange
    mocker.patch("strategies.core.strategies.engulfing.load_config", return_value={"data": {"directory": "/tmp"}})
    mocker.patch("os.listdir", return_value=[])
    strategy = EngulfingStrategy("1d")

    # Act
    result = strategy.get_symbols()

    # Assert
    assert result == []
    assert isinstance(result, list)


def test_get_symbols_LogsFinalSymbolCount(mocker):
    """get_symbols(): Should log total number of detected symbols."""
    # Arrange
    mock_logger = mocker.patch("strategies.core.strategies.engulfing.logger")
    mocker.patch("strategies.core.strategies.engulfing.load_config", return_value={"data": {"directory": "/tmp"}})
    mocker.patch("os.listdir", return_value=["1d-AAPL.csv"])
    mocker.patch("os.path.isfile", return_value=True)
    df = pd.DataFrame([
        {"Open": 100, "Close": 90},
        {"Open": 85, "Close": 105},
    ])
    mocker.patch("pandas.read_csv", return_value=df)

    strategy = EngulfingStrategy("1d")

    # Act
    result = strategy.get_symbols()

    # Assert
    assert result == ["AAPL"]
    mock_logger.info.assert_any_call("EngulfingStrategy: Found 1 symbols")
