from loguru import logger
import sys

def setup_logger(log_file: str, level: str = "INFO"):
    logger.remove()
    logger.add(sys.stdout, level=level)
    logger.add(log_file, rotation="1 MB", level=level)
    logger.info("Logger initialized")
    return logger
