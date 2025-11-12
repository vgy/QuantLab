import pytest
from strategies.core.strategies.strategy_factory import StrategyFactory


@pytest.mark.parametrize("interval", ["1m", "5m", "1h", "1d"])
def test_get_strategy_Engulfing_ReturnsEngulfingStrategyInstance(mocker, interval):
    # Arrange
    mock_engulfing_cls = mocker.patch("strategies.core.strategies.strategy_factory.EngulfingStrategy")

    # Act
    result = StrategyFactory.get_strategy("Engulfing", interval)

    # Assert
    mock_engulfing_cls.assert_called_once_with(interval)
    assert result == mock_engulfing_cls.return_value


@pytest.mark.parametrize("invalid_strategy", ["Unknown", "Random", "Momentum"])
def test_get_strategy_UnknownStrategy_RaisesValueError(invalid_strategy):
    # Arrange
    interval = "15m"

    # Act / Assert
    with pytest.raises(ValueError) as exc_info:
        StrategyFactory.get_strategy(invalid_strategy, interval)

    # Assert
    assert f"Unknown strategy: {invalid_strategy}" in str(exc_info.value)


@pytest.mark.parametrize(
    "strategy_name, interval, expected_symbols",
    [
        ("Engulfing", "1m", ["AAPL", "TSLA"]),
        ("Engulfing", "15m", ["GOOG", "AMZN", "MSFT"]),
        ("Engulfing", "1h", []),
    ],
)
def test_get_symbols_ValidEngulfingStrategy_ReturnsExpectedSymbols(
    mocker, strategy_name, interval, expected_symbols
):
    # Arrange
    mock_strategy_instance = mocker.MagicMock()
    mock_strategy_instance.get_symbols.return_value = expected_symbols

    mocker.patch.object(StrategyFactory, "get_strategy", return_value=mock_strategy_instance)

    # Act
    result = StrategyFactory.get_symbols(strategy_name, interval)

    # Assert
    StrategyFactory.get_strategy.assert_called_once_with(strategy_name, interval)
    mock_strategy_instance.get_symbols.assert_called_once()
    assert result == expected_symbols
    assert isinstance(result, list)


@pytest.mark.parametrize("error_message", ["Unknown strategy", "Invalid strategy name"])
def test_get_symbols_UnknownStrategy_RaisesValueError(mocker, error_message):
    # Arrange
    mocker.patch.object(StrategyFactory, "get_strategy", side_effect=ValueError(error_message))

    # Act / Assert
    with pytest.raises(ValueError, match=error_message):
        StrategyFactory.get_symbols("Invalid", "5m")
