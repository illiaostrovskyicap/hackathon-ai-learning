from functools import lru_cache

from pydantic import AliasChoices, Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    app_env: str = Field(default="local", alias="PATHFINDER_APP_ENV")
    log_level: str = Field(default="INFO", alias="PATHFINDER_LOG_LEVEL")
    openai_base_url: str = Field(
        validation_alias=AliasChoices("OPENAI_BASE_URL", "FOUNDRY_OPENAI_BASE_URL"),
    )
    openai_api_key: str = Field(
        validation_alias=AliasChoices("OPENAI_API_KEY", "AZURE_OPENAI_API_KEY"),
    )
    model_deployment_name: str = Field(
        validation_alias=AliasChoices(
            "AZURE_OPENAI_DEPLOYMENT_NAME",
            "OPENAI_MODEL_DEPLOYMENT_NAME",
            "FOUNDRY_MODEL_DEPLOYMENT_NAME",
        ),
    )
    applicationinsights_connection_string: str | None = Field(
        default=None,
        alias="APPLICATIONINSIGHTS_CONNECTION_STRING",
    )
    microsoft_learn_mcp_endpoint: str = Field(
        default="https://learn.microsoft.com/api/mcp",
        alias="MICROSOFT_LEARN_MCP_ENDPOINT",
    )

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )


@lru_cache
def get_settings() -> Settings:
    return Settings()
