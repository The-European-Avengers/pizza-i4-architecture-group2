import logging
import sys
from typing import Optional

try:
    from pythonjsonlogger import jsonlogger
    JSON_LOGGER_AVAILABLE = True
except ImportError:
    JSON_LOGGER_AVAILABLE = False


def get_logger(
    name: Optional[str] = None,
    level: int = logging.INFO,
    json_format: bool = False,
) -> logging.Logger:
    """
    Returns a logger instance configured for Docker-friendly logging.

    Args:
        name (str, optional): Logger name. Defaults to root logger if None.
        level (int): Logging level, default INFO.
        json_format (bool): Whether to output logs in JSON format (requires python-json-logger).

    Returns:
        logging.Logger: Configured logger instance.
    """

    logger = logging.getLogger(name)
    logger.setLevel(level)

    # Remove any existing handlers
    if logger.hasHandlers():
        logger.handlers.clear()

    handler = logging.StreamHandler(sys.stdout)

    if json_format:
        if not JSON_LOGGER_AVAILABLE:
            raise ImportError(
                "python-json-logger package not installed. "
                "Install it with `pip install python-json-logger` to use json_format=True."
            )
        formatter = jsonlogger.JsonFormatter(
            '%(asctime)s %(levelname)s %(name)s %(message)s'
        )
    else:
        formatter = logging.Formatter(
            "%(asctime)s | %(levelname)s | %(name)s | %(message)s"
        )

    handler.setFormatter(formatter)
    logger.addHandler(handler)

    # Optional: Prevent log messages from propagating to the root logger twice
    logger.propagate = False

    return logger
