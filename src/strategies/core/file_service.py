import os
import pandas as pd
from loguru import logger
from strategies.utils.config_loader import load_config

class FileService:
    
    @staticmethod
    def write(interval: str, df: pd.DataFrame, filename: str) -> None:
        """Write a pandas DataFrame to a CSV file in a subdirectory for the given interval."""
        try:
            directory = FileService.__get_directory()
            folder_path = os.path.join(directory, interval)
            os.makedirs(folder_path, exist_ok=True)  # ensure interval folder exists
            file_path = os.path.join(folder_path, filename)            
            df.to_csv(file_path, index=False)
            logger.info("Data written successfully to %s", file_path)
        except Exception as e:
            logger.exception("Failed to write data for interval '%s' and file '%s' : %s", interval, filename, e)
            raise

    @staticmethod
    def read(filename: str) -> pd.DataFrame:
        """Read a CSV file from a subdirectory for the given interval into a pandas DataFrame."""
        try:
            directory = FileService.__get_directory()
            file_path = os.path.join(directory, filename)            
            if not os.path.exists(file_path):
                raise FileNotFoundError(f"File '{file_path}' does not exist")            
            df = pd.read_csv(file_path)
            logger.info("Data read successfully from %s", file_path)
            return df
        except Exception as e:
            logger.exception("Failed to read data for file '%s': %s", file_path, e)
            raise

    @staticmethod
    def read_all_csv(subdirectory: str) -> list[pd.DataFrame]:
        """
        Read all CSV files in a given subdirectory.
        Returns a list of pandas DataFrames.
        """
        dataframes = []
        directory = FileService.__get_directory()
        folder_path = os.path.join(directory, subdirectory)
        try:
            if not os.path.exists(folder_path):
                raise FileNotFoundError(f"Directory '{folder_path}' does not exist")
            csv_files = [f for f in os.listdir(folder_path) if f.lower().endswith(".csv")]
            for csv_file in csv_files:
                full_path = os.path.join(subdirectory, csv_file)  # relative path for read()
                df = FileService.read(full_path)
                dataframes.append(df)
            return dataframes
        except Exception as e:
            logger.exception("Failed to read all CSV files in '%s': %s", folder_path, e)
            raise

    @staticmethod
    def __get_directory() -> str:
        config = load_config()
        data = config.get("data")
        return data.get("directory")
    