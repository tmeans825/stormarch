"""Locates the Unity project root so the scripts work from any checkout (override with STORMARCH_ROOT)."""
import os


def project_root():
    env = os.environ.get("STORMARCH_ROOT")
    if env:
        return env
    here = os.path.abspath(os.path.dirname(__file__))
    while True:
        if os.path.isdir(os.path.join(here, "Assets")) and os.path.isdir(os.path.join(here, "ProjectSettings")):
            return here
        parent = os.path.dirname(here)
        if parent == here:
            raise RuntimeError("Unity project root not found; set STORMARCH_ROOT")
        here = parent


ASSETS = os.path.join(project_root(), "Assets")
