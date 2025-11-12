from typing import List
from .engulfing import EngulfingStrategy

class StrategyFactory:
    @staticmethod
    def get_strategy(strategy: str, interval: str):
        if strategy == "Engulfing":
            return EngulfingStrategy(interval)
        else:
            raise ValueError(f"Unknown strategy: {strategy}")

    @staticmethod
    def get_symbols(strategy: str, interval: str) -> List[str]:
        strategy = StrategyFactory.get_strategy(strategy, interval)
        return strategy.get_symbols()
