import pandas as pd
import pytest
from unittest.mock import MagicMock
from strategies.core.strategy_pipeline import StrategyPipeline

@pytest.fixture
def mock_config(mocker, tmp_path):
    config = {"data": {"directory": str(tmp_path)}}
    mocker.patch("strategies.core.strategy_pipeline.load_config", return_value=config)
    return tmp_path


@pytest.fixture
def create_csv(mock_config):
    def _create(interval, symbol, df=None):
        folder = mock_config / interval
        folder.mkdir(exist_ok=True)
        file_path = folder / f"{interval}-{symbol}.csv"

        if df is None:
            df = pd.DataFrame({"close": [1, 2, 3]})

        df.to_csv(file_path, index=False)
        return file_path
    return _create

class DummyItem:
    def __init__(self, strategy, interval, params=None):
        self.strategy = strategy
        self.interval = interval
        self.params = params

def test___init___ValidDirectory_FindsSymbols(mock_config, create_csv):
    # Arrange
    create_csv("1d", "AAPL")
    create_csv("1d", "TSLA")
    create_csv("1d", "XYZ")

    # Bad formats
    (mock_config / "1d" / "1dAAPL.csv").touch()
    (mock_config / "1d" / "foo-TSLA.csv").touch()

    # Act
    pipeline = StrategyPipeline()

    # Assert
    assert set(pipeline.symbols) == {"AAPL", "TSLA", "XYZ"}

def test_load_data_FileExists_ReturnsDataFrame(create_csv):
    # Arrange
    create_csv("1d", "AAPL")
    create_csv("1h", "AAPL")
    pipeline = StrategyPipeline()

    # Act
    df = pipeline.load_data("AAPL", "1h")

    # Assert
    assert isinstance(df, pd.DataFrame)
    assert not df.empty


def test_load_data_FileMissing_ReturnsNone(create_csv):
    # Arrange
    create_csv("1d", "AAPL")
    pipeline = StrategyPipeline()

    # Act
    df = pipeline.load_data("AAPL", "1h")

    # Assert
    assert df is None

@pytest.mark.parametrize(
    "side_effects, expected",
    [
        ([True, True], ["AAPL"]),
        ([True, False], []),
        ([False, True], []),
        ([False, False], []),
        ([False], []),
    ]
)
def test_run_pipeline_StrategyResults_ControlFlow(
    create_csv, mocker, side_effects, expected
):
    # Arrange
    create_csv("1d", "AAPL")
    create_csv("1h", "AAPL")
    pipeline = StrategyPipeline()
    mock_strategy = MagicMock(side_effect=side_effects)
    mocker.patch.dict(StrategyPipeline.STRATEGY_MAP, {"s1": mock_strategy})
    items = [DummyItem("s1", "1h") for _ in side_effects]

    # Act
    result = pipeline.run_pipeline(items)

    # Assert
    assert result == expected


def test_run_pipeline_IntervalChange_LoadsNewDf(create_csv, mocker):
    # Arrange
    create_csv("1d", "AAPL")
    create_csv("1h", "AAPL")
    create_csv("5min", "AAPL")
    pipeline = StrategyPipeline()
    mock1 = MagicMock(return_value=True)
    mock2 = MagicMock(return_value=True)
    mocker.patch.dict(
        StrategyPipeline.STRATEGY_MAP,
        {"s1": mock1, "s2": mock2}
    )
    items = [
        DummyItem("s1", "1h"),
        DummyItem("s2", "5min"),
    ]

    # Act
    pipeline.run_pipeline(items)

    # Assert
    assert mock1.call_count == 1
    assert mock2.call_count == 1


def test_run_pipeline_DataMissing_SkipsSymbol(create_csv, mocker):
    # Arrange
    create_csv("1d", "AAPL")
    pipeline = StrategyPipeline()
    mocker.patch.dict(
        StrategyPipeline.STRATEGY_MAP,
        {"s1": MagicMock(return_value=True)}
    )
    items = [DummyItem("s1", "1h")]

    # Act
    result = pipeline.run_pipeline(items)

    # Assert
    assert result == []


def test_run_pipeline_StrategyError_SkipsSymbol(create_csv, mocker):
    # Arrange
    create_csv("1d", "AAPL")
    create_csv("1h", "AAPL")
    pipeline = StrategyPipeline()
    mocker.patch.dict(
        StrategyPipeline.STRATEGY_MAP,
        {"s1": MagicMock(side_effect=Exception("boom"))}
    )

    items = [DummyItem("s1", "1h")]

    # Act
    result = pipeline.run_pipeline(items)

    # Assert
    assert result == []


def test_run_pipeline_ParamsPassedCorrectly(create_csv, mocker):
    # Arrange
    create_csv("1d", "AAPL")
    create_csv("1h", "AAPL")
    pipeline = StrategyPipeline()
    mock_f = MagicMock(return_value=True)
    mocker.patch.dict(StrategyPipeline.STRATEGY_MAP, {"s1": mock_f})
    params = {"duration": 14, "group": "test"}
    items = [DummyItem("s1", "1h", params=params)]

    # Act
    pipeline.run_pipeline(items)

    # Assert
    mock_f.assert_called_once()
    _, kwargs = mock_f.call_args
    assert kwargs == params


def test_run_pipeline_NoParams_CallHasNoKwargs(create_csv, mocker):
    # Arrange
    create_csv("1d", "AAPL")
    create_csv("1h", "AAPL")
    pipeline = StrategyPipeline()
    mock_f = MagicMock(return_value=True)
    mocker.patch.dict(StrategyPipeline.STRATEGY_MAP, {"s1": mock_f})
    items = [DummyItem("s1", "1h")]

    # Act
    pipeline.run_pipeline(items)

    # Assert
    _, kwargs = mock_f.call_args
    assert kwargs == {}


def test_run_pipeline_MultipleSymbols_OnlyPassingOnesIncluded(create_csv, mocker):
    # Arrange
    create_csv("1d", "AAPL")
    create_csv("1d", "TSLA")
    create_csv("1h", "AAPL")
    create_csv("1h", "TSLA")
    pipeline = StrategyPipeline()
    mock_f = MagicMock(side_effect=[True, False])
    mocker.patch.dict(StrategyPipeline.STRATEGY_MAP, {"s1": mock_f})

    items = [DummyItem("s1", "1h")]

    # Act
    result = pipeline.run_pipeline(items)

    # Assert
    assert result == ["AAPL"]
