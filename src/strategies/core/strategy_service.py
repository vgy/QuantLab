from loguru import logger

class StrategyService:
    def __init__(self):
        logger.info("StrategyService initialized")

    def get_strategies(self):
        strategies = ['Engulfing']
        logger.info(f"StrategyService: Returns {len(strategies)} strategies")
        return strategies

    def get_symbols_for_strategy_and_interval(self, strategy: str, interval: str):
        symbols = ['INFY','TCS','BHEL']
        logger.info(f"StrategyService: Returns {len(symbols)} symbols for strategy '{strategy}' and '{interval}")
        return symbols
