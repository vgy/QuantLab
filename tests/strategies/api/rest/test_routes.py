import pytest
from fastapi.testclient import TestClient
from unittest.mock import Mock
from strategies.api.rest.routes import create_app


@pytest.fixture
def mock_strategy_service():
    """Fixture that returns a mock strategy service."""
    return Mock()

@pytest.fixture
def mock_downsampling_service():
    """Fixture that returns a mock strategy service."""
    return Mock()

@pytest.fixture
def mock_candlestick_patterns_service():
    """Fixture that returns a mock strategy service."""
    return Mock()


@pytest.fixture
def client(mock_strategy_service, mock_downsampling_service, mock_candlestick_patterns_service):
    """Fixture that returns a FastAPI TestClient with a mocked strategy service."""
    app = create_app(mock_strategy_service, mock_downsampling_service, mock_candlestick_patterns_service)
    return TestClient(app, raise_server_exceptions=False)

def test_get_strategies_ValidRequest_ReturnsStrategiesResponse(client, mock_strategy_service):
    # Arrange
    mock_strategies = ["mean_reversion", "momentum"]
    mock_strategy_service.get_strategies.return_value = mock_strategies

    # Act
    response = client.get("/strategies")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert "message" in data
    assert "strategies" in data
    assert len(data["strategies"]) == 2
    mock_strategy_service.get_strategies.assert_called_once()


def test_get_strategies_EmptyList_ReturnsEmptyStrategiesList(client, mock_strategy_service):
    # Arrange
    mock_strategy_service.get_strategies.return_value = []

    # Act
    response = client.get("/strategies")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert data["strategies"] == []
    assert "Returns 0 strategies" in data["message"]
    mock_strategy_service.get_strategies.assert_called_once()


def test_get_strategies_ServiceRaisesException_ReturnsInternalServerError(client, mock_strategy_service):
    # Arrange
    mock_strategy_service.get_strategies.side_effect = Exception("Database error")

    # Act
    response = client.get("/strategies")

    # Assert
    assert response.status_code == 500  # FastAPI default for unhandled exceptions

def test_get_symbols_for_strategy_and_interval_ValidRequest_ReturnsSymbolsResponse(client, mock_strategy_service):
    # Arrange
    mock_symbols = ["AAPL", "MSFT", "GOOG"]
    mock_strategy_service.get_symbols_for_strategy_and_interval.return_value = mock_symbols

    strategy = "momentum"
    interval = "1d"

    # Act
    response = client.get(f"/strategies/{strategy}/{interval}")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert "symbols" in data
    assert len(data["symbols"]) == 3
    assert f"{len(mock_symbols)} symbols" in data["message"]
    mock_strategy_service.get_symbols_for_strategy_and_interval.assert_called_once_with(strategy, interval)


def test_get_symbols_for_strategy_and_interval_EmptySymbols_ReturnsEmptyList(client, mock_strategy_service):
    # Arrange
    mock_strategy_service.get_symbols_for_strategy_and_interval.return_value = []

    strategy = "mean_reversion"
    interval = "5m"

    # Act
    response = client.get(f"/strategies/{strategy}/{interval}")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert data["symbols"] == []
    assert "Returns 0 symbols" in data["message"]
    mock_strategy_service.get_symbols_for_strategy_and_interval.assert_called_once_with(strategy, interval)


def test_get_symbols_for_strategy_and_interval_ServiceRaisesException_ReturnsInternalServerError(client, mock_strategy_service):
    # Arrange
    mock_strategy_service.get_symbols_for_strategy_and_interval.side_effect = Exception("Unexpected error")

    strategy = "breakout"
    interval = "15m"

    # Act
    response = client.get(f"/strategies/{strategy}/{interval}")

    # Assert
    assert response.status_code == 500
    mock_strategy_service.get_symbols_for_strategy_and_interval.assert_called_once_with(strategy, interval)


def test_app_Metadata_IsSetCorrectly(mock_strategy_service, mock_downsampling_service, mock_candlestick_patterns_service):
    # Arrange
    app = create_app(mock_strategy_service, mock_downsampling_service, mock_candlestick_patterns_service)

    # Act
    title = app.title
    version = app.version
    middleware_names = [m.cls.__name__ for m in app.user_middleware]

    # Assert
    assert title == "Strategies Service"
    assert version == "1.0"
    assert "CORSMiddleware" in middleware_names


def test_cors_HeadersIncludedInResponse(client, mock_strategy_service):
    # Arrange
    mock_strategy_service.get_strategies.return_value = []

    # Act
    response = client.get("/strategies", headers={"Origin": "http://example.com"})

    # Assert
    assert response.status_code == 200
    assert "access-control-allow-origin" in response.headers
    assert response.headers["access-control-allow-origin"] == "*"

@pytest.mark.parametrize(
    "input_interval,output_interval",
    [
        ("5min", "15min"),
        ("15min", "30min"),
        ("15min", "1h"),
    ],
)
def test_write_downsampling_ValidRequest_ReturnsDownsamplingResponse(client, mock_downsampling_service, input_interval, output_interval):
    # Arrange
    mock_downsampling_service.write_downsampling.return_value = "successful"

    # Act
    response = client.post(f"/downsampling/{input_interval}/{output_interval}")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert "message" in data
    assert data["message"] == "successful"
    mock_downsampling_service.write_downsampling.assert_called_once()


def test_write_downsampling_ServiceRaisesException_ReturnsInternalServerError(client, mock_downsampling_service):
    # Arrange
    mock_downsampling_service.write_downsampling.side_effect = Exception("Database error")

    # Act
    response = client.post("/downsampling/15min/1h")

    # Assert
    assert response.status_code == 500  # FastAPI default for unhandled exceptions

def test_get_symbols_for_pattern_and_interval_ValidRequest_ReturnsSymbolsResponse(client, mock_candlestick_patterns_service):
    # Arrange
    mock_symbols = ["AAPL", "MSFT", "GOOG"]
    mock_candlestick_patterns_service.get_symbols_for_pattern_and_interval.return_value = mock_symbols

    group = "bullish"
    subgroup = "reversal"
    pattern = "cdlengulfing"
    interval = "1d"
    period=3

    # Act
    response = client.get(f"/candlestick/{group}/{subgroup}/{pattern}/{interval}/{period}")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert "symbols" in data
    assert len(data["symbols"]) == 3
    assert f"{len(mock_symbols)} symbols" in data["message"]
    mock_candlestick_patterns_service.get_symbols_for_pattern_and_interval.assert_called_once_with(group, subgroup, pattern, interval, period)


def test_get_symbols_for_pattern_and_interval_EmptySymbols_ReturnsEmptyList(client, mock_candlestick_patterns_service):
    # Arrange
    mock_candlestick_patterns_service.get_symbols_for_pattern_and_interval.return_value = []

    group = "bullish"
    subgroup = "reversal"
    pattern = "cdlengulfing"
    interval = "1d"
    period=3

    # Act
    response = client.get(f"/candlestick/{group}/{subgroup}/{pattern}/{interval}/{period}")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert data["symbols"] == []
    assert "Returns 0 symbols" in data["message"]
    mock_candlestick_patterns_service.get_symbols_for_pattern_and_interval.assert_called_once_with(group, subgroup, pattern, interval, period)


def test_get_symbols_for_pattern_and_interval_ServiceRaisesException_ReturnsInternalServerError(client, mock_candlestick_patterns_service):
    # Arrange
    mock_candlestick_patterns_service.get_symbols_for_pattern_and_interval.side_effect = Exception("Unexpected error")

    group = "bullish"
    subgroup = "reversal"
    pattern = "cdlengulfing"
    interval = "1d"
    period=3

    # Act
    response = client.get(f"/candlestick/{group}/{subgroup}/{pattern}/{interval}/{period}")

    # Assert
    assert response.status_code == 500
    mock_candlestick_patterns_service.get_symbols_for_pattern_and_interval.assert_called_once_with(group, subgroup, pattern, interval, period)