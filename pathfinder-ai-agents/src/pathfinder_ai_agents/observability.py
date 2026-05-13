import logging

from azure.monitor.opentelemetry import configure_azure_monitor

from pathfinder_ai_agents.config import Settings

logger = logging.getLogger(__name__)


def configure_observability(settings: Settings) -> None:
    if not settings.applicationinsights_connection_string:
        logger.info("Application Insights connection string is not configured; tracing disabled.")
        return

    configure_azure_monitor(connection_string=settings.applicationinsights_connection_string)
    logger.info("Azure Monitor tracing configured.")
