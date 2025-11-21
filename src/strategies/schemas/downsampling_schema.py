from pydantic import BaseModel, Field

class DownsamplingRequest(BaseModel):
    """Request to downsampling an interval"""
    input_interval: str = Field(..., description="Interval identifier (e.g., 1m, 5m, 1h)")
    output_interval: str = Field(..., description="Interval identifier (e.g., 1m, 5m, 1h)")

class DownsamplingResponse(BaseModel):
    """Response containing downsampling status."""
    message: str = Field(..., description="Returns status of Downsampling")

