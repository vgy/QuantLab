import grpc
from concurrent import futures
from loguru import logger
from strategies.api.grpc import strategies_pb2, strategies_pb2_grpc


class StrategyServiceGRPC(strategies_pb2_grpc.StrategyServiceServicer):
    def __init__(self, strategy_service):
        self.strategy_service = strategy_service

    def GetStrategies(self, request, context):
        logger.info("gRPC: GetStrategies is called")
        strategies = self.strategy_service.get_strategies()
        message = f"Returns {len(strategies)} strategies"
        return strategies_pb2.StrategiesResponse(message = message, strategies = strategies)

    def GetSymbolsForStrategyAndInterval(self, request, context):
        logger.info(f"gRPC: GetSymbolsForStrategyAndInterval is called with {request.strategy} and {request.interval}")
        symbols = self.strategy_service.get_symbols_for_strategy_and_interval(request.strategy, request.interval)
        message = f"Returns {len(symbols)} symbols for strategy '{request.strategy}' and '{request.interval}"
        return strategies_pb2.SymbolsResponse(message = message, symbols = symbols)

def serve_grpc(strategy_service, host, port):
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=4))
    strategies_pb2_grpc.add_StrategyServiceServicer_to_server(
        StrategyServiceGRPC(strategy_service), server
    )
    server.add_insecure_port(f"{host}:{port}")
    logger.info(f"gRPC server listening on {host}:{port}")
    server.start()
    return server
