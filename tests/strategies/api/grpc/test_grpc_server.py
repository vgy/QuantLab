import pytest
from unittest.mock import Mock, patch
from concurrent import futures

from strategies.api.grpc import strategies_pb2
from strategies.api.grpc.grpc_server import serve_grpc, StrategyServiceGRPC  # adjust import path if needed


@pytest.fixture
def mock_strategy_service():
    """Fixture that returns a mock strategy service."""
    return Mock()


@pytest.fixture
def grpc_service(mock_strategy_service):
    """Fixture returning an instance of the gRPC service."""
    return StrategyServiceGRPC(mock_strategy_service)


def test_GetStrategies_ValidRequest_ReturnsExpectedResponse(grpc_service, mock_strategy_service):
    # Arrange
    mock_strategies = ["strategy1", "strategy2"]
    mock_strategy_service.get_strategies.return_value = mock_strategies
    mock_context = Mock()

    # Act
    response = grpc_service.GetStrategies(request=Mock(), context=mock_context)

    # Assert
    assert isinstance(response, strategies_pb2.StrategiesResponse)
    assert response.message == "Returns 2 strategies"
    assert response.strategies == mock_strategies
    mock_strategy_service.get_strategies.assert_called_once()


def test_GetStrategies_EmptyList_ReturnsEmptyResponse(grpc_service, mock_strategy_service):
    # Arrange
    mock_strategy_service.get_strategies.return_value = []
    mock_context = Mock()

    # Act
    response = grpc_service.GetStrategies(request=Mock(), context=mock_context)

    # Assert
    assert response.message == "Returns 0 strategies"
    assert response.strategies == []


def test_GetStrategies_ServiceRaisesException_RaisesException(grpc_service, mock_strategy_service):
    # Arrange
    mock_strategy_service.get_strategies.side_effect = Exception("DB error")
    mock_context = Mock()

    # Act / Assert
    with pytest.raises(Exception, match="DB error"):
        grpc_service.GetStrategies(request=Mock(), context=mock_context)


def test_GetSymbolsForStrategyAndInterval_ValidRequest_ReturnsExpectedResponse(grpc_service, mock_strategy_service):
    # Arrange
    mock_symbols = ["AAPL", "GOOG"]
    mock_strategy_service.get_symbols_for_strategy_and_interval.return_value = mock_symbols

    mock_request = Mock(strategy="momentum", interval="1h")
    mock_context = Mock()

    # Act
    response = grpc_service.GetSymbolsForStrategyAndInterval(request=mock_request, context=mock_context)

    # Assert
    assert isinstance(response, strategies_pb2.SymbolsResponse)
    assert response.symbols == mock_symbols
    assert "Returns 2 symbols" in response.message
    mock_strategy_service.get_symbols_for_strategy_and_interval.assert_called_once_with("momentum", "1h")


def test_GetSymbolsForStrategyAndInterval_EmptySymbols_ReturnsEmptyResponse(grpc_service, mock_strategy_service):
    # Arrange
    mock_strategy_service.get_symbols_for_strategy_and_interval.return_value = []

    mock_request = Mock(strategy="mean_reversion", interval="5m")
    mock_context = Mock()

    # Act
    response = grpc_service.GetSymbolsForStrategyAndInterval(request=mock_request, context=mock_context)

    # Assert
    assert response.symbols == []
    assert "Returns 0 symbols" in response.message


def test_GetSymbolsForStrategyAndInterval_ServiceRaisesException_RaisesException(grpc_service, mock_strategy_service):
    # Arrange
    mock_strategy_service.get_symbols_for_strategy_and_interval.side_effect = Exception("Unexpected error")

    mock_request = Mock(strategy="breakout", interval="15m")
    mock_context = Mock()

    # Act / Assert
    with pytest.raises(Exception, match="Unexpected error"):
        grpc_service.GetSymbolsForStrategyAndInterval(request=mock_request, context=mock_context)


@patch("strategies.api.grpc.grpc_server.grpc.server")
@patch("strategies.api.grpc.strategies_pb2_grpc.add_StrategyServiceServicer_to_server")
def test_serve_grpc_ValidInputs_StartsServerAndReturnsInstance(mock_add_servicer, mock_grpc_server, mock_strategy_service):
    # Arrange
    mock_server_instance = Mock()
    mock_grpc_server.return_value = mock_server_instance

    host = "127.0.0.1"
    port = 50051

    # Act
    server = serve_grpc(mock_strategy_service, host, port)

    # Assert the gRPC server was created
    mock_grpc_server.assert_called_once()
    args, kwargs = mock_grpc_server.call_args
    assert isinstance(args[0], futures.ThreadPoolExecutor)

    # Assert the servicer was added
    mock_add_servicer.assert_called_once()
    called_args, _ = mock_add_servicer.call_args
    assert called_args[0].__class__.__name__ == "StrategyServiceGRPC"
    assert called_args[1] == mock_server_instance

    # Assert the returned server is our mock
    assert server == mock_server_instance


@patch("strategies.api.grpc.grpc_server.grpc.server", side_effect=Exception("Failed to start server"))
def test_serve_grpc_ServerCreationFails_RaisesException(mock_grpc_server, mock_strategy_service):
    # Arrange
    host = "localhost"
    port = 5050

    # Act / Assert
    with pytest.raises(Exception, match="Failed to start server"):
        serve_grpc(mock_strategy_service, host, port)

