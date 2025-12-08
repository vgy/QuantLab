from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from strategies.schemas.strategy_schema import StrategiesResponse, SymbolsResponse, PatternsResponse, StrategyPipelineRequest
from strategies.schemas.downsampling_schema import DownsamplingResponse
from loguru import logger

def create_app(strategy_service, downsampling_service, candlestick_patterns_service, strategy_pipeline):
    app = FastAPI(title="Strategies Service", version="1.0")

    # Enable CORS
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    @app.get("/strategies", response_model = StrategiesResponse)
    def get_strategies():
        logger.info("REST: get_strategies is called")
        strategies = strategy_service.get_strategies()
        message = f"Returns {len(strategies)} strategies"
        return StrategiesResponse(message = message, strategies = strategies)

    @app.get("/strategies/{strategy}/{interval}", response_model = SymbolsResponse)
    def get_symbols_for_strategy_and_interval(strategy: str, interval: str):
        logger.info(f"REST: get_symbols_for_strategy_and_interval is called with {strategy} and {interval}")
        symbols = strategy_service.get_symbols_for_strategy_and_interval(strategy, interval)
        message = f"Returns {len(symbols)} symbols for strategy '{strategy}' and '{interval}"
        return SymbolsResponse(message = message, symbols = symbols)

    @app.post("/downsampling/{input_interval}/{output_interval}", response_model = DownsamplingResponse)
    def write_downsampling(input_interval : str, output_interval : str):
        logger.info("REST: write_downsampling is called")
        message = downsampling_service.write_downsampling(input_interval, output_interval)
        return DownsamplingResponse(message = message)

    @app.get("/candlestick/{group}/{subgroup}/{pattern}/{interval}/{period}", response_model = SymbolsResponse)
    def get_symbols_for_pattern_and_interval(group: str, subgroup: str, pattern: str, interval: str, period: int):
        logger.info(f"REST: get_symbols_for_pattern_and_interval is called with group:{group}, subgroup:{subgroup}, pattern:{pattern}, interval:{interval} and period:{period}")
        symbols = candlestick_patterns_service.get_symbols_for_pattern_and_interval(group, subgroup, pattern, interval, period)
        message = f"Returns {len(symbols)} symbols for group:{group}, subgroup:{subgroup}, pattern:{pattern}, interval:{interval} and period:{period}"
        return SymbolsResponse(message = message, symbols = symbols)

    @app.get("/patterns/candlestick/{symbol}/{interval}/{period}", response_model = PatternsResponse)
    def get_candlestick_patterns_for_symbol_interval_period(symbol: str, interval: str, period: int):
        logger.info(f"REST: get_candlestick_patterns_for_symbol_interval_period is called with symbol:{symbol}, interval:{interval}, and period:{period}")
        patterns = candlestick_patterns_service.get_candlestick_patterns_for_symbol_interval_period(symbol, interval, period)
        message = f"Returns {len(patterns)} candlestick patterns for symbol:{symbol}, interval:{interval}, and period:{period}"
        return PatternsResponse(message = message, patterns = patterns)
    
    @app.post("/pipeline/run", response_model = SymbolsResponse)
    def run_pipeline(request: StrategyPipelineRequest):
        logger.info(f"REST: run_pipeline is called with strategies:{request.strategies}")
        symbols = strategy_pipeline.run_pipeline(request.strategies)
        message = f"Returns {len(symbols)} symbols for strategies:{request.strategies}"
        return SymbolsResponse(message = message, symbols = symbols)
    return app
