import logging
from logging import Logger

class LoggingHelper:
    def get_logger(self, name: str) -> Logger:
        """
        Returns a logger object with a console handler
        """
        logger = logging.getLogger(name)
        logger.setLevel(logging.DEBUG)
        console_handler = logging.StreamHandler()
        console_handler.setLevel(logging.DEBUG)
        formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
        console_handler.setFormatter(formatter)
        logger.addHandler(console_handler)

        return logger