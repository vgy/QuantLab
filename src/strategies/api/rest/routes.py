from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from strategies.schemas.strategy_schema import StrategiesResponse, SymbolsResponse
from strategies.schemas.downsampling_schema import DownsamplingResponse
from loguru import logger

def create_app(strategy_service, downsampling_service):
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

    return app
