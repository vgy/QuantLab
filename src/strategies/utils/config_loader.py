import os
import yaml

_config_cache = None  # simple in-memory cache

def load_config(path=None):
    global _config_cache
    if _config_cache is not None:
        return _config_cache

    if path is None:
        # default path relative to this file
        path = os.path.join(os.path.dirname(__file__), "../config/config.yaml")
        path = os.path.abspath(path)

    with open(path, "r") as f:
        _config_cache = yaml.safe_load(f)

    return _config_cache