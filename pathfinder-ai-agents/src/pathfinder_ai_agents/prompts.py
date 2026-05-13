from functools import lru_cache
from pathlib import Path


@lru_cache
def load_prompt(filename: str) -> str:
    project_root = Path(__file__).resolve().parents[2]
    prompt_path = project_root / "prompts" / filename
    return prompt_path.read_text(encoding="utf-8").strip()
