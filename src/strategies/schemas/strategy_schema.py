from pydantic import BaseModel, Field
from typing import List

class Empty(BaseModel):
    """Equivalent to the gRPC Empty message."""
    pass


class StrategyAndIntervalRequest(BaseModel):
    """Request to get symbols for a given strategy and interval."""
    strategy: str = Field(..., description="Strategy name (e.g., MeanReversion, Momentum)")
    interval: str = Field(..., description="Interval identifier (e.g., 1m, 5m, 1h)")


class StrategiesResponse(BaseModel):
    """Response containing available strategies."""
    message: str = Field(..., description="Returns len(strategies) strategies")
    strategies: List[str] = Field(default_factory=list, description="List of available strategies")


class SymbolsResponse(BaseModel):
    """Response containing symbols for a given strategy and interval."""
    message: str = Field(..., description="Returns len(symbols) symbols for strategy (strategy) and (interval)")
    symbols: List[str] = Field(default_factory=list, description="List of matching symbols")


class PatternsResponse(BaseModel):
    """Response containing patterns for a given symbol, interval, and period."""
    message: str = Field(..., description="Returns len(patterns) patterns for symbol, interval and period")
    patterns: List[str] = Field(default_factory=list, description="List of matching patterns and their timestamp")
