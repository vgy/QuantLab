import asyncio
import uvicorn
from loguru import logger
from strategies.utils.config_loader import load_config
from strategies.utils.logger import setup_logger
from strategies.core.downsampling_service import DownsamplingService
from strategies.core.strategy_service import StrategyService
from strategies.api.rest.routes import create_app
from strategies.api.grpc.grpc_server import serve_grpc

async def serve_rest(app, host, port):
    """Run FastAPI (REST) server."""
    logger.info(f"Starting REST server on {host}:{port}")
    config = uvicorn.Config(app=app, host=host, port=port, log_level="info")
    server = uvicorn.Server(config)
    await server.serve()

async def main():
    config = load_config()
    setup_logger(config["logging"]["log_file"], config["logging"]["level"])

    # Initialize core services
    downsampling_service = DownsamplingService()
    strategy_service = StrategyService()

    # Start gRPC server (in separate thread)
    grpc_server = serve_grpc(
        strategy_service,
        config["server"]["grpc_host"],
        config["server"]["grpc_port"]
    )

    # Start REST server (in asyncio loop)
    app = create_app(strategy_service, downsampling_service)
    rest_task = asyncio.create_task(
        serve_rest(app, config["server"]["rest_host"], config["server"]["rest_port"])
    )

    # Keep both servers alive
    try:
        await rest_task
    except KeyboardInterrupt:
        logger.info("Shutting down servers...")
        grpc_server.stop(0)

if __name__ == "__main__":
    asyncio.run(main())
