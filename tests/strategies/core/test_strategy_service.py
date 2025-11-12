import pytest
from strategies.core.strategy_service import StrategyService

@pytest.fixture
def service():
    """Fixture to initialize the StrategyService."""
    return StrategyService()


def test_get_strategies_WhenCalled_ReturnsListOfStrategies(service):
    # Act
    result = service.get_strategies()

    # Assert
    assert isinstance(result, list)
    assert len(result) > 0
    assert all(isinstance(x, str) for x in result)
    assert "Engulfing" in result

def test_get_strategies_WhenCalled_LogsExpectedMessages(mocker):
    # Arrange
    mock_logger = mocker.patch("strategies.core.strategy_service.logger")
    service = StrategyService()

    # Act
    result = service.get_strategies()

    # Assert
    mock_logger.info.assert_any_call("StrategyService initialized")
    mock_logger.info.assert_any_call(f"StrategyService: Returns {len(result)} strategies")

@pytest.mark.parametrize(
    "strategy,interval,expected_symbols",
    [
        ("Engulfing", "1h", ["INFY", "TCS", "BHEL"]),
        ("Engulfing", "5m", ["GOOGL", "MSFT", "AAPL"]),
        ("Unknown", "1h", ["META", "AMZN", "NFLX"]),
    ],
)
def test_get_symbols_for_strategy_and_interval_ValidInputs_ReturnsExpectedSymbols(mocker, service, strategy, interval, expected_symbols):
    #Arrange
    mocker.patch("strategies.core.strategy_service.StrategyFactory.get_symbols", return_value=expected_symbols)

    # Act
    result = service.get_symbols_for_strategy_and_interval(strategy, interval)

    # Assert
    assert isinstance(result, list)
    assert result == expected_symbols


def test_get_symbols_for_strategy_and_interval_WhenCalled_LogsExpectedMessages(mocker):
    # Arrange
    mock_logger = mocker.patch("strategies.core.strategy_service.logger")
    service = StrategyService()
    strategy = "Engulfing"
    interval = "1h"

    # Act
    symbols = service.get_symbols_for_strategy_and_interval(strategy, interval)

    # Assert
    mock_logger.info.assert_any_call("StrategyService initialized")
    mock_logger.info.assert_any_call(
        f"StrategyService: Returns {len(symbols)} symbols for strategy '{strategy}' and '{interval}"
    )


def test_get_symbols_for_strategy_and_interval_WhenEmptyInput_ReturnsDefaultSymbols(mocker, service):
    #Arrange
    mocker.patch("strategies.core.strategy_service.StrategyFactory.get_symbols", return_value=["INFY", "TCS", "BHEL"])

    # Act
    result = service.get_symbols_for_strategy_and_interval("", "")

    # Assert
    assert isinstance(result, list)
    assert len(result) == 3
    assert all(isinstance(x, str) for x in result)
