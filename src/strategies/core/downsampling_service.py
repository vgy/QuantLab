from loguru import logger
from typing import Optional
from strategies.utils.config_loader import load_config
from strategies.core.file_service import FileService
import pandas as pd

class DownsamplingService:
    def __init__(self):
        config = load_config()
        data = config.get("data")
        self.directory = data.get("directory")
        logger.info("DownsamplingService initialized")


    def write_downsampling(self, input_interval : str, output_interval : Optional[str] = "1h") -> str:
        try:
            dfs = FileService.read_all_csv(input_interval)
            ds_dfs = [self.downsample(df, output_interval) for df in dfs]

            for ds_df in ds_dfs:
                symbol = ds_df.iloc[0]["Symbol"]
                FileService.write(output_interval, ds_df, f"{output_interval}-{symbol}.csv")
            
            message = f"DownsamplingService: Downsampled from {input_interval} to {output_interval}"
            logger.info(message)
            return message
        except Exception as e:
            logger.exception("Failed to write downsample of all CSV files in '%s' to '%s': %s", input_interval, output_interval, e)
            raise
    
    def downsample(self, df: pd.DataFrame, interval: str, offset: Optional[str] = "15min") -> pd.DataFrame:
        df["Timestamp"] = pd.to_datetime(df["Timestamp"], format="%Y-%m-%d %H:%M:%S")
        df = df.set_index("Timestamp")
        ds_df = df.resample(
            interval,
            offset = offset  # aligns windows at HH:15
        ).agg({
            "Symbol": "first",
            "Interval": "first",
            "Open": "first",
            "High": "max",
            "Low": "min",
            "Close": "last",
            "Volume": "sum"
        })
        ds_df = ds_df.between_time("09:00", "16:00")
        ds_df = ds_df[ds_df.index.dayofweek < 5]
        ds_df = ds_df.dropna(subset=["Open", "Close"], how="any")
        ds_df["Interval"] = interval
        ds_df["Timestamp"] = ds_df.index.strftime("%Y-%m-%d %H:%M:%S")
        ds_df = ds_df[["Symbol", "Interval", "Timestamp", "Open", "High", "Low", "Close", "Volume"]]
        symbol = df.iloc[0]["Symbol"]
        logger.info(f"DownsamplingService: Downsampled to {interval} with {offset} origin for symbol {symbol}")
        return ds_df
